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

/// <summary>
/// This MonoBehaviour represents a BSP entity in Unity.
/// The entity can have a Bmodel or simply be a functional ent.
/// </summary>
public class GameModel : MonoBehaviour
{
    //prefabs
    public GameObject mapObject;
    public GameObject waterObject;
    public GameObject depthMaskObject;

    //BSP entity data.
    public BSPEnt Ent
    {
        get;
        private set;
    }

    //for functional entities
    public GameObject entModel;
    public ParticleSystem entParticles;

    //physics
    public bool clipTest = false; //clip test this model?
    public BSPModel Bmodel //bmodel for clip testing
    {
        get;
        private set;
    }

    //bmodel geometry references
    public List<GameObject> faceObjects = new List<GameObject>();

    //for generating geometry
    private List<int> tris = new List<int>();
    private List<Vector3> verts = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Vector2> uvs2 = new List<Vector2>();

    //separate functional mesh: if entity has a model that needs mesh collider then this is used.
    private List<int> collisionTris = new List<int>();
    private List<Vector3> collisionVerts = new List<Vector3>();

    //face grouping
    private List<List<BSPFace>> faceGroups;
    private const int maxgroup = 768;

    #region Unity collisions

    bool collidedThisFrame = false; //no longer needed to check this? Mesh collider is now a single brush...

    private void OnTriggerEnter(Collider other)
    {
        OnTrigger(other);
    }

    public void OnTrigger(Collider other)
    {
        //avoid multiple touches by children
        if (collidedThisFrame)
        {
            return;
        }
        collidedThisFrame = true;

        Ent.Touch?.Invoke(this, other);
    }


    private void Update()
    {
        //run think for that entity
        Ent.Think?.Invoke(this);
    }

    private void FixedUpdate()
    {
        collidedThisFrame = false;
    }

    #endregion

