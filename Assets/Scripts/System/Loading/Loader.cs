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

using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * This script manages the map file loading process and
 * game path settings.
 */

public class Loader : MonoBehaviour
{
    [Header("Prefabs")]
    public GameModel gameModel;

    [Header("World scale")]
    public float scale = 0.015625f;

    [Header("Files")]
    public string gamepath = @"C:\Quake2";
    public string modname = "baseq2";
    public string mapname = "q2unityt1";

    public SkyboxManager SkyboxManager;
    private List<GameObject> instantiatedMapObjects;
    private List<GameModel> instantiatedModelComponents;
    private List<Guid> mapTextures;

    public static Loader Instance;

    private void Awake()
    {
        Globals.scale = Cvar.Get("scale", scale.ToString(), null, CvarType.STATIC);
        BSPFile.OnMapLoadedEvent += OnMapLoaded;

        if (Instance)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        Globals.validgamepath = Cvar.Get("validgamepath", "0", null, CvarType.HIDDEN);
        Globals.validmodname = Cvar.Get("validmodname", "0", null, CvarType.HIDDEN);

        Globals.gamepath = Cvar.Get("gamepath", gamepath, OnCvarGamepathSet, CvarType.DEFAULT);
        Globals.modname = Cvar.Get("modname", modname, OnCvarModnameSet, CvarType.DEFAULT);
        OnCvarGamepathSet(Globals.gamepath);
        OnCvarModnameSet(Globals.modname);

        Globals.map = Cvar.Get("map", mapname, null, CvarType.HIDDEN);
        Cvar.Add("maploaded", "0", null, CvarType.HIDDEN);

        CommandManager.RegisterCommand("map", Map_c, "loads a map file. Format: map <mapname> <optional modname>.", CommandType.normal);
        CommandManager.RegisterCommand("writeconf", ConfigManager.SaveConfig, "writes a config file. Format: writeconf <optional config name>", CommandType.normal);
        CommandManager.RegisterCommand("loadconf", ConfigManager.LoadConfig, "loads a config file. Format: loadconf <optional config name>", CommandType.normal);
        CommandManager.RegisterCommand("printmapinfo", BSPFile.Printmapinfo_c, "prints information about the current map", CommandType.normal);
    }

    private void OnCvarGamepathSet(ConfVar c)
    {
        c.NoEventSet(c.Text.Replace('\\', '/'));
        if(c.Text.EndsWith("/"))
        {
            c.NoEventSet(c.Text.Substring(0, c.Text.Length - 1));
        }

        if (!Directory.Exists(c.Text))
        {
            Console.LogError("Gamepath folder " + c.Text + " doesnt exits!");
            Cvar.Set("validgamepath", 0);
        }
        else
        {
            Cvar.Set("validgamepath", 1);
            Console.DebugLog("Game folder seems valid.");

            Cvar.WeakGet("modname", out ConfVar modname);
            if (modname != null)
            {
                OnCvarModnameSet(modname);
                if (!Utils.IsModnameValid())
                {
                    Console.DebugLog("OnCvarGamepathSet is restarting resource loader...");
                    ResourceLoader.Init();
                    if (MenuManager.Instance)
                    {
                        MenuManager.Instance.GenerateMapPanel();
                    }
                }
            }
        }
    }

    private void OnCvarModnameSet(ConfVar c)
    {
        string mod = Globals.modname.Text;
        string fullpath = Globals.gamepath.Text + "/" + mod + "/";

        if (!Directory.Exists(fullpath))
        {
            Console.LogError("Mod folder " + mod + " doesnt exits!");
            Cvar.Set("validmodname", 0);
        }
        else
        {
            Cvar.Set("validmodname", 1);
            Console.DebugLog("Modname seems valid.");

            if (Cvar.Exists("gamepath"))
            {
                Console.DebugLog("OnCvarModnameSet is restarting resource loader...");
                ResourceLoader.Init();
                if (MenuManager.Instance)
                {
                    MenuManager.Instance.GenerateMapPanel();
                }
            }
        }
    }

    private void Map_c(string[] args)
    {
        if (args.Length == 0 || args.Length > 2)
        {
            Console.Log("Map is " + Globals.map.Text);
            return;
        }
        else if (args.Length > 2)
        {
            CommandManager.LogBadArgcount("Map", args.Length);
            return;
        }
        if (args.Length > 0)
        {
            Globals.map.Text = args[0];
        }
        if (args.Length == 2)
        {
            Globals.modname.Text = args[1];
        }

        LoadMap();
    }

    public void LoadMap()
    {
        if (!Utils.IsGamePathValid())
        {
            Console.LogError("Gamepath must be fixed before loading a map.");
            return;
        }
        if (!Utils.IsModnameValid())
        {
            Console.LogError("Modname should be fixed before loading a map.");
        }

        ClearMap();

        //load a map file...
        StateManager.ChangeGameState(GameState.loading);

        StopAllCoroutines();
        StartCoroutine(BSPFile.LoadBspRoutine(Globals.map.Text));
    }

