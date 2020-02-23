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
 * Stores input states at the current frame.
 */

public static class InputContainer
{
    public static int forwardMove = 0;
    public static int sideMove = 0;

    public static int upMove = 0;

    public static bool KeyFwd
    {
        get;
        private set;
    }

    public static bool KeyBack
    {
        get;
        private set;
    }

    public static bool KeyLeft
    {
        get;
        private set;
    }

    public static bool KeyRight
    {
        get;
        private set;
    }

    public static bool KeyJump
    {
        get;
        private set;
    }

    public static bool KeyDuck
    {
        get;
        private set;
    }


    public static void ResetKeypressState()
    {
        KeyFwd = KeyBack = KeyLeft = KeyRight = KeyJump = KeyDuck = false;
    }

    public static void MoveLeft(string[] args)
    {
        KeyLeft = true;
        if(sideMove > 0 || args.Length > 0)
        {
            sideMove = 0;
        }
        else
        {
            sideMove = -1;
        }
    }

    public static void MoveRight(string[] args)
    {
        KeyRight = true;
        if (sideMove < 0 || args.Length > 0)
        {
            sideMove = 0;
        }
        else
        {
            sideMove = 1;
        }
    }

    public static void MoveForward(string[] args)
    {
        KeyFwd = true;
        if (forwardMove < 0 || args.Length > 0)
        {
            forwardMove = 0;
        }
        else
        {
            forwardMove = 1;
        }
    }

    public static void MoveBack(string[] args)
    {
        KeyBack = true;
        if (forwardMove > 0 || args.Length > 0)
        {
            forwardMove = 0;
        }
        else
        {
            forwardMove = -1;
        }
    }

    public static void MoveUp(string[] args)
    {
        KeyJump = true;
        if (upMove < 0 || args.Length > 0)
        {
            upMove = 0;
        }
        else
        {
            upMove = 1;
        }
    }

    public static void MoveDown(string[] args)
    {
        KeyDuck = true;
        if (upMove > 0 || args.Length > 0)
        {
            upMove = 0;
        }
        else
        {
            upMove = -1;
        }
    }
}
