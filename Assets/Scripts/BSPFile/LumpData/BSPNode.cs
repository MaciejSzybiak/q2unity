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
 * Stores BSP node data.
 */

public class BSPNode
{
    public uint plane;

    public int front_child;
    public int back_child;

    public BSPNode front_node;
    public BSPNode back_node;

    public Vector3 mins;
    public Vector3 maxs;

    public ushort first_face;
    public ushort num_faces;

    public BSPPlane Plane
    {
        get
        {
            return BSPFile.planes[plane];
        }
    }
}