    private void OnMapLoaded(bool success)
    {
        if (!success)
        {
            Console.DebugLog("weird");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ProcessMapRoutine());
    }

    private IEnumerator ProcessMapRoutine()
    {
        Console.LogInfo("Process map:");

        //load textures
        Console.Log("- load textures");
        yield return new WaitForEndOfFrame();

        LoadMapTextures();

        //generate world models
        Console.Log("- generate models");
        yield return new WaitForEndOfFrame();

        GenerateGameModels();
        ConnectEntityTargets();

        //skybox
        Console.Log("- skybox");
        yield return new WaitForSeconds(1f);

        if (!BSPFile.worldspawn.strings.TryGetValue("sky", out string skyname))
        {
            skyname = "unit1_";
        }
        SkyboxManager.LoadSkyboxForMap(skyname);


        Cvar.Set("maploaded", "1");

        //replay
        //ReplayManager.Instance.LoadReplay();

        //reset player position and state
        PlayerState.ResetPlayerState();
        CommandManager.RunCommand("recall", null);
        CommandManager.RunCommand("remstore", null);

        //hide menu
        //MenuManager.Instance.Deactivate();
        StateManager.ChangeGameState(GameState.singleplayer);

        Console.LogInfo("Map loaded!");
    }

    public void ClearMap()
    {
        Cvar.Set("maploaded", "0");

        //clear bmodels and md2s
        if(instantiatedMapObjects != null)
        {
            for(int i = 0; i < instantiatedMapObjects.Count; i++)
            {
                Destroy(instantiatedMapObjects[i]);
            }
            instantiatedMapObjects.Clear();
        }
        else
        {
            instantiatedMapObjects = new List<GameObject>();
        }

        if(instantiatedModelComponents != null)
        {
            instantiatedModelComponents.Clear();
        }
        else
        {
            instantiatedModelComponents = new List<GameModel>();
        }

        if (mapTextures != null)
        {
            for (int i = 0; i < mapTextures.Count; i++)
            {
                TextureLoader.UnloadTexture(mapTextures[i]);
            }
        }
        else
        {
            mapTextures = new List<Guid>();
        }

        SkyboxManager.UnloadSkyboxData();
    }

    private void GenerateGameModels()
    {
        GameModel worldspawn = Instantiate(gameModel, transform);
        worldspawn.CreateModel(BSPFile.models[0], BSPFile.ents.First(i => i.Classname.StartsWith("worldspawn")));
        instantiatedMapObjects.Add(worldspawn.gameObject);
        instantiatedModelComponents.Add(worldspawn);


        foreach (BSPEnt e in BSPFile.ents)
        {
            //ignore worldspawn
            if (e.Classname == "worldspawn")
            {
                continue;
            }

            GameModel m = Instantiate(gameModel, transform);

            if (e.strings.TryGetValue("model", out string val) &&
                val.Length >= 2 && int.TryParse(val.Substring(1, val.Length - 1), out int num))
            {
                //create bmodel
                m.CreateModel(BSPFile.models[num], e);
            }
            else
            {
                //no bmodel
                m.CreateModel(null, e);
            }
            instantiatedMapObjects.Add(m.gameObject);
            instantiatedModelComponents.Add(m);
        }

        //upload to BSPFile for physics testing
        BSPFile.physicsModels = new List<GameModel>();
        BSPFile.physicsModels.AddRange(instantiatedMapObjects.Select(i => i.GetComponent<GameModel>()).Where(i => i.clipTest).ToArray()); //FIXME: yikes...
        Console.DebugLog("Physics models count: " + BSPFile.physicsModels.Count);
    }

    private void ConnectEntityTargets()
    {
        GameModel m;
        GameModel[] targetnames;
        for(int i = 0; i < instantiatedModelComponents.Count; i++)
        {
            m = instantiatedModelComponents[i];

            if(m.Ent.Target != null)
            {
                if(instantiatedModelComponents.Any(t => t.Ent.Targetname == m.Ent.Target))
                {
                    targetnames = instantiatedModelComponents.Where(t => t.Ent.Targetname == m.Ent.Target).ToArray();
                    m.Ent.targetEntities = targetnames;
                    Console.DebugLog("Pair target \"" + m.Ent.Target + "\" with " + targetnames.Length + " ents");
                }
                else
                {
                    Console.DebugLog("Couldn't find a target for " + m.Ent.Target + " in entity " + m.Ent.Classname);
                }
            }
        }
    }

    private void LoadMapTextures()
    {
        TextureLoader.LoadColormapFromPak();

        string name;

        for (int i = 0; i < BSPFile.texinfo.Count; i++)
        {
            name = "textures/" + BSPFile.texinfo[i].TexString;
            name += ".wal";

            Guid g = TextureLoader.LoadTexture(name);
            BSPFile.texinfo[i].texfile_guid = g;
            mapTextures.Add(g);
        }
    }

    private BrushContents GetPointContents(Vector3 point)
    {
        BSPLeaf l = Utils.NodePointSearch(BSPFile.headnode, point / scale);
        return l.contents;
    }
}
