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

using System.Linq;
using UnityEngine;

/*
 * Stores BSP model data.
 * A model is a set of faces (worldspawn, elevator, train etc.) that is combined
 * together, moves as a separate part and is tested for collisions separately.
 */

public class BSPModel
{
    public bool worldspawn;

    public int type;

    public Vector3 mins;
    public Vector3 maxs;

    public Vector3 origin;

    public BSPNode headnode;

    public float radius;
    public uint numfaces;
    public uint firstface;

    public int drawframe;

    private BSPFace[] facecache;

    public BSPFace[] Faces
    {
        get
        {
            if(facecache == null)
            {
                facecache = BSPFile.faces.Skip((int)firstface).Take((int)numfaces).ToArray();
            }

            return facecache;
        }
    }
}
