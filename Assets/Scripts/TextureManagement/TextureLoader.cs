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
using System;
using System.IO;
using System.Linq;
using UnityEngine;

/*
 * Automated texture search and loading class.
 * Tries to find a texture with all supported extensions
 * before trying to find a .wal texture. Textures are
 * referenced using a GUID.
 */

public static class TextureLoader
{
    public static PCXFile colormap;

    public static Dictionary<Guid, WalFile> walTextures = new Dictionary<Guid, WalFile>();
    public static Dictionary<Guid, OtherTexture> otherTextures = new Dictionary<Guid, OtherTexture>();
    public static Dictionary<Guid, TGAFile> tgaTextures = new Dictionary<Guid, TGAFile>();

    private static List<string> nonexistentFilenames = new List<string>();

    //generate this instead?
    public static readonly byte[] emptyTex =
    {
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 100, 100, 100, 100, 100, 100, 0,
        0, 100, 100, 100, 100, 100, 100, 0,
        0, 100, 100, 100, 100, 100, 100, 0,
        0, 100, 100, 100, 100, 100, 100, 0,
        0, 100, 100, 100, 100, 100, 100, 0,
        0, 100, 100, 100, 100, 100, 100, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
    };

    public static void LoadColormapFromPak()
    {
        colormap = new PCXFile();

        byte[] rawdata;

        if (ResourceLoader.LoadFile("pics/colormap.pcx", out rawdata))
        {
            Stream stream = new MemoryStream(rawdata);
            BinaryReader reader = new BinaryReader(stream);

            colormap.identifier = reader.ReadByte();
            colormap.version = reader.ReadByte();
            colormap.encoding = reader.ReadByte();
            colormap.bitsPerPixel = reader.ReadByte();
            colormap.xmin = reader.ReadInt16();
            colormap.ymin = reader.ReadInt16();
            colormap.xmax = reader.ReadInt16();
            colormap.ymax = reader.ReadInt16();
            colormap.xdpi = reader.ReadInt16();
            colormap.ydpi = reader.ReadInt16();
            colormap.hpalette = reader.ReadBytes(48);
            colormap.reserved = reader.ReadByte();
            colormap.planes = reader.ReadByte();
            colormap.bytesPerLine = reader.ReadInt16();
            colormap.paletteType = reader.ReadInt16();
            colormap.hScreenSize = reader.ReadInt16();
            colormap.vScreenSize = reader.ReadInt16();
            colormap.reserved2 = reader.ReadBytes(54);

            colormap.width = colormap.xmax - colormap.xmin + 1;
            colormap.height = colormap.ymax - colormap.ymin + 1;

            reader.BaseStream.Position = reader.BaseStream.Length - 768;
            colormap.qpalette = reader.ReadBytes(768);

            Color c;
            for(int i = 0; i < colormap.qpalette.Length; i += 3)
            {
                c = new Color(
                    (float)colormap.qpalette[i] / 255f,
                    (float)colormap.qpalette[i + 1] / 255f,
                    (float)colormap.qpalette[i + 2] / 255f);

                colormap.colors.Add(c);
            }
        }
    }

    public static Guid LoadTexture(string filename)
    {
        if (nonexistentFilenames.Any(i => i.Equals(filename)))
        {
            return Guid.Empty;
        }

        try
        {
            WalFile wal = walTextures.FirstOrDefault(i => i.Value.filename == filename).Value;
            if (wal != null)
            {
                return wal.GUID;
            }
            OtherTexture other = otherTextures.FirstOrDefault(i => i.Value.filename == filename).Value;
            if (other != null)
            {
                return other.GUID;
            }

            //try loading jpg, png, tga
            if(TryLoadOtherFromFiles(filename, out Guid newguid))
            {
                return newguid;
            }
            //load wal
            if (ResourceLoader.LoadFile(filename, out byte[] rawdata))
            {
                return ReadParseWal(filename, rawdata);
            }
        }
        catch (Exception)
        {
            Console.LogWarning("Couldn't load texture " + filename + ". The file is locked or corrupted.");
        }

        nonexistentFilenames.Add(filename);
        Console.LogWarning("Couldn't load texture: " + filename);
        return Guid.Empty;
    }

