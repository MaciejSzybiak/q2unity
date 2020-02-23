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

public class PCXFile
{
    public byte identifier;
    public byte version;
    public byte encoding;
    public byte bitsPerPixel;
    public short xmin;
    public short ymin;
    public short xmax;
    public short ymax;
    public short xdpi;
    public short ydpi;
    public byte[] hpalette = new byte[48];
    public byte reserved;
    public byte planes;
    public short bytesPerLine;
    public short paletteType;
    public short hScreenSize;
    public short vScreenSize;
    public byte[] reserved2 = new byte[54];

    public int width;
    public int height;
    public int scanlineLength;
    public int linePaddingSize;

    public byte[] qpalette = new byte[768];

    public List<Color> colors = new List<Color>();
}
