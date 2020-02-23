/*
Copyright (C) 2019-2020 Maciej Szybiak

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License along
with this program; if not, write to the Free Software Foundation, Inc.,
51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using UnityEngine;

/*
 * A 1:1 rewrite of Quake 2's collision system. Used for player's movement
 * and weapon projectiles.
 */

public struct SurfaceT
{
    public string name;
    public int flags;
    public int value;
}

public class TraceT
{
    public bool allsolid;
    public bool startsolid;
    public float fraction;
    public Vector3 endpos;
    public BSPPlane plane;
    public SurfaceT surface;
    public BrushContents contents;

    public BSPModel clipmodel;
}

public static class Trace
{
    private static readonly float DIST_EPSILON = 0.03125f;

    private static int leaf_count, leaf_maxcount;
    private static BSPLeaf[] leaf_list;
    private static Vector3 leaf_mins, leaf_maxs;
    private static BSPNode leaf_topnode;

    private static int checkcount = 0;
    private static int floodvalid;

    private static int trace_contents;
    private static TraceT trace_trace;

    public static Vector3[] trace_offsets = new Vector3[8];
    private static Vector3 trace_start, trace_end;

    private static Vector3 trace_extents;

    private static bool trace_ispoint;

    public static TraceT CL_Trace(Vector3 start, Vector3 mins, Vector3 maxs, Vector3 end)
    {
        TraceT t;

        CM_BoxTrace(out t, start, end, mins, maxs, BSPFile.headnode, (int)BrushContents.MASK_PLAYERSOLID);

        CL_ClipMoveToEntities(start, mins, maxs, end, ref t);

        return t;
    }

    #region solid trace

