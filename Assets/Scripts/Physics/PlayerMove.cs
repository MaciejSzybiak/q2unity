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
 * This large file is the heart of this project. 
 * A 1:1 rewrite of Quake 2 movement physics from
 * C to Unity scripts.
 */

public struct PlayerMoveData
{
    //in
    public Vector3 inOrigin;
    public Vector3 inVelocity;

    public Vector3 inForward;
    public Vector3 inRight;
    public float camX;
    public float camY;
    public Vector3 addVelocities;

    public float frametime;
    public float gravity;

    public bool initialSnap;

    //out
    public Vector3 outOrigin;
    public Vector3 outVelocity;
    public Vector3 mins;
    public Vector3 maxs;

    public float viewheight;

    public PMFlags flags;

    public bool jumped;
    public bool beginCameraLerp;
}

public static class PlayerMove
{
    //constants
    private const float pm_friction = 6f;
    private const float pm_waterfriction = 1f;
    private const float pm_flyfriction = 9f;

    private const float pm_maxspeed = 300f;
    private const float pm_speedmult = 1f;
    private const float pm_watermult = 0.5f;

    private const float pm_stopspeed = 100f;
    private const float pm_duckspeed = 100f;
    private const float pm_accelerate = 10f;
    private const float pm_wateraccelerate = 10f;
    private const float pm_waterspeed = 400f;

    private static Vector3 mins;
    private static Vector3 maxs;

    private static float viewheight;
    private static int waterlevel;
    private static BrushContents watertype;
    private static bool ladder;
    private static float timer = 0;

    private static PMFlags pmflags;

    private static bool groundentity = false;
    private static BSPPlane groundplane;
    private static SurfaceT groundsurface;
    private static BrushContents groundcontents;

    private static float frametime;

    private static PlayerMoveData pmd;

    private static Vector3 origin;
    private static Vector3 velocity;

    private static void FinishPmove()
    {
        if (!PlayerState.Noclip)
        {
            PM_SnapPosition();
        }
        else
        {
            pmd.outOrigin = origin * 8;
            pmd.outVelocity = velocity * 8;
        }

        pmd.flags = pmflags;
        pmd.mins = mins;
        pmd.maxs = maxs;
        pmd.viewheight = viewheight;
    }

    public static void Pmove(ref PlayerMoveData pmd)
    {
        //default
        mins = Vector3.zero;
        maxs = Vector3.zero;

        PlayerMove.pmd = pmd;

        groundplane = null;

        origin = pmd.inOrigin * 0.125f;
        velocity = pmd.inVelocity * 0.125f;
        frametime = pmd.frametime;
        pmflags = pmd.flags;

        if(pmd.addVelocities != Vector3.zero)
        {
            velocity += pmd.addVelocities;
        }

        if(PlayerState.mode == GameMode.flying)
        {
            PM_FlyMove();

            if (PlayerState.Noclip)
            {
                origin += velocity * frametime;
            }
            else
            {
                PM_StepSlideMove_();
            }
            FinishPmove();
            pmd = PlayerMove.pmd;
            return;
        }
        PM_CheckDuck();

        if (pmd.initialSnap)
        {
            PM_InitialSnapPosition();
        }

        PM_CategorizePosition();
        PM_CheckSpecialMovement();
        
        //drop timing counter
        if (timer > 0)
        {
            float msec = frametime;
            if (msec == 0)
            {
                //?
                msec = 1;
            }
            if (msec >= timer)
            {
                pmflags &= ~PMFlags.PMF_TIME_WATERJUMP;
                timer = 0;
            }
            else
            {
                timer -= msec;
            }
        }

        if (pmflags.HasFlag(PMFlags.PMF_TIME_WATERJUMP))
        {
            velocity.y -= PlayerState.pm_gravity * frametime;
            if (velocity.y < 0)
            {
                //cancel waterjump when we start falling
                pmflags &= ~PMFlags.PMF_TIME_WATERJUMP;
                timer = 0;
            }

            PM_StepSlideMove();
        }
        else
        {
            PM_CheckJump();
            PM_Friction();

            if (waterlevel >= 2)
            {
                PM_WaterMove();
            }
            else
            {
                PM_AirMove();
            }
        }
        PM_CategorizePosition();

        FinishPmove();
        pmd = PlayerMove.pmd;
    }

