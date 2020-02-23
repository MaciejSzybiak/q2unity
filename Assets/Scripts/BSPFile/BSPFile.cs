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
using System.IO;
using System;
using System.Linq;
using UnityEngine;
using System.Collections;

/*
 * The monstrous class used for loading BSP files.
 * This is hardly a decent code but it does the job.
 * 
 * Levels are loaded using coroutines to make the game
 * more responsive while loading.
 */

/// <summary>
/// Loads and keeps BSP file data.
/// </summary>
public static class BSPFile
{
    //FIXME: mixing structs, classes, lists and arrays..?
    public static BSPHeader header;
    public static Vector3[] verts;
    public static List<BSPEdge> edges;
    public static List<BSPFace> faces;
    public static List<BSPFaceEdge> faceedges;
    public static List<BSPTexInfo> texinfo;
    public static List<BSPEnt> ents;
    public static List<LightmapTex> lightmaps;
    public static BSPPlane[] planes;

    public static BSPBrush[] brushes;
    public static BSPBrushSide[] brushsides;

    public static BSPNode[] nodes;
    public static BSPLeaf[] leaves;

    public static ushort[] leaffaces;
    public static BSPBrush[] leafbrushes;

    public static BSPModel[] models;

    public static BSPNode headnode;
    public static BSPEnt worldspawn;

    public static List<GameModel> physicsModels;

    private static float scale;

    public static int GetLumpOffset(BSPLumps lump)
    {
        return (int)header.lumps[(int)lump].fileofs;
    }

    public static int GetLumpLength(BSPLumps lump)
    {
        return (int)header.lumps[(int)lump].filelen;
    }

    public static void Clear()
    {
        verts = null;
        edges?.Clear();
        faces?.Clear();
        faceedges?.Clear();
        texinfo?.Clear();
        ents?.Clear();
        lightmaps?.Clear();
        planes = null;

        brushes = null;
        brushsides = null;

        nodes = null;
        leaves = null;
        leaffaces = null;

        physicsModels?.Clear();
    }

    public delegate void MapLoadedEvent(bool success);
    public static MapLoadedEvent OnMapLoadedEvent;

    //map loading starts here
    public static IEnumerator LoadBspRoutine(string mapname)
    {
        Clear(); //free up resources

        scale = Globals.scale.Value;

        //get map bytes
        if (!ResourceLoader.LoadFile("maps/" + mapname + ".bsp", out byte[] rawdata))
        {
            Console.LogWarning("Map " + "maps/" + mapname + ".bsp not found!");
            OnMapLoadedEvent?.Invoke(false);
            yield break;
        }

        BinaryReader reader = new BinaryReader(new MemoryStream(rawdata));

        //load header
        header = new BSPHeader
        {
            ident = reader.ReadUInt32(),
            version = reader.ReadUInt32(),
            lumps = new List<BSPLump>()
        };

        //validate header
        if (!ValidateHeader())
        {
            reader.Close();
            OnMapLoadedEvent?.Invoke(false);
            yield break;
        }

        //load lumps
        LoadLumps(ref reader);

        //validate lumps
        if (!ValidateLumps(reader.BaseStream.Length))
        {
            reader.Close();
            OnMapLoadedEvent?.Invoke(false);
            yield break;
        }

        Console.LogInfo("Loading map:");

        Console.Log("- geometry");

        //load lump contents
        LoadVerts(ref reader);
        yield return new WaitForEndOfFrame();
        LoadPlanes(ref reader);
        yield return new WaitForEndOfFrame();
        LoadEdges(ref reader);
        yield return new WaitForEndOfFrame();
        LoadTexInfo(ref reader);
        yield return new WaitForEndOfFrame();
        LoadFaces(ref reader);
        yield return new WaitForEndOfFrame();
        LoadFaceEdges(ref reader);

        Console.Log("- BSP tree");
        yield return new WaitForEndOfFrame();

        LoadNodesAndLeaves(ref reader);
        yield return new WaitForEndOfFrame();
        LoadLeafFaces(ref reader);
        yield return new WaitForEndOfFrame();
        LoadBrushes(ref reader);
        yield return new WaitForEndOfFrame();
        LoadBrushSides(ref reader);
        yield return new WaitForEndOfFrame();
        LoadLeafBrushes(ref reader);

        Console.Log("- lightmaps");
        yield return new WaitForEndOfFrame();

        LoadLightmaps(ref reader);

        Console.Log("- BSP models");
        yield return new WaitForEndOfFrame();

        LoadModels(ref reader);

        yield return new WaitForEndOfFrame();

        if (!ParseEntstring(ref reader, mapname))
        {
            OnMapLoadedEvent?.Invoke(false);
            yield break;
        }

        reader.Close();

        PrintMapInfo();

        OnMapLoadedEvent?.Invoke(true);
        yield break;
    }