    private static Guid ReadParseWal(string filename, byte[] rawdata)
    {
        WalFile wal = new WalFile();
        wal.GUID = Guid.NewGuid();

        Stream stream = new MemoryStream(rawdata);
        BinaryReader reader = new BinaryReader(stream);

        //parse the header
        wal.name = reader.ReadChars(32);

        reader.BaseStream.Position = 32;

        wal.width = reader.ReadUInt32();
        wal.height = reader.ReadUInt32();

        if(wal.width > 2048 || wal.height > 2048)
        {
            Console.LogWarning("Bad wal file.");
            reader.Close();
            return Guid.Empty;
        }

        for (int i = 0; i < 4; i++)
        {
            wal.offset[i] = reader.ReadInt32();
        }

        wal.next_name = reader.ReadChars(32);

        wal.flags = reader.ReadUInt32();
        wal.contents = reader.ReadUInt32();
        wal.value = reader.ReadUInt32();

        reader.BaseStream.Position = wal.offset[0];
        wal.mip0_data = reader.ReadBytes((int)(wal.width * wal.height));

        reader.BaseStream.Position = wal.offset[1];
        wal.mip1_data = reader.ReadBytes((int)(wal.width * wal.height) / 2);

        reader.BaseStream.Position = wal.offset[2];
        wal.mip2_data = reader.ReadBytes((int)(wal.width * wal.height) / 4);

        reader.BaseStream.Position = wal.offset[3];
        wal.mip3_data = reader.ReadBytes((int)(wal.width * wal.height) / 8);

        int colorlen = wal.mip0_data.Length * 4;
        wal.colordata = new byte[colorlen];

        Color c;
        for (int i = 0; i < colorlen; i += 4)
        {
            c = colormap.colors[wal.mip0_data[i / 4]];

            wal.colordata[i] = (byte)(c.r * 255);
            wal.colordata[i + 1] = (byte)(c.g * 255);
            wal.colordata[i + 2] = (byte)(c.b * 255);
            wal.colordata[i + 3] = (byte)(c.a * 255);
        }

        wal.tex = new Texture2D((int)wal.width, (int)wal.height, TextureFormat.RGBA32, true);
        wal.tex.LoadRawTextureData(wal.colordata.Concat(wal.mip0_data).Concat(wal.mip1_data).Concat(wal.mip2_data).Concat(wal.mip3_data).ToArray()); //FIXME: does this load garbage into mips?
        wal.tex.filterMode = FilterMode.Trilinear;
        wal.tex.Apply(true);

        wal.filename = filename;

        walTextures.Add(wal.GUID, wal);

        reader.Close();
        return wal.GUID;
    }

    private static bool TryLoadOtherFromFiles(string filename, out Guid guid)
    {
        //strip the .wal extension
        filename = filename.Remove(filename.Length - 4);

        byte[] rawdata;

        if (ResourceLoader.LoadFile(filename + ".jpg", out rawdata) || 
            ResourceLoader.LoadFile(filename + ".png", out rawdata))
        {
            OtherTexture other;

            other = new OtherTexture();
            other.GUID = Guid.NewGuid();

            other.tex = new Texture2D(2, 2, TextureFormat.BGRA32, true);
            other.tex.filterMode = FilterMode.Trilinear;
            other.tex.LoadImage(rawdata);

            other.filename = filename;

            Vector2 dim = GetDimensionsForOtherTex(filename + ".wal");
            other.width = (uint)dim.x;
            other.height = (uint)dim.y;

            otherTextures.Add(other.GUID, other);

            guid = other.GUID;
            return true;
        }
        //try tga
        else if(ResourceLoader.LoadFile(filename + ".tga", out rawdata))
        {
            guid = ReadParseTga(filename, rawdata);
            return true;
        }
        guid = Guid.Empty;
        return false;
    }

