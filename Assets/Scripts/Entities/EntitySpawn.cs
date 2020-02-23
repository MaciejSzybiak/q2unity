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
using UnityEngine;

/*
 * Entity "factory". Creates entities from loaded models.
 */

public class EntitySpawn : MonoBehaviour
{
    public List<EntityPacket> packets = new List<EntityPacket>();

    private delegate void EntitySpawner(GameModel model);

    private class EntityEntry
    {
        public string classname;                //entity name
        public EntitySpawner entitySpawner;     //spawner method for this entity

        public EntityEntry(string classname, EntitySpawner entitySpawner)
        {
            this.classname = classname;
            this.entitySpawner = entitySpawner;
        }
    }

    #region singleton

    public static EntitySpawn Instance
    {
        get;
        private set;
    }

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
    }

    #endregion

    private readonly List<EntityEntry> entries = new List<EntityEntry>()
    {
        new EntityEntry("worldspawn", null),
        new EntityEntry("info_player_start", InfoPlayerstartSpawn),
        new EntityEntry("info_player_deathmatch", InfoPlayerstartSpawn),
        new EntityEntry("weapon_finish", TriggerFinishSpawn),
        new EntityEntry("weapon_railgun", WeaponRailgunSpawn),
        new EntityEntry("weapon_rocketlauncher", WeaponRocketlauncherSpawn),
        new EntityEntry("light", null),
        new EntityEntry("func_rotating", FuncRotatingSpawn),
        new EntityEntry("func_plat", FuncPlatSpawn),
        new EntityEntry("func_wall", GenericClipModelSpawn),
        new EntityEntry("func_door", GenericClipModelSpawn),
        new EntityEntry("func_door_rotating", GenericClipModelSpawn),
        new EntityEntry("func_train", GenericClipModelSpawn),
        new EntityEntry("trigger_teleport", TriggerTeleportSpawn),
        new EntityEntry("misc_teleporter", MiscTeleporterSpawn),
        new EntityEntry("trigger_gravity", TriggerGravitySpawn),
        new EntityEntry("misc_teleporter_dest", null)
    };

    public void SpawnEntity(GameModel model)
    {
        EntityEntry e = entries.FirstOrDefault(i => i.classname == model.Ent.Classname);

        if (e != null)
        {
            //call the spawn function
            e.entitySpawner?.Invoke(model);
        }
        else
        {
            Console.LogWarning("No definition for entity " + model.Ent.Classname + ".");
        }
    }

    private static void AddMeshCollider(GameModel model)
    {
        MeshCollider c = model.gameObject.AddComponent<MeshCollider>();
        c.convex = true;
        c.isTrigger = true;
    }

    private static void AddBoxCollider(GameModel model, Vector3 size)
    {
        BoxCollider b = model.gameObject.AddComponent<BoxCollider>();
        b.center = Vector3.zero;
        b.size = size * Globals.scale.Value;
        b.isTrigger = true;
    }

    private static void OffsetModelToEntityOrigin(GameModel model)
    {
        model.transform.position = model.Ent.Origin;
        model.Ent.MoveOrigin = model.Ent.Origin;
        model.Ent.MoveAngles = model.Ent.Angles;
    }

    private static void HideEntBmodels(GameModel model)
    {
        MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();

        for(int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    private static bool TryGetEntityPacket(string classname, out EntityPacket packet)
    {
        //not needed?
        if (!Instance)
        {
            packet = null;
            return false;
        }
        packet = Instance.packets.FirstOrDefault(i => i.classname == classname);
        return packet != null;
    }

    private static bool TryGetWeaponModel(string classname, out Weapon weapon)
    {
        if (!PlayerInventory.Instance)
        {
            weapon = null;
            return false;
        }

        WeaponTemplate t = PlayerInventory.Instance.weaponList.FirstOrDefault(i => i.WeaponType.ToString() == classname);
        if (t)
        {
            weapon = t.WeaponModel;
            return true;
        }
        weapon = null;
        return false;
    }

    #region spawners

    private static void GenericClipModelSpawn(GameModel model)
    {
        model.clipTest = true;
    }

    private static void FuncPlatSpawn(GameModel model)
    {
        model.clipTest = true;
    }

    private static void FuncRotatingSpawn(GameModel model)
    {
        model.clipTest = true;

        BSPEnt e = model.Ent;

        e.SetThinkAndTouch(EntityThink.FuncRotatingThink, null);
    }

    private static void InfoPlayerstartSpawn(GameModel model)
    {
        PlayerState.spawnOrigin = model.Ent.Origin;

        if (model.Ent.Angles != null)
        {
            PlayerState.spawnAngles = model.Ent.Angles;
        }
        else
        {
            PlayerState.spawnAngles = new Vector3(0, model.Ent.Angle, 0);
        }
    }

    private static void TriggerGravitySpawn(GameModel model)
    {
        if(model.Ent.Spawnflags != 3)
        {
            //inactive
            return;
        }

        model.Ent.SetThinkAndTouch(null, EntityTouch.TriggerGravityTouch);
        AddMeshCollider(model);
    }

    private static void MiscTeleporterSpawn(GameModel model)
    {
        model.Ent.SetThinkAndTouch(null, EntityTouch.TriggerTeleportTouch);
        AddBoxCollider(model, new Vector3(64, 48, 64));

        //move to origin
        model.transform.position = model.Ent.Origin;

        //add particle system
        if(TryGetEntityPacket(model.Ent.Classname, out EntityPacket p))
        {
            if (p.particleSystem)
            {
                ParticleSystem particles = Instantiate(p.particleSystem, model.transform);
                model.entParticles = particles;
                model.entParticles.transform.localScale *= Globals.scale.Value * 100; //keep size constant...
                model.entParticles.transform.position += Vector3.down * 28 * Globals.scale.Value;
            }
        }
    }

    private static void TriggerTeleportSpawn(GameModel model)
    {
        model.Ent.SetThinkAndTouch(null, EntityTouch.TriggerTeleportTouch);
        AddMeshCollider(model);
        HideEntBmodels(model);
    }

    private static void TriggerFinishSpawn(GameModel model)
    {
        model.Ent.SetThinkAndTouch(EntityThink.TriggerFinishThink, EntityTouch.TriggerFinishTouch);
        AddMeshCollider(model);
    }

    private static void WeaponRailgunSpawn(GameModel model)
    {
        model.Ent.SetThinkAndTouch(EntityThink.GenericWeaponThink, EntityTouch.TriggerFinishTouch);
        AddBoxCollider(model, Vector3.one * 32);

        //move to origin
        OffsetModelToEntityOrigin(model);

        //set entity model
        if(TryGetWeaponModel(model.Ent.Classname, out Weapon w))
        {
            Weapon wpn = Instantiate(w, model.transform);
            model.entModel = wpn.gameObject;
        }
    }

    private static void WeaponRocketlauncherSpawn(GameModel model)
    {
        model.Ent.SetThinkAndTouch(EntityThink.GenericWeaponThink, EntityTouch.WeaponRocketlauncherTouch);
        AddBoxCollider(model, Vector3.one * 32);

        OffsetModelToEntityOrigin(model);

        //set entity model
        if (TryGetWeaponModel(model.Ent.Classname, out Weapon w))
        {
            Weapon wpn = Instantiate(w, model.transform);
            model.entModel = wpn.gameObject;
        }
    }

    #endregion

    private static void DebugEntityStrings(BSPEnt e)
    {
#if DEBUG
        Console.DebugLog("Logging entity strings for " + e.Classname);
        foreach (var s in e.strings)
        {
            Console.Log("> " + s.Key + " = " + s.Value);
        }
    }
#endif
}