    /// <summary>
    /// Creates the model.s
    /// </summary>
    /// <param name="model">BSP model</param>
    /// <param name="entity">Entity data.</param>
    public void CreateModel(BSPModel model, BSPEnt entity)
    {
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        LightmapAtlas newAtlas;
        Color col;
        List<BSPFace> fs;
        float transparency;
        bool depthMask;
        bool hasTexture;
        bool hasLightmap;

        //set gameobject's name
        gameObject.name = "GM(" + entity.Classname + ")";
        Ent = entity;
        Bmodel = model;

        if(model == null || model.numfaces == 0)
        {
            //entity with no bmodel - run spawn and return
            EntitySpawn.Instance.SpawnEntity(this);
            return;
        }

        BSPFace[] faces = model.Faces;

        //group faces by texture and its contents
        var groupsByTexture = faces.GroupBy(i => new {
            i.BSPTexInfo.texfile_guid,
            i.BSPTexInfo.flags,
            c = Utils.GetPointContents(i.Center - (i.Normal * 0.5f)).HasFlag(BrushContents.CONTENTS_WATER) && i.Normal.y == 1
        }).Select(group => group.ToList()).ToList();

        //limit group size to maxgroup
        faceGroups = new List<List<BSPFace>>();
        foreach (List<BSPFace> list in groupsByTexture)
        {
            if (list.Count > maxgroup)
            {
                faceGroups.AddRange(list.Select((x, i) => new { Index = i, Value = x }).GroupBy(x => x.Index / maxgroup).Select(x => x.Select(v => v.Value).ToList()).ToList());
            }
            else
            {
                faceGroups.Add(list);
            }
        }

        //create Bmodel geometry
        for(int a = 0; a < faceGroups.Count; a++)
        {
            fs = faceGroups[a];

            //nodraw brushes can only have a collider mesh
            //TODO: should this only happen to clip brushes?
            if (fs[0].BSPTexInfo.flags.HasFlag(SurfFlags.SURF_NODRAW))
            {
                tris.Clear();
                verts.Clear();

                foreach (BSPFace f in fs)
                {
                    AddFaceToMeshData(f, false, false);
                }

                collisionVerts.AddRange(verts);
                collisionTris.AddRange(tris);

                continue;
            }

            //is this a depth mask?
            depthMask = fs[0].BSPTexInfo.flags.HasFlag(SurfFlags.SURF_SKY);

            //create a lightmap atlas for the group
            if (fs[0].lightmap != null && !depthMask)
            {
                Texture2D[] texs = new Texture2D[fs.Count];

                for (int g = 0; g < texs.Length; g++)
                {
                    if (fs[g].lightmap != null)
                    {
                        texs[g] = fs[g].lightmap.tex;
                    }
                }

                newAtlas = new LightmapAtlas
                {
                    tex = new Texture2D(512, 512)
                };

                //pack the atlas
                newAtlas.rect = newAtlas.tex.PackTextures(texs, 2, 512);

                newAtlas.tex.filterMode = FilterMode.Trilinear;

                for (int b = 0; b < fs.Count; b++)
                {
                    fs[b].atlas = newAtlas;
                    fs[b].atlas_index = b;
                }

                hasLightmap = true;
            }
            else
            {
                hasLightmap = false;
            }

            GameObject o;
            
            //if group is made out of sky brushes make it a depth mask
            if (depthMask)
            {
                o = Instantiate(depthMaskObject, transform);
                hasTexture = false;
                transparency = -1;

                meshRenderer = o.GetComponent<MeshRenderer>();
            }
            else
            {
                //check object transparency
                if (fs[0].BSPTexInfo.flags.HasFlag(SurfFlags.SURF_TRANS33))
                {
                    transparency = 0.33f;
                }
                else if (fs[0].BSPTexInfo.flags.HasFlag(SurfFlags.SURF_TRANS66))
                {
                    transparency = 0.66f;
                }
                else
                {
                    transparency = -1; //solid
                }

                //instantiate object and set its transparency
                if(transparency > 0)
                {
                    o = Instantiate(waterObject, transform);
                    meshRenderer = o.GetComponent<MeshRenderer>();

                    col = Color.white;
                    col.a = transparency;
                    meshRenderer.material.color = col;
                }
                else
                {
                    o = Instantiate(mapObject, transform);
                    meshRenderer = o.GetComponent<MeshRenderer>();
                }
            }

            //clear lists
            tris.Clear();
            verts.Clear();
            uvs.Clear();
            uvs2.Clear();

            meshFilter = o.GetComponent<MeshFilter>();

            //set object's name
            o.name = entity.Classname + " " + a.ToString();

            //create mesh
            Mesh mesh = new Mesh() { name = entity.Classname };

            //check if a texture has been successfully loaded
            hasTexture = TextureLoader.HasTexture(fs[0].BSPTexInfo.texfile_guid);

            //read all faces into mesh arrays
            foreach(BSPFace f in fs)
            {
                AddFaceToMeshData(f, hasTexture, hasLightmap);
            }

            //add mesh data to collision mesh
            collisionVerts.AddRange(verts);
            collisionTris.AddRange(tris);

            //fill the mesh
            mesh.vertices = verts.ToArray();
            if (hasTexture)
            {
                mesh.uv = uvs.ToArray();
            }
            mesh.triangles = tris.ToArray();
            if(hasLightmap)
            {
                mesh.uv2 = uvs2.ToArray();
            }

            //calculate normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;

            //set textures
            if (hasTexture)
            {
                meshRenderer.material.mainTexture = TextureLoader.GetTexture(fs[0].BSPTexInfo.texfile_guid);
            }

            //set lightmap textures
            if(hasLightmap)
            {
                meshRenderer.material.SetTexture("_LightmapTex", fs[0].atlas.tex);
            }

            //add object to list
            faceObjects.Add(o);
        }

        //spawn bmodel entity at the very end!
        EntitySpawn.Instance.SpawnEntity(this);

        //generate collision mesh
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider)
        {
            Mesh collisionMesh = new Mesh
            {
                vertices = collisionVerts.ToArray(),
                triangles = collisionTris.ToArray()
            };

            collisionMesh.RecalculateNormals();
            collisionMesh.RecalculateBounds();
            collisionMesh.name = entity.Classname;

            meshCollider.sharedMesh = collisionMesh;
        }
    }

    /// <summary>
    /// Reads face data and generates UV coordinates.
    /// </summary>
    private void AddFaceToMeshData(BSPFace f, bool hasTexture, bool hasLightmap)
    {
        int firstindex = f.edges[0].i1;

        for (int i = 1; i < f.edges.Count; i++)
        {
            //vertices and triangles
            verts.Add(BSPFile.verts[firstindex]);
            tris.Add(verts.Count - 1);
            verts.Add(BSPFile.verts[f.edges[i].i1]);
            tris.Add(verts.Count - 1);
            verts.Add(BSPFile.verts[f.edges[i].i2]);
            tris.Add(verts.Count - 1);

            //texture uvs
            if (hasTexture)
            {
                uvs.Add(Utils.GetUV(BSPFile.verts[firstindex], f));
                uvs.Add(Utils.GetUV(BSPFile.verts[f.edges[i].i1], f));
                uvs.Add(Utils.GetUV(BSPFile.verts[f.edges[i].i2], f));
            }

            //lightmap uvs
            if (hasLightmap)
            {
                //fetch unscaled lightmap uvs
                Rect r = f.atlas.rect[f.atlas_index];
                Vector2 uv1 = Utils.GetLightmapUV(BSPFile.verts[firstindex], f);
                Vector2 uv2 = Utils.GetLightmapUV(BSPFile.verts[f.edges[i].i1], f);
                Vector2 uv3 = Utils.GetLightmapUV(BSPFile.verts[f.edges[i].i2], f);

                //normalize the uvs
                uv1.x = uv1.x - Mathf.Floor(uv1.x);
                uv2.x = uv2.x - Mathf.Floor(uv2.x);
                uv3.x = uv3.x - Mathf.Floor(uv3.x);

                uv1.y = uv1.y - Mathf.Floor(uv1.y);
                uv2.y = uv2.y - Mathf.Floor(uv2.y);
                uv3.y = uv3.y - Mathf.Floor(uv3.y);

                //project uvs on the texture atlas
                uvs2.Add(uv1 * r.size + r.min);
                uvs2.Add(uv2 * r.size + r.min);
                uvs2.Add(uv3 * r.size + r.min);
            }
        }
    }
}

