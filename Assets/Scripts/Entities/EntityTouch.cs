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
 * Stores entity touch methods.
 */

public delegate void EntityTouchMethod(GameModel model, Collider other);

public static class EntityTouch
{
    private static bool IgnoreTriggerTouch()
    {
        return PlayerState.mode == GameMode.flying || PlayerState.mode == GameMode.spectating;
    }

    public static void TriggerFinishTouch(GameModel model, Collider other)
    {
        RunTimer.Instance.EndRun();
    }

    public static void TriggerTeleportTouch(GameModel model, Collider other)
    {
        if (model.Ent.targetEntities != null && model.Ent.targetEntities.Length > 0)
        {
            Console.DebugLog("touch trigger tele, target is " + model.Ent.targetEntities[0].Ent.Origin.ToString());

            float scale = Globals.scale.Value;
            PlayerState.RequestTeleport(model.Ent.targetEntities[0].Ent.Origin / scale, model.Ent.targetEntities[0].Ent.Angles);
            
        }
        else
        {
            Console.DebugLog("touch trigger tele, but it has no target");
        }
    }

    public static void TriggerGravityTouch(GameModel model, Collider other)
    {
        if(IgnoreTriggerTouch())
        {
            return;
        }
        Console.DebugLog("trigger_gravity: setting gravity to " + model.Ent.Gravity);
        PlayerState.pm_gravity = model.Ent.Gravity;
    }

    //weapons
    public static void WeaponRocketlauncherTouch(GameModel model, Collider other)
    {
        if (IgnoreTriggerTouch())
        {
            return;
        }

        PlayerState.OnWeaponPickup?.Invoke(WeaponClassname.weapon_rocketlauncher);
    }
}
