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
 * Stores edge data.
 */

public class BSPEdge
{
    public ushort i1;
    public ushort i2;
    public Vector3 E1
    {
        get
        {
            return BSPFile.verts[i1];
        }
    }
    public Vector3 E2
    {
        get
        {
            return BSPFile.verts[i2];
        }
    }

    public BSPFaceEdge ToFaceEdge(bool flip)
    {
        if (flip)
        {
            return new BSPFaceEdge
            {
                i1 = i2,
                i2 = i1,
                flipped = true
            };
        }
        else
        {
            return new BSPFaceEdge
            {
                i1 = i1,
                i2 = i2,
                flipped = false
            };
        }
    }
}
