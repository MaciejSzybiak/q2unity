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
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
 * Loads and stores a specified pak file information.
 * Also provides a method for fetching files from that pak file.
 */

public struct PakHeader
{
    public uint ident;
    public uint offset;
    public uint size;
}

public class PakEntry
{
    public string name;
    public uint offset;
    public uint size;
}

public class PakFile
{
    public bool IsPakLoaded
    {
        get;
        private set;
    }

    private PakHeader header;

    public List<PakEntry> Files
    {
        get;
        private set;
    }

    BinaryReader reader; //keep the reader open for later file access

    ~PakFile()
    {
        reader?.Close(); //Close the reader when the pak is no longer in use.
    }

    public void LoadPak(string filename)
    {
        try
        {

        reader?.Close();

        if (!File.Exists(filename))
        {
            IsPakLoaded = false;
            return;
        }

        try
        {
            reader = new BinaryReader(File.Open(filename, FileMode.Open));
        }
        catch(IOException)
        {
            Console.LogError("Loading pak " + filename + " failed. Make sure the file isn't locked (is Quake II running in the background?)");
            IsPakLoaded = false;
            return;
        }

        Files = new List<PakEntry>();

        header = new PakHeader
        {
            ident = reader.ReadUInt32(),
            offset = reader.ReadUInt32(),
            size = reader.ReadUInt32()
        };

        int filecount = (int)header.size / 64;

        reader.BaseStream.Position = header.offset;

        for(int i = 0; i < filecount; i++)
        {
            Files.Add(new PakEntry
            {
                name = new string(reader.ReadChars(56)),
                offset = reader.ReadUInt32(),
                size = reader.ReadUInt32()
            });
        }

        IsPakLoaded = true;
        }
        catch (Exception e)
        {
            Console.LogError("Pak exception: " + e.Message + e.StackTrace);
            IsPakLoaded = false;
        }
    }

    public void UnloadPak()
    {
        IsPakLoaded = false;
        Files.Clear();

        reader?.Close();
    }

    public bool LoadFileFromPak(string filename, out byte[] data)
    {
        data = new byte[1];
        if (!IsPakLoaded)
        {
            return false;
        }

        PakEntry file;
        try
        {
            file = Files.First(i => i.name.Contains(filename));
        }
        catch (Exception)
        {
            return false;
        }

        reader.BaseStream.Position = file.offset;

        data = reader.ReadBytes((int)file.size);

        return true;
    }
}
