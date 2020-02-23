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

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class Utils
{
    public static bool IsGamePathValid()
    {
        return Globals.validgamepath.Boolean;
    }

    public static bool IsModnameValid()
    {
        return Globals.validmodname.Boolean;
    }

    public static bool ParseThreeNumbers(string s, out Vector3 num, bool flip)
    {
        char current;
        string number = "";
        num = new Vector3();
        Queue<char> q = new Queue<char>(s);

        for (int i = 0; i < 3; i++)
        {
            current = '\0';

            while (current != ' ' && q.Count > 0)
            {
                current = q.Dequeue();
                number += current;
            }

            number = number.Replace(',', '.');
            if (float.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out float result))
            {
                if (flip)
                {
                    switch (i)
                    {
                        case 0:
                            num[0] = result;
                            break;
                        case 1:
                            num[2] = result;
                            break;
                        case 2:
                            num[1] = result;
                            break;
                    }
                }
                else
                {
                    num[i] = result;
                }
                
            }
            else
            {
                Console.LogWarning("Failed to parse three numbers: " + s + " num: " + number);
                return false;
            }

            number = "";

            if (q.Count == 0)
            {
                break;
            }
        }
        return true;
    }

    public static int BoxOnPlaneSide(Vector3 emins, Vector3 emaxs, BSPPlane plane)
    {
        Vector3[] bounds = { emins, emaxs };
        Vector3 normal = plane.normal;
        byte signbits = plane.Signbits;
        int sides = 0;
        int i = signbits & 1;
        int j = (signbits >> 1) & 1;
        int k = (signbits >> 2) & 1;

        float dist1 = normal[0] * bounds[i ^ 1][0] + normal[1] * bounds[j ^ 1][1] + normal[2] * bounds[k ^ 1][2];
        float dist2 = normal[0] * bounds[i][0] + normal[1] * bounds[j][1] + normal[2] * bounds[k][2];

        if(dist1 >= plane.distance)
        {
            sides = 1;
        }
        if(dist2 < plane.distance)
        {
            sides |= 2;
        }

        return sides;
    }

    public static BSPLeaf NodePointSearch(BSPNode node, Vector3 point)
    {
        float d;
        int i = 0;
        BSPNode sourcenode = node;

        while(!(sourcenode is BSPLeaf))
        {
            i++;
            d = PlaneDiff(point, BSPFile.planes[sourcenode.plane]);

            if(d < 0)
            {
                sourcenode = sourcenode.back_node;
            }
            else
            {
                sourcenode = sourcenode.front_node;
            }
            if(i > 5000)
            {
                Console.DebugLog("node point doesnt seem to end");
                return null;
            }
        }

        return sourcenode as BSPLeaf;
    }

    public static float PlaneDiff(Vector3 v, BSPPlane p)
    {
        return Vector3.Dot(v, p.normal) - p.distance;
    }

    public static BrushContents GetPointContents(Vector3 point)
    {
        BSPLeaf l = NodePointSearch(BSPFile.headnode, point / Globals.scale.Value);
        return l.contents;
    }

    public static Color GetAverageTextureColor(Texture2D tex)
    {
        float r = 0;
        float g = 0;
        float b = 0;

        Color[] colors = tex.GetPixels();
        int count = colors.Length;

        foreach(Color c in colors)
        {
            r += c.r;
            g += c.g;
            b += c.b;
        }

        return new Color(r / count, g / count, b / count, 1.0f);
    }

    public static Vector2 GetUV(Vector3 p, BSPFace f)
    {
        float scale = Globals.scale.Value;
        if (f.BSPTexInfo.texfile_guid == Guid.Empty)
        {
            //no texture
            return new Vector2();
        }
        Vector2 dims = TextureLoader.GetTextureDimensions(f.BSPTexInfo.texfile_guid);

        float u = Vector3.Dot(p, f.BSPTexInfo.u_axis) + f.BSPTexInfo.u_offset;
        float v = Vector3.Dot(p, f.BSPTexInfo.v_axis) + f.BSPTexInfo.v_offset;

        //scale uvs by texture dimensions
        u /= dims.x * scale;
        v /= dims.y * scale;

        return new Vector2(u, v);
    }

    public static Vector2 GetLightmapUV(Vector3 p, BSPFace f)
    {
        float scale = Globals.scale.Value;

        Vector2 uv = new Vector2();

        uv.x = Vector3.Dot(p / scale, f.BSPTexInfo.u_axis) + f.BSPTexInfo.u_offset / scale;
        uv.y = Vector3.Dot(p / scale, f.BSPTexInfo.v_axis) + f.BSPTexInfo.v_offset / scale;

        float s = ((f.lightmap.width << 4) + 8) - f.lightmap.u_min;
        float t = ((f.lightmap.height << 4) + 8) - f.lightmap.v_min;

        uv.x += s;
        uv.y += t;

        uv.x *= 1f / (f.lightmap.width * 16f);
        uv.y *= 1f / (f.lightmap.height * 16f);
        return uv;
    }

    public static void AnglesToAxis(Vector3 angles, out Vector3 forward, out Vector3 right, out Vector3 up)
    {
        float angle;
        float sr, sp, sy, cr, cp, cy;

        angle = Mathf.Deg2Rad * angles[1]; //yaw
        sy = Mathf.Sin(angle);
        cy = Mathf.Cos(angle);
        angle = Mathf.Deg2Rad * angles[0]; //pitch
        sp = Mathf.Sin(angle);
        cp = Mathf.Cos(angle);
        angle = Mathf.Deg2Rad * angles[2]; //roll
        sr = Mathf.Sin(angle);
        cr = Mathf.Cos(angle);

        forward = new Vector3(cp * cy, cp *sy, -sp);
        right = new Vector3(( sr * sp * cy) + (cr * -sy), (sr * sp * sy) + (cr * cy), sr * cp);
        up = new Vector3((cr * sp * cy) + (-sr * -sy), (cr * sp * sy) + (-sr * cy), cr * cp);
    }

    public static void TransposeAxis(ref Vector3[] axes)
    {
        float temp;

        temp = axes[0][1];
        axes[0][1] = axes[1][0];
        axes[1][0] = temp;

        temp = axes[0][2];
        axes[0][2] = axes[2][0];
        axes[2][0] = temp;

        temp = axes[1][2];
        axes[1][2] = axes[2][1];
        axes[2][1] = temp;
    }

    public static Vector3 RotatePoint(Vector3 point, Vector3[] axes)
    {
        Vector3 v = new Vector3();
        v.x = Vector3.Dot(point, axes[0]);
        v.y = Vector3.Dot(point, axes[1]);
        v.z = Vector3.Dot(point, axes[2]);
        return v;
    }
}
