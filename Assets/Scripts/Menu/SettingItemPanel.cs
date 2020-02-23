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
using TMPro;

public class SettingItemPanel : MonoBehaviour
{
    public TMP_Text label;
    public MenuManager menuManager;
    public SettingItem item;

    protected string Label
    {
        get
        {
            return label.text;
        }
        set
        {
            label.text = value;
        }
    }

    protected string command;

    public virtual void SetValue(string cmd)
    {
        
    }

    protected void CallRefresh()
    {
        menuManager.UpdateSettingsValues();
    }

    public virtual void SetInputClick()
    {

    }

    public virtual void GetKey(KeyCode keyCode)
    {

    }

    public virtual void SetClamp(Vector2 clamp)
    {

    }

    public void SetCommand(string cmd)
    {
        command = cmd;
    }

    public virtual void SetLabel(string value)
    {
        Label = value;
    }

    public virtual void Apply()
    {
        
    }
}
