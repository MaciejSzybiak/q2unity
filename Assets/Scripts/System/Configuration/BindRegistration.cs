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

using UnityEngine;

/*
 * Stores a key binding.
 */

public enum BindType
{
    toggle,
    hold
}

public class BindRegistration
{
    public readonly bool locked = false;

    private string c;
    public string Command
    {
        get
        {
            return c;
        }
        set
        {
            if (!locked)
            {
                c = value;
            }
        }
    }

    private KeyCode k;
    public KeyCode Key
    {
        get
        {
            return k;
        }
        set
        {
            if (!locked)
            {
                k = value;
            }
        }
    }

    public string Keystring
    {
        get
        {
            return k.ToString();
        }
    }

    private BindType b;
    public BindType BindType
    {
        get
        {
            return b;
        }
        set
        {
            if (!locked)
            {
                b = value;
            }
        }
    }

    public bool Edit(KeyCode key, string command)
    {
        if (locked)
        {
            return false;
        }

        k = key;
        c = command;
        return true;
    }

    public BindRegistration(KeyCode key, string command, BindType type, bool locked)
    {
        Key = key;
        Command = command;
        BindType = type;
        this.locked = locked;
    }
}
