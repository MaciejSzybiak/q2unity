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

using System;
using UnityEngine;
using TMPro;

public class RunTimer : MonoBehaviour
{
    public GameObject timePanel;
    public TMP_Text timeText;

    private float runTime = 0;
    //private float splitTime = 0;

    private bool isRun = false;
    private bool isRunEnd = false;

    private Color defaultCol = Color.white;
    private Color endCol = Color.green;

    public static RunTimer Instance
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (Instance)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        //not counting
        if (!isRun)
        {
            if (!isRunEnd && CheckRunStart())
            {
                StartRun();
            }
            return;
        }

        //mode changed
        if(PlayerState.mode != GameMode.timeattack)
        {
            ResetRun();
        }

        runTime += Time.unscaledDeltaTime;
        SetTimePanel();
    }

    public void SetPanelActive(bool isActive)
    {
        timePanel.SetActive(isActive);
        ResetRun();
    }

    private string FormatTimeString()
    {
        TimeSpan ts = TimeSpan.FromSeconds(runTime);
        string tstring = "";
        if(ts.Minutes > 0)
        {
            if(ts.Hours > 0)
            {
                tstring += string.Format("{0:D2}:", ts.Hours);
            }
            tstring += string.Format("{0:D2}:", ts.Minutes);
        }
        tstring += string.Format("{0:D2}", ts.Seconds);

        if(ts.Hours == 0)
        {
            tstring += string.Format(".{0:D3}", ts.Milliseconds);
        }

        return tstring;
    }

    private void SetTimePanel()
    {
        TimeSpan ts = TimeSpan.FromSeconds(runTime);
        timeText.text = FormatTimeString();
    }

    //slow and bad. use events instead?
    private bool CheckRunStart()
    {
        if(PlayerState.mode == GameMode.timeattack)
        {
            if(InputContainer.KeyFwd || InputContainer.KeyJump || InputContainer.KeyLeft 
                || InputContainer.KeyRight || InputContainer.KeyBack || InputContainer.KeyDuck)
            {
                return true;
            }
        }
        return false;
    }

    private void StartRun()
    {
        isRun = true;
        isRunEnd = false;
        runTime = 0;
        //splitTime = 0;
        SetTimePanel();
        timeText.color = defaultCol;
        timePanel.SetActive(true);
    }

    public void ResetRun()
    {
        isRun = false;
        isRunEnd = false;
        runTime = 0;
        //splitTime = 0;
        timePanel.SetActive(false);
    }

    public void EndRun()
    {
        if (!isRun)
        {
            return;
        }
        isRun = false;
        isRunEnd = true;
        timeText.color = endCol;

        //make sure the correct time is displayed
        string s = FormatTimeString();
        timeText.text = s;
        Console.LogInfo("Map completed with time " + s);
    }
}
