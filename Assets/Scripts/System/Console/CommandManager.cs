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
using System.Linq;

/*
 * Registers, parses and executes commands.
 */

public delegate void CommandHandler(string[] args);

public static class CommandManager
{
    private static Dictionary<string, CommandRegistration> commands = new Dictionary<string, CommandRegistration>();

    public static bool RegisterCommand(string command, CommandHandler handler, string help, CommandType type)
    {
        if (!commands.ContainsKey(command))
        {
            commands.Add(command, new CommandRegistration(command, handler, help, type));
            return true;
        }
        return false;
    }

    public static string[] GetCommandAutocompletions(string text)
    {
        return commands.Where(i => i.Key.StartsWith(text) && i.Value.CommandType != CommandType.hidden).Select(i => i.Key).ToArray();
    }

    private static void RegisterPredefinedCommands()
    {
        RegisterCommand("quit", Quit_c, "quits the game", CommandType.normal);
        RegisterCommand("help", Help_c, "display command list", CommandType.normal);
        RegisterCommand("cvar", Cvar_c, "list and set cvars", CommandType.normal);
        RegisterCommand("bind", Bind_c, "list and set binds", CommandType.normal);
        RegisterCommand("unbind", Unbind_c, "remove binds", CommandType.normal);

        //movement
        RegisterCommand("+left", InputContainer.MoveLeft, "left movement", CommandType.normal);
        RegisterCommand("+right", InputContainer.MoveRight, "right movement", CommandType.normal);
        RegisterCommand("+forward", InputContainer.MoveForward, "forward movement", CommandType.normal);
        RegisterCommand("+back", InputContainer.MoveBack, "back movement", CommandType.normal);
        RegisterCommand("+jump", InputContainer.MoveUp, "up movement", CommandType.normal);
        RegisterCommand("+duck", InputContainer.MoveDown, "down movement", CommandType.normal);
    }

    //constructor
    static CommandManager()
    {
        RegisterPredefinedCommands();
    }

    #region running commands

    public static void RunCommandString(string command)
    {
        string[] parsed = ParseCommand(command);
        string[] args;

        if (parsed.Length == 0)
        {
            return;
        }

        if(parsed.Length > 1)
        {
            int argsCount = parsed.Length - 1;
            args = new string[argsCount];
            Array.Copy(parsed, 1, args, 0, argsCount);
        }
        else
        {
            args = new string[0];
        }
        RunCommand(parsed[0].ToLower(), args);
    }

    public static void RunCommand(string command, string[] args)
    {
        if(commands.TryGetValue(command, out CommandRegistration reg))
        {
            reg.Handler?.Invoke(args);
        }
        else
        {
            LogInvalidCommand(command);
        }
    }

    public static void LogInvalidCommand(string command)
    {
        Console.LogWarning("Command " + command + " is invalid!");
    }

    private static string[] ParseCommand(string command)
    {
        return command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool IsCommandHidden(string command)
    {
        if(command.Length == 0)
        {
            return false;
        }

        string[] cmd = ParseCommand(command);

        if(commands.TryGetValue(cmd[0], out CommandRegistration r))
        {
            return r.CommandType == CommandType.hidden;
        }
        return false;
    }

    #endregion

    #region command handlers

    private static void Quit_c(string[] args)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Console.LogWarning("pepehands");
        Application.Quit();
#endif
    }

    private static void Help_c(string[] args)
    {
        Console.LogInfo("Command list:");

        foreach(var c in commands)
        {
            if(c.Value.CommandType != CommandType.hidden)
            {
                Console.Log(c.Key + " - " + c.Value.Help);
            }
        }
    }

    private static void Bind_c(string[] args)
    {
        if(args.Length == 0)
        {
            string[] bindlist = BindManager.GetAllBindPairs();
            Console.LogInfo("Current bindings:");
            foreach(string s in bindlist)
            {
                Console.Log(s);
            }
        }
        else if(args.Length == 1)
        {
            if(BindManager.TryGetBindCommand(args[0], out string c))
            {
                Console.Log("Key " + args[0] + " is " + c);
            }
        }
        else if(args.Length >= 2)
        {
            string bindArg = string.Join(" ", args, 1, args.Length - 1);
            if(BindManager.SetBind(args[0], bindArg, bindArg.StartsWith("+") ? BindType.hold : BindType.toggle, false))
            {
                Console.LogInfo(args[0] + " -> " + bindArg);
            }
        }
        else
        {
            LogBadArgcount("Cvar", args.Length);
        }
    }

    private static void Unbind_c(string[] args)
    {
        if (args.Length == 0)
        {
            string[] bindlist = BindManager.GetAllBindPairs();
            Console.LogInfo("Current bindings:");
            foreach (string s in bindlist)
            {
                Console.Log(s);
            }
        }
        else if(args.Length == 1)
        {
            if (BindManager.RemoveBind(args[0]))
            {
                Console.LogInfo("Removed bind from key " + args[0]);
            }
        }
        else
        {
            LogBadArgcount("Unbind", args.Length);
        }
    }

    private static void Cvar_c(string[] args)
    {
        if(args.Length == 0)
        {
            ConfVar[] cvars = Cvar.GetCvars(CvarType.DEFAULT);
            string[] cvarlist = Cvar.GetCvars(CvarType.DEFAULT).Select(i => i.Name).ToArray();
            string[] valuelist = cvars.Select(i => i.Text).ToArray();
            int maxLength = cvarlist.OrderByDescending(i => i.Length).First().Length + 1;

            Console.LogInfo("Cvars:");
            for(int i = 0; i < cvarlist.Length; i++)
            {
                Console.Log(cvarlist[i] + new string(' ', maxLength - cvarlist[i].Length) + valuelist[i]);
            }
            return;
        }
        else if(args.Length == 1)
        {
            if(Cvar.WeakGet(args[0], out ConfVar cvar) && cvar.Type == CvarType.DEFAULT)
            {
                Console.Log(cvar.Name + " = " + cvar.Text);
                return;
            }
        }
        else if(args.Length == 2)
        {
            if (Cvar.WeakGet(args[0], out ConfVar cvar) && cvar.Type == CvarType.DEFAULT)
            {
                Cvar.Set(cvar.Name, args[1]);
                Console.LogInfo(args[0] + " -> " + args[1]);
                return;
            }
        }
        else
        {
            LogBadArgcount("Cvar", args.Length);
            return;
        }
        Console.LogWarning("Cvar \"" + args[0] + "\" doesn't exist");
    }

#endregion

    public static void LogBadArgcount(string caller, int count)
    {
        Console.LogWarning(caller + ": bad argument count (" + count + ")");
    }
}
