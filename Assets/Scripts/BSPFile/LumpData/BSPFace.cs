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
using UnityEngine;

/*
 * Stores face data.
 */

public class BSPFace
{
    public ushort planenum;
    public ushort side;

    public uint firstedge;
    public ushort numedges;
    public ushort texinfo;

    public byte[] styles;
    public uint lightofs;

    public List<BSPFaceEdge> edges; //edges are preloaded by BSPFile
    public BSPTexInfo BSPTexInfo
    {
        get
        {
            return BSPFile.texinfo[texinfo];
        }
    }

    public Vector3 Normal
    {
        get
        {
            return side != 0 ? -BSPFile.planes[planenum].normal : BSPFile.planes[planenum].normal;
        }
    }

    public LightmapTex lightmap;

    public LightmapAtlas atlas;
    public int atlas_index;

    public Vector3 Center
    {
        get
        {
            Vector3 c = new Vector3();
            foreach(BSPFaceEdge e in edges)
            {
                c += e.E1;
            }

            return c / edges.Count;
        }
    }
}