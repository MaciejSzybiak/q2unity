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
 * Main game state management.
 */

public enum GameState
{
    menu, loading, singleplayer, editor
}

public static class StateManager
{
    //private static GameState lastState = GameState.menu;

    public static void ChangeGameState(GameState state)
    {
        if(state == GameState.loading)
        {
            //begin loading

            Loader.Instance.ClearMap();
            MenuManager.Instance.Deactivate();
            LoadscreenManager.Instance.ActivateLoadScreen();

            SetGameHudActive(false);
        }
        else
        {
            if(state != GameState.menu)
            {
                if(state == GameState.editor)
                {
                    //editor
                    SetGameHudActive(false);
                }
                else
                {
                    //map
                    SetGameHudActive(true);
                }

                MenuManager.Instance.Deactivate();
            }
            else
            {
                //go to menu

                Loader.Instance.ClearMap();
                MenuManager.Instance.Activate();

                SetGameHudActive(false);
            }

            LoadscreenManager.Instance.DeactivateLoadScreen();

            if (Cvar.Boolean("console_open"))
            {
                CommandManager.RunCommand("toggleconsole", null);
            }
        }
    }

    private static void SetGameHudActive(bool isActive)
    {
        KeyBarController.Instance.gameObject.SetActive(isActive);
        RunTimer.Instance.SetPanelActive(true);
    }
}
