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
 * Stores BSP leaf data.
 */

public class BSPLeaf : BSPNode
{
    public BrushContents contents;

    public ushort cluster;
    public ushort area;

    public ushort first_brush;
    public ushort num_brushes;

    public int index;

    public BSPBrush FirstBrush
    {
        get
        {
            return BSPFile.leafbrushes[first_brush];
        }
    }

    private BSPBrush[] brushCache;

    public BSPBrush[] Brushes
    {
        get
        {
            if(brushCache == null)
            {
                //cache brushes for this leaf
                brushCache = new BSPBrush[num_brushes];

                for(int i = 0; i < num_brushes; i++)
                {
                    brushCache[i] = BSPFile.leafbrushes[first_brush + i];
                }
            }

            return brushCache;
        }
    }
}
