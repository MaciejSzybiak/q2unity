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
 * Quake 2 brush contents.
 */

[Flags]
public enum BrushContents
{
    CONTENTS_SOLID =         1,             // an eye is never valid in a solid
    CONTENTS_WINDOW =        2,             // translucent, but not watery
    CONTENTS_AUX =           4,
    CONTENTS_LAVA =          8,
    CONTENTS_SLIME =         16,
    CONTENTS_WATER =         32,
    CONTENTS_MIST =          64,
    LAST_VISIBLE_CONTENTS =  64,

    // remaining contents are non-visible, and don't eat brushes

    CONTENTS_AREAPORTAL =    0x8000,

    CONTENTS_PLAYERCLIP =    0x10000,
    CONTENTS_MONSTERCLIP =   0x20000,

    // currents can be added to any other contents, and may be mixed
    CONTENTS_CURRENT_0 =     0x40000,
    CONTENTS_CURRENT_90 =    0x80000,
    CONTENTS_CURRENT_180 =   0x100000,
    CONTENTS_CURRENT_270 =   0x200000,
    CONTENTS_CURRENT_UP =    0x400000,
    CONTENTS_CURRENT_DOWN =  0x800000,

    CONTENTS_ORIGIN =        0x1000000,     // removed before bsping an entity
    
    CONTENTS_MONSTER =       0x2000000,     // should never be on a brush, only in game
    CONTENTS_DEADMONSTER =   0x4000000,
    CONTENTS_DETAIL =        0x8000000,     // brushes to be added after vis leafs
    CONTENTS_TRANSLUCENT =   0x10000000,    // auto set if any surface has trans
    CONTENTS_LADDER =        0x20000000,

    MASK_PLAYERSOLID = CONTENTS_SOLID | CONTENTS_PLAYERCLIP | CONTENTS_WINDOW | CONTENTS_MONSTER,
    MASK_WATER = CONTENTS_WATER | CONTENTS_LAVA | CONTENTS_SLIME
}
