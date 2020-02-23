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

[System.Flags]
public enum SettingItemType
{
    cvarText = 1 << 0,
    cvarBool = 1 << 1,
    cvarFloat = 1 << 2,
    cvarInt = 1 << 3,
    bindToggle = 1 << 4,
    bindHold = 1 << 5,
    cvarIntSlider = 1 << 6,
    cvarFloatSlider = 1 << 7,
    
    //don't use in scriptable object
    bindType = bindHold | bindToggle,
    //don't use in scriptable object
    cvarType = cvarText | cvarBool | cvarFloat | cvarInt | cvarIntSlider | cvarFloatSlider
}

[CreateAssetMenu(fileName = "SettingItem", menuName = "Q2/SettingItem")]
public class SettingItem : ScriptableObject
{
    public string label;
    public string command;
    public SettingItemType SettingCategory;

    public Vector2 clamp;
}