    public static bool LoadBsp(string mapname)
    {
        Clear(); //free up resources

        scale = Globals.scale.Value;

        //get map bytes
        if (!ResourceLoader.LoadFile("maps/" + mapname + ".bsp", out byte[] rawdata))
        {
            Console.LogWarning("Map " + "maps/" + mapname + ".bsp not found!");
            return false;
        }

        BinaryReader reader = new BinaryReader(new MemoryStream(rawdata));

        //load header
        header = new BSPHeader
        {
            ident = reader.ReadUInt32(),
            version = reader.ReadUInt32(),
            lumps = new List<BSPLump>()
        };

        //validate header
        if (!ValidateHeader())
        {
            reader.Close();
            return false;
        }

        //load lumps
        LoadLumps(ref reader);

        //validate lumps
        if (!ValidateLumps(reader.BaseStream.Length))
        {
            reader.Close();
            return false;
        }

        //load lump contents
        LoadVerts(ref reader);
        LoadPlanes(ref reader);
        LoadEdges(ref reader);
        LoadTexInfo(ref reader);
        LoadFaces(ref reader);
        LoadFaceEdges(ref reader);
        LoadNodesAndLeaves(ref reader);
        LoadLeafFaces(ref reader);
        LoadBrushes(ref reader);
        LoadBrushSides(ref reader);
        LoadLeafBrushes(ref reader);
        LoadLightmaps(ref reader);
        LoadModels(ref reader);
        if (!ParseEntstring(ref reader, mapname))
        {
            return false;
        }

        //load md2 models
        //TODO: move this somewhere else
        //update: get rid of this?

        reader.Close();

        PrintMapInfo();

        return true;
    }

    public static void Printmapinfo_c(string[] args)
    {
        if (!Cvar.Boolean("maploaded"))
        {
            Console.LogWarning("No map loaded.");
            return;
        }

        string mapMsg = worldspawn.strings.FirstOrDefault(i => i.Key.StartsWith("message")).Value;

        Console.LogInfo("MAP INFO");
        Console.Log(
            "\nMap name: " + Globals.map.Text +
            "\nBSP file version: " + header.version +
            "\nVertices: " + verts.Length +
            "\nPlanes: " + planes.Length +
            "\nEdges: " + edges.Count +
            "\nFaces: " + faces.Count +
            "\nFace edges: " + faceedges.Count +
            "\nTexure variants: " + texinfo.Count +
            "\nBrushes: " + brushes.Length +
            "\nBrush sides: " + brushsides.Length +
            "\nLeaves: " + leaves.Length +
            "\n" +
            "\nMap msg: " + (mapMsg != null ? mapMsg : "") +
            "\n"
            );
    }

    private static void PrintMapInfo()
    {
        string mapMsg = worldspawn.strings.FirstOrDefault(i => i.Key.StartsWith("message")).Value;

        Console.Log("<color=green>-_-_-_-_-_-_-_-_-_-_-_-_-\n" + 
                (mapMsg == null ? Globals.map.Text : mapMsg)
                + "\n_-_-_-_-_-_-_-_-_-_-_-_-_</color>");
    }

    #region validation