    private static Guid ReadParseTga(string filename, byte[] rawdata)
    {
        byte[] imgdata;
        int dims;
        int bpp;

        Stream stream = new MemoryStream(rawdata);
        BinaryReader reader = new BinaryReader(stream);

        TGAFile tga = new TGAFile();
        tga.GUID = Guid.NewGuid();

        tga.idLength = reader.ReadByte();
        tga.colormapType = reader.ReadByte();
        tga.imageType = reader.ReadByte();
        
        //skip the useless stuff
        reader.BaseStream.Position = 12;

        tga.width = reader.ReadUInt16();
        tga.height = reader.ReadUInt16();
        tga.pixelSize = reader.ReadByte();
        tga.attributes = reader.ReadByte();

        //skip tga comment
        reader.BaseStream.Position = 18 + tga.idLength; //tga header is 18 bytes

        //check if the file is valid
        if(tga.colormapType != 0)
        {
            Console.LogWarning("Colormap TGA images are not supported.");
            reader.Close();
            return Guid.Empty;
        }
        if(tga.width < 1 || tga.height < 1 || tga.width > 16384 || tga.height > 16384)
        {
            Console.LogWarning("Invalid TGA dimensions: " + tga.width + "x" + tga.height);
            reader.Close();
            return Guid.Empty;
        }


        if(tga.pixelSize == 32)
        {
            bpp = 4;
        }
        else if(tga.pixelSize == 24)
        {
            bpp = 3;
        }
        else
        {
            Console.LogWarning("Incorrect tga bit depth: " + tga.pixelSize);
            reader.Close();
            return Guid.Empty;
        }

        dims = (int)(tga.width * tga.height);

        //validate file length
        if (18 + tga.idLength + dims * bpp > reader.BaseStream.Length && tga.imageType != 10)
        {
            if(tga.imageType != 2)
            {
                Console.LogWarning("Unsupported TGA image type " + tga.imageType);
            }
            else
            {
                Console.LogWarning("Unexpected TGA file end.");
            }
            reader.Close();
            return Guid.Empty;
        }

        //generate row indices
        int[] rowIndices = new int[tga.height];

        if((tga.attributes & 32) != 0)
        {
            for(int i = 0; i < tga.height; i++)
            {
                rowIndices[i] = i * (int)tga.width * bpp;
            }
        }
        else
        {
            for (int i = 0; i < tga.height; i++)
            {
                rowIndices[i] = ((int)tga.height - i - 1) * (int)tga.width * bpp;
            }
        }

        //check image type
        if (tga.imageType == 2)
        {
            imgdata = DecodeTGA(reader.ReadBytes(dims * bpp), (int)tga.width, (int)tga.height, bpp, rowIndices);
            tga.tex = new Texture2D((int)tga.width, (int)tga.height, bpp == 4 ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tga.tex.LoadRawTextureData(imgdata);
        }
        else if(tga.imageType == 10)
        {
            imgdata = DecodeTGARLE(reader.ReadBytes(dims * bpp), (int)tga.width, (int)tga.height, bpp, rowIndices);
            tga.tex = new Texture2D((int)tga.width, (int)tga.height, bpp == 4 ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tga.tex.LoadRawTextureData(imgdata);
        }
        else
        {
            Console.LogWarning("Unsupported TGA image type.");
            reader.Close();
            return Guid.Empty;
        }

        tga.tex.filterMode = FilterMode.Point;
        tga.tex.Apply();

        reader.Close();

        tgaTextures.Add(tga.GUID, tga);
        return tga.GUID;
    }

    private static byte[] DecodeTGARLE(byte[] rawdata, int width, int height, int bpp, int[] rowIndices)
    {
        byte[] pixels = new byte[bpp];
        int decodeLength = bpp * width * height;
        byte[] output = new byte[decodeLength];
        int decoded = 0;
        int offset = 0;

        while(decoded < decodeLength)
        {
            int packet = rawdata[offset++] & 0xff;
            if((packet & 0x80) != 0)
            {
                for(int i = 0; i < bpp; i++)
                {
                    pixels[i] = rawdata[offset++];
                }
                int count = 1 + (packet & 0x7f);

                for(int i = 0; i < count; i++)
                {
                    for(int j = 0; j < bpp; j++)
                    {
                        output[decoded++] = pixels[j];
                    }
                }
            }
            else
            {
                int count = (packet + 1) * bpp;
                for(int i = 0; i< count; i++)
                {
                    output[decoded++] = rawdata[offset++];
                }
            }
        }

        //swap B and R
        byte v;
        for(int c = 0; c < decodeLength; c += bpp)
        {
            v = output[c];
            output[c] = output[c + 2];
            output[c + 2] = v;
        }

        return output;
    }

    private static byte[] DecodeTGA(byte[] rawdata, int width, int height, int bpp, int[] rowIndices)
    {
        int index;
        byte[] pixels = new byte[bpp];
        int decodeLength = bpp * width * height;
        byte[] output = new byte[decodeLength];
        int rIndex = 0;

        for (int row = 0; row < height; row++)
        {
            index = rowIndices[height - row - 1];
            for (int i = 0; i < width; i++, index += bpp, rIndex += bpp)
            {
                output[index + 2] = rawdata[rIndex];
                output[index + 1] = rawdata[rIndex + 1];
                output[index] = rawdata[rIndex + 2];
                if(bpp == 4)
                {
                    output[index + 3] = rawdata[rIndex + 3];
                }
            }
        }

        return output;
    }

    private static Vector2 GetDimensionsForOtherTex(string filename)
    {
        if (ResourceLoader.LoadFile(filename, out byte[] rawdata))
        {
            Vector2 v = new Vector2();

            Stream stream = new MemoryStream(rawdata);
            BinaryReader reader = new BinaryReader(stream);
            reader.ReadChars(32);

            v.x = reader.ReadUInt32();
            v.y = reader.ReadUInt32();

            return v;
        }
        else
        {
            Console.Log("No matching .wal for " + filename);
            return Vector2.one;
        }
    }

    public static void GenerateNormalMaps()
    {
        foreach(var pair in walTextures)
        {
            if (pair.Value.tex)
            {
                pair.Value.normalmap = GenerateNormalMap(pair.Value.tex);
            }
        }

        foreach(var pair in otherTextures)
        {
            if (pair.Value.tex)
            {
                pair.Value.normalmap = GenerateNormalMap(pair.Value.tex);
            }
        }
    }

    private static Texture2D GenerateNormalMap(Texture2D tex)
    {
        Texture2D normal = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
        int w = tex.width;
        int h = tex.height;

        float xplus, xminus, yplus, yminus;
        float dx, dy, sqrt;

        byte[] rawdata = tex.GetRawTextureData();
        float[] gs = new float[rawdata.Length / 3];
        float[,] grayscale = new float[w, h];
        float[,] vx = new float[w, h];
        float[,] vy = new float[w, h];
        float[,] vz = new float[w, h];
        byte[] rgb = new byte[w * h * 3];

        float[] vx1;
        float[] vy1;
        float[] vz1;
        
        if(rawdata.Length < 3)
        {
            return null;
        }
        for(int i = 0; i < rawdata.Length - 4; i += 4)
        {
            gs[i / 4] = (float)(rawdata[i] + rawdata[i + 1] + rawdata[i + 2]) / 3;
        }

        //calculate normal vectors
        for(int y = 0; y < h; y += 1)
        {
            for (int x = 0; x < w; x += 1)
            {
                grayscale[x, y] = gs[x * h + y];
            }
        }

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                //get neighbor pixel values
                if(x == 0)
                {
                    xplus = grayscale[1, y];
                    xminus = grayscale[w - 1, y];
                }
                else if(x == w - 1)
                {
                    xminus = grayscale[w - 1, y];
                    xplus = grayscale[0, y];
                }
                else
                {
                    xplus = grayscale[x + 1, y];
                    xminus = grayscale[x - 1, y];
                }

                if (y == 0)
                {
                    yplus = grayscale[x, 1];
                    yminus = grayscale[x, h - 1];
                }
                else if (y == tex.height - 1)
                {
                    yminus = grayscale[x, h - 1];
                    yplus = grayscale[x, 0];
                }
                else
                {
                    yplus = grayscale[x, y + 1];
                    yminus = grayscale[x, y - 1];
                }

                //get derivatives
                dx = xplus - xminus;
                dy = yplus - yminus;

                //calculate vector values
                sqrt = Mathf.Sqrt(dx * dx + dy * dy + 1);

                vx[x, y] = (((-dx) / sqrt) + 1) / 2;
                vy[x, y] = (((-dy) / sqrt) + 1) / 2;
                vz[x, y] = 1 / sqrt;
            }
        }

        //lazy
        vx1 = vx.Cast<float>().ToArray();
        vy1 = vy.Cast<float>().ToArray();
        vz1 = vz.Cast<float>().ToArray();

        for (int i = 0; i < rgb.Length; i += 3)
        {
            rgb[i] = (byte)(255 - vx1[i / 3] * 255);
            rgb[i + 1] = (byte)(255 - vy1[i / 3] * 255);
            rgb[i + 2] = (byte)(255 - vz1[i / 3] * 255);
        }

        normal.LoadRawTextureData(rgb);
        normal.filterMode = FilterMode.Trilinear;
        normal.Apply();

        return normal;
    }

