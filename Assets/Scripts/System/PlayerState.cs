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
using UnityEngine;
using UnityEngine.Events;

public enum GameMode
{
    flying, spectating, training, timeattack
}

public static class PlayerState
{
    //weapon pickup
    public delegate void PickupWeapon(WeaponClassname weaponType);
    public static PickupWeapon OnWeaponPickup;
    public static UnityEvent itemResetEvent = new UnityEvent();

    public static UnityEvent teleportEvent = new UnityEvent();
    public static Vector3 teleportDestination = Vector3.zero;
    public static Vector3 teleportAngles = Vector3.zero;

    //pmove state
    public static float currentSpeed = 0;
    public static float currentVieweight = 0;
    public static Vector3 currentOrigin = Vector3.zero;
    public static Vector3 mins = Vector3.zero;
    public static Vector3 maxs = Vector3.zero;
    public static float pm_gravity = 800;

    public static Vector3 addVelocities = Vector3.zero;

    public static Vector3 spawnOrigin = Vector3.zero;
    public static Vector3 spawnAngles = Vector3.right;

    public static GameMode mode = GameMode.flying;
    public static PMFlags moveFlags;

    //replay stuff??
    private static GameMode preReplayMode;
    private static bool replaying;
    public static bool Replaying
    {
        get
        {
            return replaying;
        }
        set
        {
            if(value == false)
            {
                mode = preReplayMode;
            }
            else
            {
                preReplayMode = mode;
            }
            replaying = value;
        }
    }

    private static bool noclip;
    public static bool Noclip
    {
        get
        {
            return mode == GameMode.flying ? noclip : false; //noclip only in fly mode
        }
        set
        {
            noclip = value;
        }
    }

    public static bool snapInitial; //to be used in the future

    public static void GameMode_c(string[] args)
    {
        GameMode gamemode = mode;
        if(args.Length != 1)
        {
            Console.LogInfo("Gamemode is \"" + mode.ToString() + "\"");
            return;
        }
        if(Enum.TryParse(args[0], out GameMode gm))
        {
            //no spectator implemented yet
            if(gm != GameMode.spectating)
            {
                mode = gm;
            }
            else
            {
                Console.DebugLog("I: spectator not yet implemented.");
                return;
            }
        }
        else if(int.TryParse(args[0], out int num))
        {
            if(num >= 0 && num < 4)
            {
                if (gm != GameMode.spectating)
                {
                    mode = (GameMode)num;
                }
                else
                {
                    Console.DebugLog("I: spectator not yet implemented.");
                    return;
                }                
            }
            else
            {
                Console.LogWarning("Incorrect game mode: " + args[0]);
                return;
            }
        }
        else
        {
            Console.LogWarning("Incorrent game mode: " + args[0]);
            return;
        }

        Console.LogInfo("Game mode " + mode.ToString().ToUpper());

        //restart player if timeattack is started
        if(mode == GameMode.timeattack)
        {
            CommandManager.RunCommand("recall", null);
        }
    }

    public static void Noclip_c(string[] args)
    {
        //toggle noclip
        int val = Cvar.Boolean("noclip") ? 0 : 1;
        Cvar.Set("noclip", val);
        Console.LogInfo("Noclip set to " + (val != 0 ? "ON" : "OFF"));
    }

    public static void OnCvarNoclipSet(ConfVar v)
    {
        noclip = v.Boolean;
    }

    public static void RequestTeleport(Vector3 target, Vector3 angles)
    {
        if(mode == GameMode.flying || mode == GameMode.spectating)
        {
            return;
        }

        teleportDestination = target;
        teleportAngles = angles;
        Console.DebugLog("Requested teleport angle: " + angles.ToString());

        teleportEvent?.Invoke();
    }

    public static void ResetPlayerItems()
    {
        itemResetEvent?.Invoke();
    }

    /// <summary>
    /// On map load.
    /// </summary>
    public static void ResetPlayerState()
    {
        ResetPlayerItems();

        mode = GameMode.flying;
        teleportDestination = Vector3.zero;
        teleportAngles = Vector3.zero;

        currentSpeed = 0;
        pm_gravity = 800;
    }
}