    private static bool ValidateHeader()
    {
        if(header.ident != (('P' << 24) + ('S' << 16) + ('B' << 8) + 'I'))
        {
            Console.LogWarning("Invalid map file format!");
            return false;
        }
        if(header.version != 38)
        {
            Console.LogWarning("Unsupported BSP file version: " + header.version);
            return false;
        }
        return true;
    }

    private static bool ValidateLumps(long streamSize)
    {
        for (int i = 0; i < 19; i++)
        {
            BSPLump lump = header.lumps[i];
            if(lump.filelen + lump.fileofs > streamSize)
            {
                Console.LogWarning("Incorrect lump size! (" + (BSPLumps)i + ")");
                return false;
            }
        }
        return true;
    }

    #endregion

    /// <summary>
    /// Loads lump info into the lumps list.
    /// </summary>
    private static void LoadLumps(ref BinaryReader reader)
    {
        for (int i = 0; i < 19; i++)
        {
            uint offs = reader.ReadUInt32();
            uint len = reader.ReadUInt32();

            header.lumps.Add(new BSPLump
            {
                fileofs = offs, filelen = len
            });
        }
    }

    //geometry

    /// <summary>
    /// Loads vertices swapping Y and Z.
    /// </summary>
    private static bool LoadVerts(ref BinaryReader reader)
    {
        int offset = GetLumpOffset(BSPLumps.LUMP_VERTEXES);
        int count = GetLumpLength(BSPLumps.LUMP_VERTEXES) / 12; //12 bytes per vertex
        verts = new Vector3[count];

        reader.BaseStream.Position = offset;

        float v1, v2, v3;
        for(int i = 0; i < count; i++)
        {
            v1 = reader.ReadSingle() * scale;
            v2 = reader.ReadSingle() * scale;
            v3 = reader.ReadSingle() * scale;

            verts[i] = new Vector3(v1, v3, v2); //swapping Y with Z because Y is up in Unity
        }
        return true;
    }

    /// <summary>
    /// Loads planes. Planes have YZ swapped but no scale applied.
    /// </summary>
    private static void LoadPlanes(ref BinaryReader reader)
    {
        float v1, v2, v3;

        int size = 20;
        int count = GetLumpLength(BSPLumps.LUMP_PLANES) / size;
        reader.BaseStream.Position = GetLumpOffset(BSPLumps.LUMP_PLANES);

        planes = new BSPPlane[count];

        for(int i = 0; i < count; i++)
        {
            v1 = reader.ReadSingle();
            v2 = reader.ReadSingle();
            v3 = reader.ReadSingle();

            planes[i] = new BSPPlane()
            {
                normal = new Vector3(v1, v3, v2),
                distance = reader.ReadSingle(),
                type = reader.ReadUInt32()
            };
        }
    }

    /// <summary>
    /// Loads edges lump.
    /// </summary>
    private static bool LoadEdges(ref BinaryReader reader)
    {
        edges = new List<BSPEdge>();
        int count = GetLumpLength(BSPLumps.LUMP_EDGES) / 4;
        reader.BaseStream.Position = GetLumpOffset(BSPLumps.LUMP_EDGES);

        //load vertex indices
        ushort i1;
        ushort i2;
        for (int i = 0; i < count; i++)
        {
            i1 = reader.ReadUInt16();
            i2 = reader.ReadUInt16();

            if(i1 > verts.Length || i2 > verts.Length)
            {
                Console.Log("Bad vertex index in edge lump.");
                return false;
            }

            edges.Add(new BSPEdge { i1 = i1, i2 = i2 });
        }
        return true;
    }

    /// <summary>
    /// Loads texture information lump.
    /// </summary>
    private static void LoadTexInfo(ref BinaryReader reader)
    {
        texinfo = new List<BSPTexInfo>();

        int size = 76;
        int count = GetLumpLength(BSPLumps.LUMP_TEXINFO) / size;
        reader.BaseStream.Position = GetLumpOffset(BSPLumps.LUMP_TEXINFO);

        float v1, v2, v3;
        for(int i = 0; i < count; i++)
        {
            BSPTexInfo info = new BSPTexInfo();

            v1 = reader.ReadSingle();
            v2 = reader.ReadSingle();
            v3 = reader.ReadSingle();

            info.u_axis = new Vector3(v1, v3, v2);
            info.u_offset = reader.ReadSingle() * scale;

            v1 = reader.ReadSingle();
            v2 = reader.ReadSingle();
            v3 = reader.ReadSingle();

            info.v_axis = new Vector3(v1, v3, v2);
            info.v_offset = reader.ReadSingle() * scale;

            info.flags = (SurfFlags)reader.ReadUInt32();
            info.value = reader.ReadUInt32();

            info.texture_name = reader.ReadChars(32);

            info.next_texinfo = reader.ReadUInt32();

            texinfo.Add(info);
        }
    }

