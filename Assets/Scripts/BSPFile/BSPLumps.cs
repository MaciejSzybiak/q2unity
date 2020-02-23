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

/*
 * All lump types that can be found in a BSP file.
 */

public enum BSPLumps
{
    LUMP_ENTSTRING = 0,
    LUMP_PLANES = 1,
    LUMP_VERTEXES = 2,
    LUMP_VISIBILITY = 3,
    LUMP_NODES = 4,
    LUMP_TEXINFO = 5,
    LUMP_FACES = 6,
    LUMP_LIGHTING = 7,
    LUMP_LEAFS = 8,
    LUMP_LEAFFACES = 9,
    LUMP_LEAFBRUSHES = 10,
    LUMP_EDGES = 11,
    LUMP_SURFEDGES = 12,
    LUMP_MODELS = 13,
    LUMP_BRUSHES = 14,
    LUMP_BRUSHSIDES = 15,
    LUMP_POP = 16,
    LUMP_AREAS = 17,
    LUMP_AREAPORTALS = 18
}