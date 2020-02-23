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
using System.IO;
using System.Linq;
using UnityEditor;

/*
 * Manages the pak file list and provides methods for 
 * automated file search in game and mod directories.
 */

public static class ResourceLoader
{
    private static bool initialized = false;
    private static List<PakFile> paks = new List<PakFile>();

    public static void Init()
    {
        if (initialized)
        {
            for (int i = 0; i < paks.Count; i++)
            {
                paks[i].UnloadPak();
            }
            paks.Clear();
        }
        else
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        if(!Utils.IsGamePathValid())
        {
            Console.LogError("ResourceLoader: Couldn't verify game folder. Fix your paths!");
            initialized = true;
            return;
        }
        if (!Utils.IsModnameValid())
        {
            Console.LogWarning("ResourceLoader: Invalid modname!");
        }

        //find paks and load them
        string gamepath = Globals.gamepath.Text;
        string pattern = "*.pak";
        List<FileInfo> pakfiles = new List<FileInfo>();

        //load moddir paks first (higher priority) - skip when mod is baseq2...
        if(Globals.modname.Text != "baseq2")
        {
            DirectoryInfo moddir = new DirectoryInfo(Globals.gamepath.Text + "/" + Globals.modname.Text);
            pakfiles.AddRange(moddir.GetFiles(pattern, SearchOption.TopDirectoryOnly));
        }

        DirectoryInfo baseq2 = new DirectoryInfo(Globals.gamepath.Text + "/baseq2");
        pakfiles.AddRange(baseq2.GetFiles(pattern, SearchOption.TopDirectoryOnly));

        foreach(FileInfo i in pakfiles)
        {
            PakFile p = new PakFile();
            p.LoadPak(gamepath + "/baseq2/" + i.Name);
            if (p.IsPakLoaded)
            {
                paks.Add(p);
            }
        }

        Console.DebugLog("Detected " + pakfiles.Count + " pak files containing " + paks.Select(i => i.Files.Count).Sum() + " files.");

        initialized = true;
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if(state == PlayModeStateChange.ExitingPlayMode)
        {
            for(int i = 0; i < paks.Count; i++)
            {
                paks[i].UnloadPak();
            }
            paks.Clear();
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filename">Local file path including file extension. Should use forward slashes.</param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static bool LoadFile(string filename, out byte[] data)
    {
        if (!initialized)
        {
            Init();
        }

        if(GetFileFromDir(filename.Replace('\\', '/'), out data))
        {
            return true;
        }

        foreach(PakFile p in paks)
        {
            if(p.LoadFileFromPak(filename, out data))
            {
                return true;
            }
        }

        return false;
    }

    private static bool GetFileFromDir(string filename, out byte[] data)
    {
        string path = Globals.gamepath.Text + "/" + Globals.modname.Text + "/" + filename;

        //try moddir
        if (!File.Exists(path))
        {
            path = Globals.gamepath.Text + "/baseq2/" + filename;

            //try baseq2
            if (!File.Exists(path))
            {
                data = null;
                return false;
            }
        }

        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (IOException e)
        {
            Console.LogIOException("GetFileFromDir", e);
            data = null;
            return false;
        }

        return true;
    }

    public static string[] GetFileNamesFromPrefix(string prefix)
    {
        List<string> filenames = new List<string>();

        foreach(PakFile p in paks)
        {
            if(p.Files.Any(i => i.name.StartsWith(prefix)))
            {
                //stip all but the file names
                filenames.AddRange(p.Files.Where(i => i.name.StartsWith(prefix))
                    .Select(i => i.name.Substring(i.name.LastIndexOf("/") + 1, i.name.LastIndexOf(".") - i.name.LastIndexOf("/") - 1)));
            }
        }

        var dirFiles = GetFileNamesFromDir(prefix);
        if (dirFiles.Length > 0)
        {
            filenames.AddRange(dirFiles);
        }
        return filenames.OrderBy(i => i).ToArray();
    }

    private static string[] GetFileNamesFromDir(string prefix)
    {
        prefix = prefix.Replace('\\', '/');
        string fullpath = Globals.gamepath.Text + "/" + Globals.modname.Text + "/" + prefix;
        
        if (prefix.EndsWith("/"))
        {
            if (!Directory.Exists(fullpath))
            {
                return new string[] { };
            }

            //get all file names
            return Directory.GetFiles(fullpath).Select(i => i.Replace('\\', '/')).
                Select(i => i.Substring(i.LastIndexOf("/") + 1, i.LastIndexOf(".") - i.LastIndexOf("/") - 1)).ToArray();
        }
        else
        {
            //get files starting with the prefix
            string name = prefix.Substring(prefix.LastIndexOf("/") + 1);
            if (!Directory.Exists(fullpath.Substring(0, fullpath.LastIndexOf("/") + 1)))
            {
                return new string[] { };
            }
            return Directory.GetFiles(fullpath.Substring(0, fullpath.LastIndexOf("/") + 1)).Select(i => i.Replace('\\', '/')).Select(i => i.Substring(i.LastIndexOf("/") + 1))
                .Where(i => i.StartsWith(name)).Select(i => i.Substring(0, i.LastIndexOf("."))).ToArray();
        }
    }
}
