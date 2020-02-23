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

/*
 * Quake 2 surface flags.
 */

[Flags]
public enum SurfFlags
{
    SURF_NONE =      0x0,
    SURF_LIGHT =     0x1,       // value will hold the light strength

    SURF_SLICK =     0x2,       // effects game physics

    SURF_SKY =       0x4,       // don't draw, but add to skybox
    SURF_WARP =      0x8,       // turbulent water warp
    SURF_TRANS33 =   0x10,
    SURF_TRANS66 =   0x20,
    SURF_FLOWING =   0x40,      // scroll towards angle
    SURF_NODRAW =    0x80,      // don't bother referencing the texture

    SURF_ALPHATEST = 0x02000000,// used by kmquake2

    MASK_TRANSPARENT = SURF_TRANS33 | SURF_TRANS66,
    MASK_NOLIGHTMAP = MASK_TRANSPARENT | SURF_SKY | SURF_NODRAW | SURF_WARP
}