    /// <summary>
    /// Loads faces.
    /// </summary>
    private static bool LoadFaces(ref BinaryReader reader)
    {
        if(faces == null)
        {
            faces = new List<BSPFace>();
        }
        int count = GetLumpLength(BSPLumps.LUMP_FACES) / 20;
        reader.BaseStream.Position = GetLumpOffset(BSPLumps.LUMP_FACES);

        for(int i = 0; i < count; i++)
        {
            BSPFace f = new BSPFace
            {
                planenum = reader.ReadUInt16(),
                side = reader.ReadUInt16(),

                firstedge = reader.ReadUInt32(),
                numedges = reader.ReadUInt16(),
                texinfo = reader.ReadUInt16(),

                styles = reader.ReadBytes(4),

                lightofs = reader.ReadUInt32(),
            };

            if(f.numedges < 3 || f.numedges > 4096) //is the upper limit necessary?
            {
                Console.LogWarning("Bad edge count for face.");
                return false;
            }
            if (f.firstedge + f.numedges < f.firstedge)
            {
                Console.LogWarning("Bad edge index for face.");
                return false;
            }
            if(f.planenum >= planes.Length)
            {
                Console.LogWarning("Bad plane index.");
                return false;
            }
            if(f.texinfo >= texinfo.Count)
            {
                Console.LogWarning("Bad texinfo index.");
                return false;
            }

            faces.Add(f);
        }
        return true;
    }

    /// <summary>
    /// Loads face edges.
    /// </summary>
    private static bool LoadFaceEdges(ref BinaryReader reader)
    {
        if (faceedges == null)
        {
            faceedges = new List<BSPFaceEdge>();
        }

        int offset = GetLumpOffset(BSPLumps.LUMP_SURFEDGES);
        int size = 4;
        int currentedge;
        int edgeIndex;

        for (int i = 0; i < faces.Count; i++)
        {
            faces[i].edges = new List<BSPFaceEdge>();
            reader.BaseStream.Position = offset + faces[i].firstedge * size;

            for(int a = 0; a < faces[i].numedges; a++)
            {
                currentedge = reader.ReadInt32();
                edgeIndex = Mathf.Abs(currentedge);
                if(edgeIndex > edges.Count)
                {
                    Console.LogWarning("Bad edge index in face edge lump.");
                    return false;
                }

                faces[i].edges.Add(edges[edgeIndex].ToFaceEdge(currentedge < 0));
            }
            faceedges.AddRange(faces[i].edges);
        }
        return true;
    }

    //bsp tree