    private static void PM_InitialSnapPosition()
    {
        int x, y, z;
        Vector3 basev;
        int[] offset = { 0, -1, 1 };

        basev = origin;

        for(y = 0; y < 3; y++)
        {
            origin.y = basev.y + offset[y];
            for (z = 0; z < 3; z++)
            {
                origin.z = basev.z + offset[z];
                for (x = 0; x < 3; x++)
                {
                    origin.x = basev.x + offset[x];
                    if (PM_GoodPositionUnscaled(origin))
                    {
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates player position and tries to fix it if it's not valid.
    /// </summary>
    private static void PM_SnapPosition()
    {
        int[] sign = new int[3];
        int i, j, bits;
        Vector3 basee;
        byte[] jitterbits = { 0, 4, 1, 2, 3, 5, 6, 7 };
        Vector3 outorigin = new Vector3();

        for (i = 0; i < 3; i++)
        {
            pmd.outVelocity[i] = (int)(velocity[i] * 8);
        }

        for (i = 0; i < 3; i++)
        {
            sign[i] = origin[i] >= 0 ? 1 : -1;

            outorigin[i] = (int)(origin[i] * 8);

            if (outorigin[i] * 0.125f == origin[i])
            {
                sign[i] = 0;
            }
        }

        basee = outorigin;

        //try all combinations
        for (j = 0; j < 8; j++)
        {
            bits = jitterbits[j];
            outorigin = basee;
            for (i = 0; i < 3; i++)
            {
                if ((bits & (1 << i)) != 0)
                {
                    outorigin[i] += sign[i];
                }
            }

            if (PM_GoodPosition(outorigin))
            {
                pmd.outOrigin = outorigin;
                return;
            }
        }

        pmd.outOrigin = pmd.inOrigin;
    }

    /// <summary>
    /// Checks whether player is stuck in a wall.
    /// </summary>
    private static bool PM_GoodPosition(Vector3 pos)
    {
        pos *= 0.125f;
        return PM_GoodPositionUnscaled(pos);
    }

    private static bool PM_GoodPositionUnscaled(Vector3 pos)
    {
        TraceT trace;

        trace = Trace.CL_Trace(pos, mins, maxs, pos);

        return !trace.allsolid;
    }

    /// <summary>
    /// Sets player extents and viewheight.
    /// </summary>
    private static void PM_CheckDuck()
    {
        mins.x = -16;
        mins.z = -16;

        maxs.x = 16;
        maxs.z = 16;

        mins.y = -24;

        if (InputContainer.upMove < 0 && pmflags.HasFlag(PMFlags.PMF_ON_GROUND))
        {
            pmflags |= PMFlags.PMF_DUCKED;
            pmd.beginCameraLerp = true;
        }
        else
        {
            //stand up if possible
            if (pmflags.HasFlag(PMFlags.PMF_DUCKED))
            {
                //try to stand up
                maxs.y = 32;

                TraceT trace = Trace.CL_Trace(origin, mins, maxs, origin);
                if (!trace.allsolid)
                {
                    pmflags &= ~PMFlags.PMF_DUCKED;
                }
            }
        }

        if (pmflags.HasFlag(PMFlags.PMF_DUCKED))
        {
            maxs.y = 4;
            viewheight = -2;
        }
        else
        {
            maxs.y = 32;
            viewheight = 22;
        }
    }

    /// <summary>
    /// Checks if player is standing on the ground and if he is in a water body.
    /// </summary>
    private static void PM_CategorizePosition()
    {
        TraceT trace;
        BrushContents cont;
        int sample1, sample2;

        if (velocity.y > 180)
        {
            groundentity = false;
            pmflags &= ~PMFlags.PMF_ON_GROUND;
        }
        else
        {
            trace = Trace.CL_Trace(origin, mins, maxs, origin - new Vector3(0, 0.25f, 0));
            groundplane = trace.plane;
            groundsurface = trace.surface;
            groundcontents = trace.contents;

            if (trace.plane == null || (trace.plane.normal.y < 0.7f && !trace.startsolid))
            {
                groundentity = false;
                pmflags &= ~PMFlags.PMF_ON_GROUND;
            }
            else
            {
                groundentity = true;

                //hitting solid ground will end a waterjump
                if (pmflags.HasFlag(PMFlags.PMF_TIME_WATERJUMP))
                {
                    pmflags &= ~PMFlags.PMF_TIME_WATERJUMP;
                    timer = 0;
                }

                if (!pmflags.HasFlag(PMFlags.PMF_ON_GROUND))
                {
                    //just hit the ground
                    pmflags |= PMFlags.PMF_ON_GROUND;
                }
            }
        }

        //get waterlevel
        waterlevel = 0;
        watertype = 0;

        sample2 = (int)viewheight - (int)mins.y;
        sample1 = sample2 / 2;

        Vector3 point = origin;
        point.y = origin.y + mins.y + 1;
        cont = Utils.NodePointSearch(BSPFile.headnode, point).contents;

        if ((cont & BrushContents.MASK_WATER) != 0)
        {
            watertype = cont;
            waterlevel = 1;

            point.y = origin.y + mins.y + sample1;
            cont = Utils.NodePointSearch(BSPFile.headnode, point).contents;

            if ((cont & BrushContents.MASK_WATER) != 0)
            {
                waterlevel = 2;
                point.y = origin.y + mins.y + sample2;
                cont = Utils.NodePointSearch(BSPFile.headnode, point).contents;

                if ((cont & BrushContents.MASK_WATER) != 0)
                {
                    waterlevel = 3;
                }
            }
        }
    }

    /// <summary>
    /// Check if climbing a ladder or jumping out of water.
    /// </summary>
    private static void PM_CheckSpecialMovement()
    {
        Vector3 spot;
        BrushContents cont;
        Vector3 flatforward;
        TraceT trace;

        if (timer > 0)
        {
            return;
        }

        ladder = false;

        //check for ladder
        flatforward = pmd.inForward;
        flatforward.y = 0;
        flatforward.Normalize();

        spot = origin + flatforward * 1;
        trace = Trace.CL_Trace(origin, mins, maxs, spot);
        if ((trace.fraction < 1) && trace.contents.HasFlag(BrushContents.CONTENTS_LADDER))
        {
            ladder = true;
        }

        //check for waterjump
        if (waterlevel != 2)
        {
            return;
        }

        spot = origin + flatforward * 30;
        spot.y += 4;
        cont = Utils.NodePointSearch(BSPFile.headnode, spot).contents;
        if (!cont.HasFlag(BrushContents.CONTENTS_SOLID))
        {
            return;
        }

        spot.y += 16;
        cont = Utils.NodePointSearch(BSPFile.headnode, spot).contents;
        if (cont != 0)
        {
            return;
        }

        //jump out of water
        velocity = flatforward * 50;
        velocity.y = 350;

        pmflags |= PMFlags.PMF_TIME_WATERJUMP;
        timer = 0.255f;
    }

    /// <summary>
    /// Check if the player can jump.
    /// </summary>
    private static void PM_CheckJump()
    {
        if (InputContainer.upMove <= 0)
        {
            pmflags &= ~PMFlags.PMF_JUMP_HELD;
            return;
        }

        if (pmflags.HasFlag(PMFlags.PMF_JUMP_HELD))
        {
            return;
        }

        //swimming
        if (waterlevel >= 2)
        {
            groundentity = false;

            if (velocity.y <= -300)
            {
                return;
            }

            //FIXME: John wanted to fix this so...
            if (watertype == BrushContents.CONTENTS_WATER)
            {
                velocity.y = 100;
            }
            else if (watertype == BrushContents.CONTENTS_SLIME)
            {
                velocity.y = 80;
            }
            else
            {
                velocity.y = 50;
            }
            return;
        }

        //in air?
        if (!groundentity)
        {
            return;
        }

        pmflags |= PMFlags.PMF_JUMP_HELD;

        groundentity = false;
        pmflags &= ~PMFlags.PMF_ON_GROUND;

        velocity.y += 270;
        if (velocity.y < 270)
        {
            velocity.y = 270;
        }

        //play sound
        pmd.jumped = true;
    }

    /// <summary>
    /// Apply ground and water friction.
    /// </summary>
    private static void PM_Friction()
    {
        float speed, newspeed, control;
        float friction;
        float drop;

        speed = velocity.magnitude;

        if (speed < 1)
        {
            velocity.x = 0;
            velocity.z = 0;
            return;
        }

        drop = 0;

        //apply ground friction
        if (groundentity && (groundsurface.flags & (int)SurfFlags.SURF_SLICK) == 0 || ladder)
        {
            friction = pm_friction;
            control = speed < pm_stopspeed ? pm_stopspeed : speed;
            drop += control * friction * frametime;
        }

        //water friction
        if (waterlevel > 0 && !ladder)
        {
            drop += speed * pm_waterfriction * waterlevel * frametime;
        }

        //scale the velocity
        newspeed = speed - drop;
        if (newspeed < 0)
        {
            newspeed = 0;
        }
        newspeed /= speed;
        velocity *= newspeed;
    }

    private static void PM_FlyMove()
    {
        float speed, drop, friction, control, newspeed;
        float currentspeed, addspeed, accelspeed;
        Vector3 wishvel;
        float fmove, smove;
        Vector3 wishdir;
        float wishspeed;

        viewheight = 22;

        if (!PlayerState.Noclip)
        {
            mins.x = -16;
            mins.z = -16;

            maxs.x = 16;
            maxs.z = 16;

            mins.y = -24;
            maxs.y = 32;
        }

        //start with friction
        speed = velocity.magnitude;

        if (speed < 1)
        {
            velocity = Vector3.zero;
        }
        else
        {
            drop = 0;

            friction = pm_flyfriction;
            control = speed < pm_stopspeed ? pm_stopspeed : speed;
            drop += control * friction * frametime;

            newspeed = speed - drop;
            if (newspeed < 0)
            {
                newspeed = 0;
            }

            newspeed /= speed;

            velocity *= newspeed;
        }

        //acceleration
        float maxspeed = Globals.flySpeed.Value;

        fmove = InputContainer.forwardMove * maxspeed;
        smove = InputContainer.sideMove * maxspeed;

        wishvel = pmd.inForward * fmove + pmd.inRight * smove;
        wishvel.y += InputContainer.upMove * maxspeed;

        wishdir = wishvel.normalized;
        wishspeed = wishvel.magnitude;

        //clamp to maxspeed
        if (wishspeed > maxspeed)
        {
            wishvel *= maxspeed / wishspeed;
            wishspeed = maxspeed;
        }

        currentspeed = Vector3.Dot(velocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed > 0)
        {
            accelspeed = pm_accelerate * frametime * wishspeed;
            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            velocity += accelspeed * wishdir;
        }
    }

    /// <summary>
    /// Move underwater.
    /// </summary>
    private static void PM_WaterMove()
    {
        Vector3 wishvel = new Vector3();
        float wishspeed;
        Vector3 wishdir;

        Vector3 fwd = pmd.inForward;
        Vector3 rgt = pmd.inRight;
        float fmove = InputContainer.forwardMove * 300f;
        float smove = InputContainer.sideMove * 300f;
        float umove = InputContainer.upMove * 300f;

        //user intentions
        for (int i = 0; i < 3; i++)
        {
            wishvel[i] = fwd[i] * fmove + rgt[i] * smove;
        }

        if (fmove == 0 && smove == 0 && umove == 0)
        {
            wishvel.y -= 60;
        }
        else
        {
            wishvel.y += umove;
        }

        PM_AddCurrents(ref wishvel);

        wishdir = wishvel.normalized;
        wishspeed = wishvel.magnitude;

        if (wishspeed > pm_maxspeed)
        {
            wishvel *= pm_maxspeed / wishspeed;
            wishspeed = pm_maxspeed;
        }
        wishspeed *= pm_watermult;

        PM_Accelerate(wishdir, wishspeed, pm_wateraccelerate);

        PM_StepSlideMove();
    }

    /// <summary>
    /// Do ladder move.
    /// </summary>
    private static void PM_AddCurrents(ref Vector3 wishvel)
    {
        //account for ladders
        if (ladder && Mathf.Abs(velocity.y) <= 200)
        {
            if (-pmd.camX <= -15 && InputContainer.forwardMove > 0)
            {
                wishvel.y = 200;
            }
            else if (-pmd.camX >= 15 && InputContainer.forwardMove > 0)
            {
                wishvel.y = -200;
            }
            else if (InputContainer.upMove > 0)
            {
                wishvel.y = 200;
            }
            else if (InputContainer.upMove < 0)
            {
                wishvel.y = -200;
            }
            else
            {
                wishvel.y = 0;
            }

            wishvel.x = Mathf.Clamp(wishvel.x, -25, 25);
            wishvel.z = Mathf.Clamp(wishvel.z, -25, 25);
        }
    }

    /// <summary>
    /// Move in the air and on the ground.
    /// </summary>
    private static void PM_AirMove()
    {
        Vector3 wishvel = new Vector3();
        float fmove, smove;
        Vector3 wishdir;
        Vector3 flatforward, flatright;
        float wishspeed;
        float maxspeed;

        fmove = InputContainer.forwardMove * 300f;
        smove = InputContainer.sideMove * 300f;

        flatforward = pmd.inForward;
        flatforward.y = 0;
        flatforward.Normalize();

        flatright = pmd.inRight;
        flatright.y = 0;
        flatright.Normalize();

        wishvel.x = flatforward.x * fmove + flatright.x * smove;
        wishvel.z = flatforward.z * fmove + flatright.z * smove;
        wishvel.y = 0;

        PM_AddCurrents(ref wishvel);

        wishdir = new Vector3(wishvel.x, wishvel.y, wishvel.z);
        wishspeed = wishdir.magnitude;
        wishdir.Normalize();

        //clamp to maxspeed
        maxspeed = pmflags.HasFlag(PMFlags.PMF_DUCKED) ? pm_duckspeed : pm_maxspeed;

        if (wishspeed > maxspeed)
        {
            wishvel *= maxspeed / wishspeed;
            wishspeed = maxspeed;
        }

        //ladderstuff
        if (ladder)
        {
            PM_Accelerate(wishdir, wishspeed, pm_accelerate);
            if (wishvel.y == 0)
            {
                if (velocity.y > 0)
                {
                    velocity.y -= PlayerState.pm_gravity * frametime;
                    if (velocity.y < 0)
                    {
                        velocity.y = 0;
                    }
                }
                else
                {
                    velocity.y += PlayerState.pm_gravity * frametime;
                    if (velocity.y > 0)
                    {
                        velocity.y = 0;
                    }
                }
            }
            PM_StepSlideMove();
        }
        else if (groundentity)
        {
            //walking on ground
            velocity.y = 0;
            PM_Accelerate(wishdir, wishspeed, pm_accelerate);

            //fix for negative gravity here      
            PM_StepSlideMove();
        }
        else
        {
            PM_Accelerate(wishdir, wishspeed, 1);

            //add gravity
            velocity.y -= PlayerState.pm_gravity * frametime;

            PM_StepSlideMove();
        }
    }

    /// <summary>
    /// Adds user intented velocity.
    /// </summary>
    private static void PM_Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed, accelspeed, currentspeed;

        currentspeed = Vector3.Dot(velocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
        {
            return;
        }
        accelspeed = accel * frametime * wishspeed;

        if (accelspeed > addspeed)
        {
            accelspeed = addspeed;
        }

        velocity += accelspeed * wishdir;
    }

    /// <summary>
    /// Moves the player along velocity vector and try to step up.
    /// </summary>
    private static void PM_StepSlideMove()
    {
        Vector3 start_o, start_v;
        Vector3 down_o, down_v;
        TraceT trace;
        float down_dist, up_dist;
        Vector3 up, down;

        start_o = origin;
        start_v = velocity;

        PM_StepSlideMove_();

        down_o = origin;
        down_v = velocity;

        up = start_o;
        up += new Vector3(0, 18, 0);

        trace = Trace.CL_Trace(up, mins, maxs, up);
        if (trace.allsolid)
        {
            return; //can't step up
        }

        origin = up;
        velocity = start_v;

        PM_StepSlideMove_();

        //push down the final amount
        down = origin;
        down.y -= 18;
        trace = Trace.CL_Trace(origin, mins, maxs, down);
        if (!trace.allsolid)
        {
            origin = trace.endpos;
        }

        up = origin;

        down_dist = Mathf.Pow(down_o.x - start_o.x, 2) + Mathf.Pow(down_o.z - start_o.z, 2);
        up_dist = Mathf.Pow(up.x - start_o.x, 2) + Mathf.Pow(up.z - start_o.z, 2);

        if (down_dist > up_dist || trace.plane == null || trace.plane.normal.y < 0.7f)
        {
            origin = down_o;
            velocity = down_v;

            return;
        }

        //special case blah blah
        velocity.y = down_v.y;

        pmd.beginCameraLerp = true;
    }

    /// <summary>
    /// Moves the player along velocity vector while clipping it by the collided level geometry.
    /// </summary>
    private static void PM_StepSlideMove_()
    {
        int bumpcount, numbumps;
        Vector3 dir;
        float d;
        int numplanes;
        Vector3[] planes = new Vector3[5];
        Vector3 primal_velocity;
        int i, j;
        TraceT trace;
        Vector3 end;
        float time_left;

        numbumps = 4;

        primal_velocity = velocity;
        numplanes = 0;

        time_left = frametime;

        for (bumpcount = 0; bumpcount < numbumps; bumpcount++)
        {
            end = origin + time_left * velocity;

            trace = Trace.CL_Trace(origin, mins, maxs, end);

            if (trace.allsolid)
            {
                //entity is trapped in another solid
                velocity.y = 0;
                return;
            }

            if (trace.fraction > 0)
            {
                //actually covered some distance
                origin = trace.endpos;
                numplanes = 0;
            }

            if (trace.fraction == 1)
            {
                break;
            }

            //entity touch blah blah

            time_left -= time_left * trace.fraction;

            if (numplanes >= 5)
            {
                //this shouldn't really happen
                velocity = Vector3.zero;
                break;
            }

            for (i = 0; i < numplanes; i++)
            {
                if (planes[i] != null && Vector3.Dot(trace.plane.normal, planes[i]) > 0.99f)
                {
                    velocity += trace.plane.normal;
                    break;
                }
            }
            if (i < numplanes)
            {
                continue;
            }

            planes[numplanes] = trace.plane.normal;
            numplanes++;

            //modify original velocity so it parallels all of the clip planes
            for (i = 0; i < numplanes; i++)
            {
                PM_ClipVelocity(velocity, planes[i], out velocity, 1.01f);
                for (j = 0; j < numplanes; j++)
                {
                    if (j != i)
                    {
                        if (Vector3.Dot(velocity, planes[j]) < 0)
                        {
                            break; //not ok (lol?)
                        }
                    }
                }
                if (j == numplanes)
                {
                    break;
                }
            }

            if (i != numplanes)
            {
                //go along this plane
            }
            else
            {
                //go along the crease
                if (numplanes != 2)
                {
                    velocity = Vector3.zero;
                    break;
                }

                dir = Vector3.Cross(planes[0], planes[1]);
                d = Vector3.Dot(dir, velocity);
                velocity = dir * d;
            }

            if (Vector3.Dot(velocity, primal_velocity) <= 0)
            {
                velocity = Vector3.zero;
                break;
            }
        }
    }

    /// <summary>
    /// Clips input vector by the plane normal.
    /// </summary>
    private static void PM_ClipVelocity(Vector3 input, Vector3 normal, out Vector3 output, float overbounce)
    {
        float backoff;
        float change;

        backoff = Vector3.Dot(input, normal);

        if (backoff < 0)
        {
            backoff *= overbounce;
        }
        else
        {
            backoff /= overbounce;
        }

        Vector3 ot = new Vector3();

        for (int i = 0; i < 3; i++)
        {
            change = normal[i] * backoff;
            ot[i] = input[i] - change;
        }

        output = ot;
    }
}
