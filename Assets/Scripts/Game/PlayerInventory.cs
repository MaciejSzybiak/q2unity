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

using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

/*
 * Manages player's inventory.
 */

public class PlayerInventory : MonoBehaviour
{
    public List<WeaponTemplate> weaponList;
    public Transform playerCamera;

    private Weapon[] weaponSlots;
    private int currentWeapon = 0;

    private float weaponXOffset;

    ConfVar hand;

    #region singleton

    public static PlayerInventory Instance;

    private void Awake()
    {
        if (Instance)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }

        #endregion

        CommandManager.RegisterCommand("+fire", Fire_c, "fires the weapon if player has any", CommandType.normal);
        BindManager.SetBind(KeyCode.Mouse0, "+fire", BindType.hold, false);

        weaponSlots = new Weapon[Enum.GetNames(typeof(WeaponClassname)).Length];

#if DEBUG
        Cvar.Add("debug_rocketmult", "1");
        CommandManager.RegisterCommand("debug_giverocket", DebugGiveRocket, "debug only command for giving rocket launcher to player", CommandType.normal);
#endif
    }

#if DEBUG
    private void DebugGiveRocket(string[] args)
    {
        OnWeaponPickup(WeaponClassname.weapon_rocketlauncher);
    }
#endif

    private void Start()
    {
        PlayerState.OnWeaponPickup += OnWeaponPickup;
        PlayerState.itemResetEvent.AddListener(ClearWeapons);

        hand = Cvar.Get("hand", "1", OnCvarHandChange, CvarType.DEFAULT);
        OnCvarHandChange(hand);
    }

    private void Update()
    {
        foreach(Weapon w in weaponSlots)
        {
            if (w)
            {
                w.UpdateCooldown();
            }
        }
    }

    private void OnDestroy()
    {
        PlayerState.OnWeaponPickup -= OnWeaponPickup;
    }

    private void OnCvarHandChange(ConfVar c)
    {
        float scale = Globals.scale.Value;
        switch (c.Integer)
        {
            case 1:
                weaponXOffset = 11.4f * scale;
                break;
            case 2:
                weaponXOffset = -11.4f * scale;
                break;
            default:
                weaponXOffset = 0f;
                break;
        }

        float y, z;
        for(int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i])
            {
                y = weaponSlots[i].transform.localPosition.y;
                z = weaponSlots[i].transform.localPosition.z;
                weaponSlots[i].transform.localPosition = new Vector3(weaponXOffset, y, z);
            }
        }
    }

    public void ClearWeapons()
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i])
            {
                Destroy(weaponSlots[i].gameObject);
                weaponSlots[i] = null;

            }
        }
    }

    private void Fire_c(string[] args)
    {
        if(!Cvar.Boolean("console_open"))
        {
            if(args.Length > 0)
            {
                return;
            }

            Weapon w = weaponSlots[currentWeapon];

            if (!w)
            {
                return;
            }

            if (!w.IsCooldown)
            {
                float scale = Globals.scale.Value;

                Projectile p = Instantiate(w.WeaponTemplate.ProjectileModel);
                p.transform.position = w.transform.position + w.transform.forward * 0.2f;

                Vector3 far = (playerCamera.transform.position / scale) + playerCamera.transform.forward * 10000;
                TraceT farTrace = Trace.CL_Trace(playerCamera.transform.position / scale, Vector3.zero, Vector3.zero, far);

                if(farTrace.fraction < 1)
                {
                    p.transform.LookAt(farTrace.endpos * scale); //this will always happen if map is sealed and the player is inside the map volume
                }
                else
                {
                    p.transform.forward = w.transform.forward;
                }
                p.speed = w.WeaponTemplate.speed;
                p.damage = w.WeaponTemplate.damage;
                p.radiusDamage = w.WeaponTemplate.radiusDamage;
                p.damageRadius = w.WeaponTemplate.damageRadius;
                w.SetCooldown(w.WeaponTemplate.cooldown);
            }
        }
    }

    private void SetWeaponSlot(int slot)
    {
        currentWeapon = slot;
    }

    private void SetWeaponSlot(WeaponClassname weaponClassname)
    {
        SetWeaponSlot((int)weaponClassname);
    }

    private void OnWeaponPickup(WeaponClassname weaponClassname)
    {
        if (weaponSlots[(int)weaponClassname])
        {
            Console.DebugLog("Weapon already picked up.");
            return;
        }

        if(weaponList.Any(i => i.WeaponType == weaponClassname))
        {
            Console.DebugLog("Pickup " + weaponClassname.ToString() + "!");

            WeaponTemplate wt = weaponList.First(i => i.WeaponType == weaponClassname);

            Weapon w = Instantiate(wt.WeaponModel, playerCamera);
            w.WeaponTemplate = wt;

            float scale = Globals.scale.Value;
            w.transform.localPosition -= new Vector3(-weaponXOffset, 12.3f * scale, 0);

            weaponSlots[(int)weaponClassname] = w;

            SetWeaponSlot((int)weaponClassname);
        }
        else
        {
            Console.DebugLog("Weapon " + weaponClassname.ToString() + " not found!");
        }
    }
}
