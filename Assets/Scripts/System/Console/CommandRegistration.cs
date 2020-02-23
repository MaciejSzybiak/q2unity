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
 * Command registration represents a command.
 */

public enum CommandType
{
    normal,
    hidden
}

public class CommandRegistration
{
    public string Command
    {
        get;
        private set;
    }

    public CommandHandler Handler
    {
        get;
        private set;
    }

    public string Help
    {
        get;
        private set;
    }

    public CommandType CommandType
    {
        get;
        private set;
    }

    public CommandRegistration(string command, CommandHandler handler, string help, CommandType type)
    {
        Command = command;
        Handler = handler;
        Help = help;
        CommandType = type;
    }
}