    private static void CM_BoxTrace(out TraceT trace, Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, BSPNode headnode, int brushmask)
    {
        checkcount++;
        trace_trace = new TraceT
        {
            fraction = 1,
            surface = new SurfaceT()
        };

        trace = trace_trace;
        trace_start = start;
        trace_end = end;
        trace_contents = brushmask;

        Vector3[] bounds = new Vector3[]
        {
            mins, maxs
        };

        for (int k = 0; k < 8; k++)
        {
            trace_offsets[k] = new Vector3();
        }

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                trace_offsets[i][j] = bounds[i >> j & 1][j];
            }
        }

        //check for position test special case
        if (start == end)
        {
            BSPLeaf[] leafs = new BSPLeaf[1024];
            int numleafs;
            Vector3 c1, c2;

            //M increase bounds size
            c1 = start + mins - Vector3.one;
            c2 = start + maxs + Vector3.one;

            numleafs = CM_BoxLeafs_Headnode(c1, c2, ref leafs, 1024, headnode, out BSPNode topnode);

            for (int i = 0; i < numleafs; i++)
            {
                CM_TestInLeaf(leaf_list[i]);

                if (trace_trace.allsolid)
                {
                    break;
                }
            }

            trace_trace.endpos = start;
            return;
        }

        //check for point special case
        if(mins == Vector3.zero && maxs == Vector3.zero)
        {
            trace_ispoint = true;
            trace_extents = Vector3.zero;
        }
        else
        {
            trace_ispoint = false;
            trace_extents = new Vector3(Mathf.Max(-mins[0], maxs[0]), Mathf.Max(-mins[1], maxs[1]), Mathf.Max(-mins[2], maxs[2]));
        }

        //general sweeping through world
        CM_RecursiveHullCheck(headnode, 0, 1, start, end);

        if(trace_trace.fraction == 1)
        {
            trace_trace.endpos = end;
        }
        else
        {
            trace_trace.endpos = Vector3.Lerp(start, end, trace_trace.fraction);
        }
    }

    /// <summary>
    /// Tests the world by recursively picking correct leafs and executing TraceToLeaf
    /// </summary>
    /// <param name="node">Headnode for testing (can be bmodel or worldspawn).</param>
    /// <param name="p1f">Fraction of start->hit distance.</param>
    /// <param name="p2f">Fraction of end->hit distance.</param>
    /// <param name="p1">Trace start.</param>
    /// <param name="p2">Trace end.</param>
    private static void CM_RecursiveHullCheck(BSPNode node, float p1f, float p2f, Vector3 p1, Vector3 p2)
    {
        BSPPlane plane;
        float t1, t2, offset;
        float frac, frac2;
        float idist;
        Vector3 mid;
        int side;
        float midf;

        if(trace_trace.fraction <= p1f)
        {
            return; //already hit something nearer
        }

        //instead of goto in the original code...
        bool recheck = true;

        //...do while true!
        //Follows the BSP tree down to the leafs.
        do
        {
            //if node is a leaf perform tracing
            if (node is BSPLeaf)
            {
                CM_TraceToLeaf(node as BSPLeaf);
                return; //go up recursion
            }
            plane = node.Plane;

            //find the point distances to the separating plane
            //and the offset for the size of the box
            //M - ignore axial stuff...
            t1 = Utils.PlaneDiff(p1, plane);
            t2 = Utils.PlaneDiff(p2, plane);
            if (trace_ispoint)
            {
                offset = 0;
            }
            else
            {
                offset = Mathf.Abs(trace_extents[0] * plane.normal[0]) +
                    Mathf.Abs(trace_extents[1] * plane.normal[1]) +
                    Mathf.Abs(trace_extents[2] * plane.normal[2]);
            }

            //see which sides we need to consider
            if (t1 >= offset && t2 >= offset)
            {
                //go down to front node
                node = node.front_node;
                continue;
            }
            if (t1 < -offset && t2 < -offset)
            {
                //go down to back node
                node = node.back_node;
                continue;
            }
            recheck = false;
        } while (recheck);

        //put the crosspoint DIST_EPSILON pixels on the near sides
        //?
        if (t1 < t2)
        {
            idist = 1.0f / (t1 - t2);
            side = 1;
            frac2 = (t1 + offset + DIST_EPSILON) * idist;
            frac = (t1 - offset + DIST_EPSILON) * idist;
        }
        else if(t1 > t2)
        {
            idist = 1.0f / (t1 - t2);
            side = 0;
            frac2 = (t1 - offset - DIST_EPSILON) * idist;
            frac = (t1 + offset + DIST_EPSILON) * idist;
        }
        else
        {
            side = 0;
            frac = 1;
            frac2 = 0;
        }

        //move up to the node
        midf = p1f + (p2f - p1f) * Mathf.Clamp(frac, 0, 1);
        mid = Vector3.Lerp(p1, p2, frac);

        CM_RecursiveHullCheck(side == 0 ? node.front_node : node.back_node, p1f, midf, p1, mid);

        //go past the node
        midf = p1f + (p2f - p1f) * Mathf.Clamp(frac2, 0, 1);
        mid = Vector3.Lerp(p1, p2, frac2);

        CM_RecursiveHullCheck((side ^ 1) == 0 ? node.front_node : node.back_node, midf, p2f, mid, p2);
    }

    private static void CM_TraceToLeaf(BSPLeaf leaf)
    {
        int k;
        BSPBrush b;
        BSPBrush[] leafbrushes;

        if(((int)leaf.contents & trace_contents) == 0)
        {
            return;
        }

        //trace line against all brushes in the leaf
        leafbrushes = leaf.Brushes;

        for (k = 0; k < leaf.num_brushes; k++)
        {
            b = leafbrushes[k];

            //a lame method to avoid rechecks for brushes that are in multiple leafs
            if(b.checkcount == checkcount)
            {
                continue;
            }
            b.checkcount = checkcount;

            if (((int)b.contents & trace_contents) == 0)
            {
                continue;
            }

            CM_ClipBoxToBrush(trace_start, trace_end, ref trace_trace, b);
            if (trace_trace.fraction == 0) //didn't move at all, so we can return
            {
                return;
            }
        }
    }

    /// <summary>
    /// Checks for intersection with a brush during a moving trace.
    /// </summary>
    /// <param name="p1">Trace start.</param>
    /// <param name="p2">Trace end.</param>
    /// <param name="trace">Data struct.</param>
    /// <param name="brush">Brush to test against.</param>
    private static void CM_ClipBoxToBrush(Vector3 p1, Vector3 p2, ref TraceT trace, BSPBrush brush)
    {
        int i;
        BSPPlane plane;
        BSPPlane clipplane = new BSPPlane(); //initialize this to keep c# happy...
        float dist;
        float enterfrac, leavefrac;
        float d1, d2;
        bool getout, startout;
        bool hasclip = false;
        bool haslead = false;
        float f;
        BSPBrushSide leadside = new BSPBrushSide(); //initialize this to keep c# happy...
        BSPBrushSide[] sides;

        if (brush.numsides == 0)
        {
            return;
        }

        sides = brush.Sides;

        enterfrac = -1;
        leavefrac = 1;

        getout = false;     //gets out of the brush
        startout = false;   //starts inside the brush
        for(i = 0; i < brush.numsides; i++)
        {
            plane = sides[i].Plane; //we only need plane for each side

            //get plane to bounds point distance
            if (!trace_ispoint)
            {
                //bounding box trace
                dist = Vector3.Dot(trace_offsets[plane.Signbits], plane.normal);
                dist = plane.distance - dist;
            }
            else
            {
                //point trace
                dist = plane.distance;
            }

            //get distances for start and end of the trace
            d1 = Vector3.Dot(p1, plane.normal) - dist; //start
            d2 = Vector3.Dot(p2, plane.normal) - dist; //end

            if (d2 > 0)
            {
                getout = true; //endpoint is not in solid
            }
            if(d1 > 0)
            {
                startout = true; //starts outside
            }

            //if completely in front of a face, no intersection
            if(d1 > 0 && d2 >= d1)
            {
                return; //a single face plane that is not crossed while tracing means that we never hit that brush
            }

            //?
            if(d1 <= 0 && d2 <= 0)
            {
                continue;
            }

            //crosses face
            if(d1 > d2)
            {
                //enter
                f = (d1 - DIST_EPSILON) / (d1 - d2); //fraction of the distance covered before hitting a side
                if(f > enterfrac)
                {
                    enterfrac = f;
                    clipplane = plane;
                    hasclip = true; //plane clips the trace
                    leadside = sides[i];
                    haslead = true; //could be combined with hasclip? writes flags for the collided face
                }
            }
            else
            {
                //leave
                f = (d1 + DIST_EPSILON) / (d1 - d2);
                if(f < leavefrac)
                {
                    leavefrac = f;
                }
            }
        }

        //startout wasn't true for any face
        if (!startout)
        {
            //original point was inside brush
            trace.startsolid = true;
            if (!getout)
            {
                trace.allsolid = true;
                //map_allsolid_bug
                trace.fraction = 0;
                trace.contents = brush.contents;
            }
            return;
        }

        //after testing all faces we need to see which test went further
        if(enterfrac < leavefrac)
        {
            //only write the test if the brush was closer than any brushes hit before
            if (enterfrac > -1 && enterfrac < trace.fraction)
            {
                if (enterfrac < 0)
                {
                    enterfrac = 0;
                }
                trace.fraction = enterfrac;
                if (hasclip)
                {
                    trace.plane = clipplane;
                }
                if (haslead)
                {
                    if (leadside.HasTexInfo)
                    {
                        trace.surface = leadside.TexInfo.Surface;
                    }
                }
                trace.contents = brush.contents;
            }
        }
    }

    private static void CM_TestInLeaf(BSPLeaf leaf)
    {
        int k;
        BSPBrush b;
        BSPBrush[] leafbrushes;

        if (leaf.num_brushes == 0)
        {
            return;
        }
        if (((int)trace_contents & (int)leaf.contents) == 0)
        {
            return;
        }

        //trace line against all brushes in the leaf
        leafbrushes = leaf.Brushes;

        for (k = 0; k < leafbrushes.Length; k++)
        {
            b = leafbrushes[k];

            if(b.checkcount == checkcount)
            {
                continue;
            }
            b.checkcount = checkcount;

            if (((int)b.contents & trace_contents) == 0)
            {
                continue;
            }

            CM_TestBoxInBrush(trace_start, ref trace_trace, b);

            if(trace_trace.fraction == 0)
            {
                return;
            }
        }
    }

    private static void CM_TestBoxInBrush(Vector3 p1, ref TraceT trace, BSPBrush brush)
    {
        BSPBrushSide[] sides = brush.Sides;
        BSPPlane plane;
        float dist, d1;

        for(int i = 0; i < sides.Length; i++)
        {
            plane = sides[i].Plane;

            dist = Vector3.Dot(trace_offsets[plane.Signbits], plane.normal);
            dist = plane.distance - dist;

            d1 = Vector3.Dot(p1, plane.normal) - dist;

            //completely in front of a face, no intersection
            if(d1 > 0)
            {
                return;
            }
        }

        trace.startsolid = trace.allsolid = true;
        trace.fraction = 0;
        trace.contents = brush.contents;
    }

    private static int CM_BoxLeafs_Headnode(Vector3 mins, Vector3 maxs, ref BSPLeaf[] list, int listsize, BSPNode headnode, out BSPNode topnode)
    {
        leaf_list = list;
        leaf_count = 0;
        leaf_maxcount = listsize;
        leaf_mins = mins;
        leaf_maxs = maxs;

        leaf_topnode = null;

        CM_BoxLeafs_Recursive(headnode);

        topnode = null;
        return leaf_count;
    }

    private static void CM_BoxLeafs_Recursive(BSPNode node)
    {
        int s;

        while(!(node is BSPLeaf))
        {
            s = Utils.BoxOnPlaneSide(leaf_mins, leaf_maxs, node.Plane);
            if(s == 1)
            {
                node = node.front_node; //M is this the correct child? - yes it is /M
            }
            else if(s == 2)
            {
                node = node.back_node;
            }
            else
            {
                //go down both
                if(leaf_topnode == null)
                {
                    leaf_topnode = node;
                }
                CM_BoxLeafs_Recursive(node.front_node);
                node = node.back_node;
            }
        }

        if(leaf_count < leaf_maxcount)
        {
            leaf_list[leaf_count] = node as BSPLeaf;
            leaf_count++;
        }
    }

    #endregion

    #region entity clip

    private static void CL_ClipMoveToEntities(Vector3 start, Vector3 mins, Vector3 maxs, Vector3 end, ref TraceT tr)
    {
        TraceT trace = new TraceT();
        BSPNode headnode;
        //entity blah
        BSPModel cmodel;

        //null check for physicsModels?
        if(BSPFile.physicsModels == null)
        {
            return; //i guess so
        }
        for(int i = 0; i < BSPFile.physicsModels.Count; i++)
        {
            cmodel = BSPFile.physicsModels[i].Bmodel;

            headnode = cmodel.headnode;

            if (tr.allsolid)
            {
                return;
            }

            CM_TransformedBoxTrace(ref trace, start, end, mins, maxs, headnode, BrushContents.MASK_PLAYERSOLID, 
                BSPFile.physicsModels[i].Ent.MoveOrigin / Globals.scale.Value, BSPFile.physicsModels[i].Ent.MoveAngles);

            CM_ClipEntity(ref tr, ref trace, cmodel);
        }
    }

    //for moving and rotating entities :x
    private static void CM_TransformedBoxTrace(ref TraceT trace, Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, BSPNode headnode, BrushContents brushmask, Vector3 origin, Vector3 angles)
    {
        Vector3 start_l;
        Vector3 end_l;
        bool rotated;
        Vector3[] axes = new Vector3[3];

        start_l = start - origin;
        end_l = end - origin;

        rotated = angles != null;

        if (rotated)
        {
            Utils.AnglesToAxis(angles, out axes[0], out axes[1], out axes[2]);
            start_l = Utils.RotatePoint(start_l, axes);
            end_l = Utils.RotatePoint(end_l, axes);
        }

        CM_BoxTrace(out trace, start_l, end_l, mins, maxs, headnode, (int)brushmask);

        if (rotated && trace.fraction != 1 && trace.plane != null)
        {
            Utils.TransposeAxis(ref axes);
            trace.plane.normal = Utils.RotatePoint(trace.plane.normal, axes);
        }

        //Carmack: FIXME: offset plane distance? M: wut?

        trace.endpos = Vector3.Lerp(start, end, trace.fraction);
    }

    private static void CM_ClipEntity(ref TraceT dst, ref TraceT src, BSPModel model)
    {
        dst.allsolid = src.allsolid;
        dst.startsolid = src.startsolid;

        if(src.fraction < dst.fraction)
        {
            dst.fraction = src.fraction;
            dst.endpos = src.endpos;
            dst.plane = src.plane;
            dst.surface = src.surface;
            dst.contents |= src.contents;
            dst.clipmodel = model;
        }
    }

    #endregion
}
