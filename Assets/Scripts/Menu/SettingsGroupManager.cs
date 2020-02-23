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
using UnityEngine;

public class SettingsGroupManager : MonoBehaviour
{
    public Transform viewport;

    public SettingsGroup group;
    private List<SettingItemPanel> items = new List<SettingItemPanel>();

    public void Generate(SettingsGroup group, MenuManager manager)
    {
        if(items.Count > 0)
        {
            for(int i = 0; i < items.Count; i++)
            {
                Destroy(items[i].gameObject);
            }
            items.Clear();
        }

        this.group = group;

        foreach(SettingItem si in group.items)
        {
            AddItem(si, manager);
        }
    }

    private void AddItem(SettingItem item, MenuManager manager)
    {
        SettingItemPanel panel;
        switch (item.SettingCategory)
        {
            case SettingItemType.bindHold:
                panel = Instantiate(manager.holdItemPrefab, viewport);
                break;
            case SettingItemType.bindToggle:
                panel = Instantiate(manager.toggleItemPrefab, viewport);
                break;
            case SettingItemType.cvarBool:
                panel = Instantiate(manager.booleanItemPrefab, viewport);
                break;
            case SettingItemType.cvarFloat:
                panel = Instantiate(manager.floatItemPrefab, viewport);
                break;
            case SettingItemType.cvarInt:
                panel = Instantiate(manager.integerItemPrefab, viewport);
                break;
            case SettingItemType.cvarText:
                panel = Instantiate(manager.textItemPrefab, viewport);
                break;
            case SettingItemType.cvarIntSlider:
                panel = Instantiate(manager.integerItemSliderPrefab, viewport);
                panel.SetClamp(item.clamp);
                break;
            case SettingItemType.cvarFloatSlider:
                panel = Instantiate(manager.floatItemSliderPrefab, viewport);
                panel.SetClamp(item.clamp);
                break;
            default:
                panel = Instantiate(manager.textItemPrefab, viewport);
                break;
        }

        //set stuff
        if((item.SettingCategory & SettingItemType.bindType) != 0)
        {
            if (BindManager.TryFindBindKey(item.command, out KeyCode k))
            {
                panel.SetValue(k.ToString());
            }
        }
        else
        {
            if (Cvar.WeakGet(item.command, out ConfVar cvar))
            {
                panel.SetValue(cvar.Text);
            }
        }

        panel.SetLabel(item.label);
        panel.SetCommand(item.command);
        panel.menuManager = manager;
        panel.SetInputClick();
        panel.item = item;

        items.Add(panel);
    }

    public void RefreshValues()
    {
        foreach(SettingItemPanel panel in items)
        {
            SettingItem item = panel.item;

            if ((item.SettingCategory & SettingItemType.bindType) != 0)
            {
                if (BindManager.TryFindBindKey(item.command, out KeyCode k))
                {
                    panel.SetValue(k.ToString());
                }
                else
                {
                    panel.SetValue("");
                }
            }
            else
            {
                if (Cvar.WeakGet(item.command, out ConfVar cvar))
                {
                    panel.SetValue(cvar.Text);
                }
            }
        }
    }

    public void ApplySettings()
    {
        foreach(SettingItemPanel item in items)
        {
            item.Apply();
        }
    }
}
