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

using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class SettingItemPanelSlider : SettingItemPanelFloat
{
    public Slider slider;
    private bool setListenter = false;

    public override void SetClamp(Vector2 clamp)
    {
        base.SetClamp(clamp);
        slider.minValue = clamp.x;
        slider.maxValue = clamp.y;

        input.readOnly = true;
    }

    public override void SetValue(string cmd)
    {
        base.SetValue(cmd);
        slider.value = float.Parse(cmd.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat);

        if (!setListenter)
        {
            slider.onValueChanged.AddListener(OnSliderChange);
            setListenter = true;
        }
    }

    private void OnSliderChange(float value)
    {
        base.SetValue(value.ToString());

        if(Cvar.Text(command) != input.text)
        {
            ConfVar var = Cvar.Get(command, input.text);
        }
    }
}