    /// <summary>
    /// Loads and parses the BSP tree.
    /// </summary>
    private static void LoadNodesAndLeaves(ref BinaryReader reader)
    {
        int i;
        float v1, v2, v3;

        int offset = GetLumpOffset(BSPLumps.LUMP_NODES);
        int count = GetLumpLength(BSPLumps.LUMP_NODES) / 28;

        reader.BaseStream.Position = offset;

        nodes = new BSPNode[count];

        for(i = 0; i < count; i++)
        {
            nodes[i] = new BSPNode
            {
                plane = reader.ReadUInt32(),

                front_child = reader.ReadInt32(),
                back_child = reader.ReadInt32()
            };

            v1 = reader.ReadInt16() * scale;
            v2 = reader.ReadInt16() * scale;
            v3 = reader.ReadInt16() * scale;

            nodes[i].mins = new Vector3(v1, v3, v2);

            v1 = reader.ReadInt16() * scale;
            v2 = reader.ReadInt16() * scale;
            v3 = reader.ReadInt16() * scale;

            nodes[i].maxs = new Vector3(v1, v3, v2);

            nodes[i].first_face = reader.ReadUInt16();
            nodes[i].num_faces = reader.ReadUInt16();
        }

        offset = GetLumpOffset(BSPLumps.LUMP_LEAFS);
        count = GetLumpLength(BSPLumps.LUMP_LEAFS) / 28;

        reader.BaseStream.Position = offset;

        leaves = new BSPLeaf[count];

        for (i = 0; i < count; i++)
        {
            leaves[i] = new BSPLeaf()
            {
                contents = (BrushContents)reader.ReadUInt32(),

                cluster = reader.ReadUInt16(),
                area = reader.ReadUInt16(),
                index = i
            };

            //load mins and maxs unscaled
            v1 = reader.ReadInt16();
            v2 = reader.ReadInt16();
            v3 = reader.ReadInt16();

            leaves[i].mins = new Vector3(v1, v3, v2);

            v1 = reader.ReadInt16();
            v2 = reader.ReadInt16();
            v3 = reader.ReadInt16();

            leaves[i].maxs = new Vector3(v1, v3, v2);

            leaves[i].first_face = reader.ReadUInt16();
            leaves[i].num_faces = reader.ReadUInt16();

            leaves[i].first_brush = reader.ReadUInt16();
            leaves[i].num_brushes = reader.ReadUInt16();
        }

        headnode = nodes[0];

        for(i = 0; i < nodes.Length; i++)
        {
            nodes[i].front_node = nodes[i].front_child >= 0 ? nodes[nodes[i].front_child] : leaves[-(nodes[i].front_child + 1)];
            nodes[i].back_node = nodes[i].back_child >= 0 ? nodes[nodes[i].back_child] : leaves[-(nodes[i].back_child + 1)];
        }
    }

