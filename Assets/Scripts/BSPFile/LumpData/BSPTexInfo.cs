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

using System;
using UnityEngine;

/*
 * Stores texture info data.
 */

public class BSPTexInfo : IEquatable<BSPTexInfo>
{
    public Vector3 u_axis;
    public float u_offset;

    public Vector3 v_axis;
    public float v_offset;

    public SurfFlags flags;
    public uint value;

    public char[] texture_name;

    public uint next_texinfo;

    public Guid texfile_guid;

    public SurfaceT Surface
    {
        get
        {
            return new SurfaceT
            {
                name = TexString,
                flags = (int)flags,
                value = (int)value
            };
        }
    }

    //this is needed because the regular conversion somehow eats up any characters added after it...
    public string TexString
    {
        get
        {
            string output = "";
            int i = 0;
            while (i < 32 && texture_name[i] != 0)
            {
                output += texture_name[i];
                i++;
            }
            return output;
        }
    }

    public bool Equals(BSPTexInfo other)
    {
        if(other == null)
        {
            return false;
        }
        return other.u_axis.Equals(u_axis) &&
            other.u_offset == u_offset &&
            other.v_axis.Equals(v_axis) &&
            other.v_offset == v_offset &&
            other.flags == flags &&
            other.value == value &&
            other.texture_name == texture_name &&
            other.next_texinfo == next_texinfo &&
            other.texfile_guid == texfile_guid;

    }
}