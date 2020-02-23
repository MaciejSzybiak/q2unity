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
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/*
* Key binding management.
*/

public class BindManager : MonoBehaviour
{
    private static Dictionary<KeyCode, BindRegistration> binds = new Dictionary<KeyCode, BindRegistration>();
    private static string[] keylist;

    #region singleton

    public static BindManager Instance
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (Instance)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    #endregion

    private void Start()
    {
        keylist = Enum.GetNames(typeof(KeyCode));
    }

    private void Update()
    {
        //use new input system for console toggle (fixes some keyboard layouts)
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            CommandManager.RunCommand("toggleconsole", null);
            return;
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CommandManager.RunCommand("togglemenu", null);
            return;
        }
        InputContainer.ResetKeypressState();
        RunBinds(Cvar.Boolean("console_open") || Globals.menuopen.Boolean);
    }

    private void RunBinds(bool consoleOpen)
    {
        foreach(var kv in binds)
        {
            if(kv.Value.BindType == BindType.toggle && Input.GetKeyDown(kv.Key) && !(consoleOpen && kv.Key != KeyCode.BackQuote && consoleOpen && kv.Key != KeyCode.Escape))
            {
                CommandManager.RunCommandString(kv.Value.Command);
            }
            else if(kv.Value.BindType == BindType.hold)
            {
                if (Input.GetKeyUp(kv.Key) || consoleOpen)
                {
                    CommandManager.RunCommandString(kv.Value.Command + " up");
                }
                else if (Input.GetKey(kv.Key))
                {
                    
                    CommandManager.RunCommandString(kv.Value.Command);
                }
            }
        }
    }

    public static BindRegistration[] GetBinds()
    {
        return binds.Select(i => i.Value).Where(i => !i.locked).ToArray();
    }

    public static bool TryFindBindKey(string command, out KeyCode key)
    {
        var pair = binds.FirstOrDefault(i => i.Value.Command == command);
        key = pair.Key;

        return !pair.Equals(default(KeyValuePair<KeyCode, BindRegistration>));
    }

    /// <summary>
    /// Edits or creates a new bind.
    /// </summary>
    public static bool SetBind(string key, string command, BindType type, bool locked)
    {
        if(Enum.TryParse(key, out KeyCode kc))
        {
            return SetBind(kc, command, type, locked);
        }
        LogBadKey(key);
        return false;
    }

    /// <summary>
    /// Edits or creates a new bind.
    /// </summary>
    public static bool SetBind(KeyCode key, string command, BindType type, bool locked)
    {
        if(key == KeyCode.Escape)
        {
            Console.Log("Key \"" + key.ToString() + "\" is locked");
            return false;
        }
        if (binds.TryGetValue(key, out BindRegistration bind))
        {
            if (bind.locked)
            {
                LogLockedBind(bind.Command, key.ToString());
                return false;
            }
            bind.Command = command;
            bind.BindType = type;
        }
        else
        {
            binds.Add(key, new BindRegistration(key, command, type, locked));
        }
        return true;
    }

    public static bool RemoveBind(string key)
    {
        if (Enum.TryParse(key, out KeyCode kc))
        {
            RemoveBind(kc);
            return true;
        }
        else
        {
            LogBadKey(key);
            return false;
        }
    }

    public static void RemoveBind(KeyCode key)
    {
        if (binds.ContainsKey(key))
        {
            binds.Remove(key);
        }
    }

    public static void RemoveBindsOfCommand(string command)
    {
        if(!binds.Any(i => i.Value.Command == command && !i.Value.locked))
        {
            return;
        }
        KeyCode[] keys = binds.Where(i => i.Value.Command == command && !i.Value.locked).Select(i => i.Key).ToArray();

        foreach(KeyCode k in keys)
        {
            binds.Remove(k);
        }
    }

    public static bool TryGetBindCommand(string key, out string command)
    {
        if (Enum.TryParse(key, out KeyCode kc))
        {
            return TryGetBindCommand(kc, out command);
        }
        LogBadKey(key);
        command = "";
        return false;
    }

    public static bool TryGetBindCommand(KeyCode key, out string command)
    {
        if(binds.TryGetValue(key, out BindRegistration b))
        {
            command = b.Command;
            return true;
        }
        LogNoBind(key.ToString());
        command = "";
        return false;
    }

    public static string[] GetBindKeyAutocompletions(string text)
    {
        if(text == "")
        {
            return keylist;
        }
        return keylist.Where(i => i.StartsWith(text)).ToArray();
    }

    public static string[] GetAllBindPairs()
    {
        int dist = binds.Values.Select(item => item.Keystring).OrderByDescending(item => item.Length).First().Length;
        dist++;

        string[] list = new string[binds.Count];

        int i = 0;
        int len;
        string key;
        foreach(var b in binds)
        {
            key = b.Key.ToString();
            len = key.Length;

            list[i] = key + new string(' ', dist - len) + b.Value.Command;
            i++;
        }

        return list;
    }

    private static void LogBadKey(string key)
    {
        Console.LogWarning("Incorrect key: " + key + ".");
#if DEBUG
        Console.Log(Environment.StackTrace);
#endif
    }

    private static void LogNoBind(string key)
    {
        Console.Log("Key \"" + key + "\" is not bound.");
    }

    private static void LogLockedBind(string command, string key)
    {
        Console.Log("Key \"" + key + "\" is locked to command \"" + command + "\".");
    }
}