    /// <summary>
    /// Loads leaf faces.
    /// </summary>
    private static bool LoadLeafFaces(ref BinaryReader reader)
    {
        int offset = GetLumpOffset(BSPLumps.LUMP_LEAFFACES);
        int count = GetLumpLength(BSPLumps.LUMP_LEAFFACES) / 2;

        reader.BaseStream.Position = offset;
        leaffaces = new ushort[count];

        for(int i = 0; i < count; i++)
        {
            leaffaces[i] = reader.ReadUInt16();

            if(leaffaces[i] >= faces.Count)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Loads brushes.
    /// </summary>
    private static void LoadBrushes(ref BinaryReader reader)
    {
        int offset = GetLumpOffset(BSPLumps.LUMP_BRUSHES);
        int count = GetLumpLength(BSPLumps.LUMP_BRUSHES) / 12;

        reader.BaseStream.Position = offset;
        brushes = new BSPBrush[count];

        for(int i = 0; i < count; i++)
        {
            brushes[i] = new BSPBrush
            {
                firstside = reader.ReadUInt32(),
                numsides = reader.ReadUInt32(),
                contents = (BrushContents)reader.ReadUInt32()
            };
        }
    }

    /// <summary>
    /// Loads brush sides.
    /// </summary>
    private static void LoadBrushSides(ref BinaryReader reader)
    {
        int offset = GetLumpOffset(BSPLumps.LUMP_BRUSHSIDES);
        int count = GetLumpLength(BSPLumps.LUMP_BRUSHSIDES) / 4;

        reader.BaseStream.Position = offset;
        brushsides = new BSPBrushSide[count];

        for(int i = 0; i < count; i++)
        {
            brushsides[i] = new BSPBrushSide
            {
                planenum = reader.ReadUInt16(),
                texinfo = reader.ReadUInt16()
            };
        }
    }

    /// <summary>
    /// Loads leaf brushes.
    /// </summary>
    private static void LoadLeafBrushes(ref BinaryReader reader)
    {
        int offset = GetLumpOffset(BSPLumps.LUMP_LEAFBRUSHES);
        int count = GetLumpLength(BSPLumps.LUMP_LEAFBRUSHES) / 2;

        reader.BaseStream.Position = offset;
        leafbrushes = new BSPBrush[count];

        for(int i = 0; i < count; i++)
        {
            leafbrushes[i] = brushes[reader.ReadUInt16()];
        }
    }

    /// <summary>
    /// Loads and parses lightmaps.
    /// </summary>
    private static void LoadLightmaps(ref BinaryReader reader)
    {
        if (lightmaps == null)
        {
            lightmaps = new List<LightmapTex>();
        }

        if(GetLumpLength(BSPLumps.LUMP_LIGHTING) == 0)
        {
            //no lightmap data in the file
            return;
        }

        int offset = GetLumpOffset(BSPLumps.LUMP_LIGHTING);

        List<float> us = new List<float>();
        List<float> vs = new List<float>();

        for (int i = 0; i < faces.Count; i++)
        {
            //skip faces with no lightmaps
            if((faces[i].BSPTexInfo.flags & SurfFlags.MASK_NOLIGHTMAP) != SurfFlags.SURF_NONE)
            {
                continue;
            }

            float u, v;
            Vector3 u_axis = faces[i].BSPTexInfo.u_axis;
            Vector3 v_axis = faces[i].BSPTexInfo.v_axis;

            //calculate face extents
            for (int j = 0; j < faces[i].edges.Count; j++)
            {
                Vector3 p = faces[i].edges[j].E1 / scale;

                u = Vector3.Dot(p, u_axis) + faces[i].BSPTexInfo.u_offset / scale;
                v = Vector3.Dot(p, v_axis) + faces[i].BSPTexInfo.v_offset / scale;

                if (!us.Contains(u))
                {
                    us.Add(u);
                }
                if (!vs.Contains(v))
                {
                    vs.Add(v);
                }
            }

            us.Sort();
            vs.Sort();

            float min_u = us.First();
            float max_u = us.Last();
            float min_v = vs.First();
            float max_v = vs.Last();

            us.Clear();
            vs.Clear();

            LightmapTex lightmap = new LightmapTex();

            //calculate lightmap dimensions
            //decimal cast not required...?
            lightmap.width = (int)Math.Ceiling((decimal)(max_u / 16f)) - (int)Math.Floor((decimal)(min_u / 16f)) + 1;
            lightmap.height = (int)Math.Ceiling((decimal)(max_v / 16f)) - (int)Math.Floor((decimal)(min_v / 16f)) + 1;

            //fix lightmap dimensions...
            if(u_axis.magnitude > 1)
            {
                lightmap.width /= (int)u_axis.magnitude;
            }
            if (v_axis.magnitude > 1)
            {
                lightmap.height /= (int)v_axis.magnitude;
            }

            lightmap.u_min = min_u;
            lightmap.v_min = min_v;

            //read lightmap data
            reader.BaseStream.Position = offset + faces[i].lightofs;
            lightmap.rawdata = reader.ReadBytes(lightmap.width * lightmap.height * 3);

            //create a lightmap texture
            lightmap.tex = new Texture2D(lightmap.width, lightmap.height, TextureFormat.RGB24, false);
            if (lightmap.rawdata.Length > 0)
            {
                lightmap.tex.LoadRawTextureData(lightmap.rawdata);
                lightmap.tex.filterMode = FilterMode.Trilinear;
                lightmap.tex.Apply();
            }

            faces[i].lightmap = lightmap;
            lightmaps.Add(lightmap);
        }
    }

    /// <summary>
    /// Loads BSP models.
    /// </summary>
    private static void LoadModels(ref BinaryReader reader)
    {
        float v1, v2, v3;
        uint headnode;
        int size = 48;
        int offset = GetLumpOffset(BSPLumps.LUMP_MODELS);
        int count = GetLumpLength(BSPLumps.LUMP_MODELS) / size;

        models = new BSPModel[count];

        reader.BaseStream.Position = offset;

        for (int i = 0; i < count; i++)
        {
            models[i] = new BSPModel();

            v1 = reader.ReadSingle() - 1;
            v2 = reader.ReadSingle() - 1;
            v3 = reader.ReadSingle() - 1;

            models[i].mins = new Vector3(v1, v3, v2) * scale;

            v1 = reader.ReadSingle() + 1;
            v2 = reader.ReadSingle() + 1;
            v3 = reader.ReadSingle() + 1;

            models[i].maxs = new Vector3(v1, v3, v2) * scale;

            v1 = reader.ReadSingle();
            v2 = reader.ReadSingle();
            v3 = reader.ReadSingle();

            models[i].origin = new Vector3(v1, v3, v2) * scale;

            headnode = reader.ReadUInt32();
            if((headnode & 0x80000000) != 0)
            {
                headnode = ~headnode;
                models[i].headnode = leaves[headnode];
            }
            else
            {
                models[i].headnode = nodes[headnode];
            }

            models[i].firstface = reader.ReadUInt32();
            models[i].numfaces = reader.ReadUInt32();

            models[i].worldspawn = i == 0;
        }
    }

    /// <summary>
    /// A really scrappy way of parsing the ent string...
    /// Don't judge me please.
    /// </summary>
    private static bool ParseEntstring(ref BinaryReader reader, string mapname)
    {
        if (ents == null)
        {
            ents = new List<BSPEnt>();
        }

        reader.BaseStream.Position = GetLumpOffset(BSPLumps.LUMP_ENTSTRING);

        long charcount = reader.BaseStream.Length - reader.BaseStream.Position - 256;
        char[] chars = reader.ReadChars((int)charcount);

        if (ResourceLoader.LoadFile("ent/" + mapname + ".ent", out byte[] bytes))
        {
            Console.DebugLog("Found ent file ent/" + mapname + ".ent");
            chars = System.Text.Encoding.UTF8.GetChars(bytes);
        }

        Queue<char> q = new Queue<char>(chars);

        char current;
        string key = "";
        string value = "";

        while (q.Count > 0)
        {
            current = q.Dequeue();

            if (q.Count == 0)
            {
                break;
            }
            else if (current != '{')
            {
                if (current == '\0')
                {
                    Console.DebugLog("Entstring terminated by a null character.");
                    break;
                }
                goto fail;
            }

            current = q.Dequeue();
            while (char.IsControl(current) && current != '\n' && q.Count > 0)
            {
                current = q.Dequeue();
            }
            if (current != '\n')
            {
                goto fail;
            }

            BSPEnt ent = new BSPEnt();

            current = q.Dequeue();

            while (current != '}')
            {
                if (current != '"')
                {
                    goto fail;
                }

                current = q.Dequeue();
                while (q.Count > 0 && current != '"')
                {
                    key += current;
                    current = q.Dequeue();
                }

                current = q.Dequeue();
                if (current != ' ')
                {
                    goto fail;
                }

                current = q.Dequeue();
                if (current != '"')
                {
                    goto fail;
                }

                current = q.Dequeue();
                while (q.Count > 0 && current != '"')
                {
                    value += current;
                    current = q.Dequeue();
                }

                ent.strings.Add(key, value);

                key = "";
                value = "";

                current = q.Dequeue();
                while (char.IsControl(current) && current != '\n' && q.Count > 0)
                {
                    current = q.Dequeue();
                }
                if (current != '\n')
                {
                    goto fail;
                }

                if (q.Count == 0)
                {
                    goto fail;
                }
                current = q.Dequeue();
            }

            ents.Add(ent);

            current = q.Dequeue();
            if (current == '\0')
            {
                break;
            }

            while (char.IsControl(current) && current != '\n' && q.Count > 0)
            {
                current = q.Dequeue();
            }
            if (current != '\n' && q.Count > 0)
            {
                goto fail;
            }
        }

        foreach (BSPEnt ent in ents)
        {
            ent.ParseEntStrings();

            if (ent.Classname == "worldspawn")
            {
                worldspawn = ent;
            }
        }

        return true;

        fail:
        Console.LogWarning("ENTSTRING: queue count: " + q.Count + " Incorrect ent char code: " + (int)current);

        ents.Clear();
        return false;
    }
}
