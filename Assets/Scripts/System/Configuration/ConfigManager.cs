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
using UnityEngine;

/*
 * Responsible for loading and applying a configuration file
 * or creating/overwriting an existing one with the current
 * settings.
 */

public static class ConfigManager
{
    public static bool DefaultConfigExists()
    {
        string filename = Application.dataPath + "/configs/" + "default" + ".conf";

        if (File.Exists(filename))
        {
            return true;
        }
        Console.LogWarning("Default config not found, please review and save your settings.");
        return false;
    }

    public static void SaveConfig(string[] args)
    {
        int i, j;
        int linecount = 0;
        string file = "";

        //set culture
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        //write cvars
        ConfVar[] cvars = Cvar.GetDefaultCvars();
        for (i = 0; i < cvars.Length; i++)
        {
            //skip debug cvars
            if (cvars[i].Name.StartsWith("debug_"))
            {
                continue;
            }
            file += "cvar " + cvars[i].Name + " \"" + cvars[i].Text + "\"\n";
            linecount++;
        }

        //write binding
        BindRegistration[] binds = BindManager.GetBinds();
        for(j = 0; j < binds.Length; j++)
        {
            file += "bind " + binds[j].Keystring + " \"" + binds[j].Command + "\"\n";
            linecount++;
        }

        //write config
        string foldername = Application.dataPath + "/configs/";
        string filename = foldername + ((args != null && args.Length > 0) ? args[0] : "default") + ".conf";

        Console.LogInfo("Writing " + filename + " with " + (i + j + 2) + " config lines.");

        if (!Directory.Exists(foldername))
        {
            Directory.CreateDirectory(foldername);
        }

        try
        {
            File.WriteAllText(filename, file);
        }
        catch (IOException e)
        {
            Console.LogIOException("SaveConfig", e);
        }
    }

    public static void LoadConfig(string[] args)
    {
        //set culture
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        string foldername = Application.dataPath + "/configs/";
        string filename = foldername + (args.Length > 0 ? args[0] : "default") + ".conf";

        if (File.Exists(filename))
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(filename);
            }
            catch (IOException e)
            {
                Console.LogIOException("LoadConfig", e);
                return;
            }

            string[] split;
            Dictionary<string, string> bindings = new Dictionary<string, string>();

            foreach(string line in lines)
            {
                if (line == "" || line.StartsWith("//"))
                {
                    continue;
                }

                split = line.Split('"').Select((element, index) => index % 2 == 0 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  : new string[] { element })
                     .SelectMany(element => element).ToArray();

                if (split.Length != 3)
                {
                    Console.LogWarning("Cannot parse config line: " + line);
                    continue;
                }
                switch (split[0])
                {
                    case "cvar":
                        Cvar.Get(split[1], split[2]);
                        break;
                    case "bind":
                        if(split[2] == "toggleconsole" || split[1] == KeyCode.Escape.ToString())
                        {
                            break;
                        }
                        //clear binds from config
                        BindManager.RemoveBindsOfCommand(split[2]);
                        bindings.Add(split[1], split[2]);
                        break;
                    default:
                        Console.LogWarning("Cannot parse config line: " + line);
                        break;
                }
            }
            //set binds
            foreach (var b in bindings)
            {
                BindManager.SetBind(b.Key, b.Value, b.Value.StartsWith("+") ? BindType.hold : BindType.toggle, false);
            }
        }
        else
        {
            Console.LogWarning("No config file " + (args.Length > 0 ? args[0] : "default") + ".conf");
        }
    }
}
