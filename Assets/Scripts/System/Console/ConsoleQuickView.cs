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
using TMPro;

/*
 * A script for managing an additional console output preview
 * at the top of the game screen.
 */

public class ConsoleQuickView : MonoBehaviour
{
    public TMP_Text field;

    private Queue<string> lines = new Queue<string>();

    private float currentLineTimer = 0f;

    ConfVar quickviewSize;
    ConfVar quickviewTime;

    private void Awake()
    {
        quickviewSize = Cvar.Get("quickview_size", "5");
        quickviewTime = Cvar.Get("quickview_time", "4");
    }

    private void Update()
    {
        currentLineTimer += Time.deltaTime;

        if(currentLineTimer > quickviewTime.Value)
        {
            DequeueOldestLine();
            currentLineTimer = 0f;
        }
    }

    public void ToggleField(bool active)
    {
        gameObject.SetActive(active);
        if (!active)
        {
            lines.Clear();
        }
        RefreshFieldText();
    }

    public void AddNewLine(string line)
    {
        lines.Enqueue(line);
        if(lines.Count > quickviewSize.Integer)
        {
            lines.Dequeue();
        }
        RefreshFieldText();
        if(lines.Count  == 1)
        {
            currentLineTimer = 0f;
        }
    }

    private void DequeueOldestLine()
    {
        if (lines.Count > 0)
        {
            lines.Dequeue();
            RefreshFieldText();
        }
    }

    private void RefreshFieldText()
    {
        string[] text = lines.ToArray();
        field.text = string.Join("\n", text);
    }
}
