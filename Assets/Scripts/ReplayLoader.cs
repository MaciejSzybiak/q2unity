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

using System.IO;
using UnityEngine;

/*
 * Used to load Quake 2 Jump Mod replay files.
 */

public class ReplayFile
{
    public ReplayFrame[] replayFrames;
}

public struct ReplayFrame
{
    public Vector3 angle;
    public Vector3 origin;

    public int framenum;
}

public class ReplayLoader
{
    public ReplayFile LoadReplay(string filename, float scale)
    {
        float v1, v2, v3;

        if (!File.Exists(filename))
        {
            return null;
        }

        BinaryReader reader;
        try
        {
            reader = new BinaryReader(File.OpenRead(filename));
        }
        catch (IOException e)
        {
            Console.LogIOException("LoadReplay", e);
            return null;
        }

        long len = reader.BaseStream.Length;

        int framesize = sizeof(float) * 6 + sizeof(int);

        if(len % framesize > 0)
        {
            Console.DebugLog("Incorrect goblin replay size!");
            return null;
        }

        Console.DebugLog("Goblin replay size: " + len + " frames");
        len /= framesize;

        ReplayFile r = new ReplayFile()
        {
            replayFrames = new ReplayFrame[len]
        };

        for(long i = 0; i < len; i++)
        {
            ReplayFrame f = new ReplayFrame();

            v1 = reader.ReadSingle();
            v2 = reader.ReadSingle();
            v3 = reader.ReadSingle();

            f.angle = new Vector3(v1, -v2 + 90, v3); //eh?

            v1 = reader.ReadSingle();
            v2 = reader.ReadSingle();
            v3 = reader.ReadSingle();

            f.origin = new Vector3(v1, v3, v2) * scale;

            f.framenum = reader.ReadInt32();

            r.replayFrames[i] = f;
        }
        return r;
    }
}