    public static Texture2D GetTexture(Guid guid)
    {
        if(TryGetOtherTexture(guid, out OtherTexture o))
        {
            return o.tex;
        }
        if(TryGetWalTexture(guid, out WalFile w))
        {
            return w.tex;
        }
        if(TryGetTgaTexture(guid, out TGAFile t))
        {
            return t.tex;
        }
        return null;
    }

    public static Texture2D GetNormalTexture(Guid guid)
    {
        if (TryGetOtherTexture(guid, out OtherTexture o))
        {
            return o.normalmap;
        }
        if (TryGetWalTexture(guid, out WalFile w))
        {
            return w.normalmap;
        }
        return null;
    }

    public static Guid GetGuid(string filename)
    {
        var w = walTextures.FirstOrDefault(i => i.Value.filename.Contains(filename)).Value;
        if (w != null)
        {
            return w.GUID;
        }
        var o = otherTextures.FirstOrDefault(i => i.Value.filename.Contains(filename)).Value;
        if (o != null)
        {
            return o.GUID;
        }

        return Guid.Empty;
    }

    public static bool TryGetWalTexture(Guid guid, out WalFile wal)
    {
        return walTextures.TryGetValue(guid, out wal);
    }

    public static bool TryGetOtherTexture(Guid guid, out OtherTexture other)
    {
        return otherTextures.TryGetValue(guid, out other);
    }

    public static bool TryGetTgaTexture(Guid guid, out TGAFile other)
    {
        return tgaTextures.TryGetValue(guid, out other);
    }

    public static bool HasTexture(string filename)
    {
        return walTextures.Any(i => i.Value.filename == filename) || otherTextures.Any(i => i.Value.filename == filename);
    }

    public static bool HasTexture(Guid guid)
    {
        return walTextures.Any(i => i.Key == guid) || otherTextures.Any(i => i.Key == guid);
    }

    public static Vector2 GetTextureDimensions(Guid guid)
    {
        if (TryGetOtherTexture(guid, out OtherTexture o))
        {
            return new Vector2(o.width, o.height);
        }
        if (TryGetWalTexture(guid, out WalFile w))
        {
            return new Vector2(w.width, w.height);
        }
        else return Vector2.one;
    }

    public static void UnloadTexture(Guid guid)
    {
        if(walTextures.ContainsKey(guid))
        {
            walTextures.Remove(guid);
        }
        else if (otherTextures.ContainsKey(guid))
        {
            otherTextures.Remove(guid);
        }
        else if (tgaTextures.ContainsKey(guid))
        {
            tgaTextures.Remove(guid);
        }
    }
}
