﻿/*
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

using UnityEngine.UI;

public class SettingItemPanelBoolean : SettingItemPanel
{
    public Toggle input;

    public override void SetLabel(string value)
    {
        base.SetLabel(value);
        input.onValueChanged.AddListener(OnToggleStateChanged);
    }

    public override void SetValue(string cmd)
    {
        base.SetValue(cmd);

        input.isOn = cmd != "0";
    }

    private void OnToggleStateChanged(bool a)
    {
        Apply();
    }

    public override void Apply()
    {
        string ison = input.isOn ? "1" : "0";

        if(Cvar.Text(command) != ison)
        {
            ConfVar var = Cvar.Get(command, ison);
        }
    }
}
