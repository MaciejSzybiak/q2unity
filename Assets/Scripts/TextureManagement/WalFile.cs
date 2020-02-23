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

public class WalFile : TexFile
{
    public char[] name = new char[32];

    public int[] offset = new int[4];

    public char[] next_name = new char[32];

    public uint flags;
    public uint contents;
    public uint value;

    public byte[] mip0_data;
    public byte[] mip1_data;
    public byte[] mip2_data;
    public byte[] mip3_data;

    public string filename;

    public byte[] colordata;
    public Texture2D tex;

    public string name_string
    {
        get
        {
            string output = "";
            int i = 0;
            while (i < 32 && name[i] != 0)
            {
                output += name[i];
                i++;
            }
            return output;
        }
    }

    public string next_tex_string
    {
        get
        {
            string output = "";
            int i = 0;
            while (i < 32 && next_name[i] != 0)
            {
                output += next_name[i];
                i++;
            }
            return output;
        }
    }
}
