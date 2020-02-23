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
using System;
using System.Linq;
using UnityEngine;
using TMPro;

/*
 * Script for managing the in-game console.
 */

public class Console : MonoBehaviour
{
    public GameObject ConsolePanel;
    public TMP_Text LogField;
    public TMP_InputField InputField;
    public ConsoleQuickView QuickView;

    private static ConfVar historyCapacity;
    private static ConfVar scrollbackCapacity;
    private static ConfVar scrollSpeed;
    private static ConfVar consoleOpen;

#if DEBUG
    private static ConfVar logDebugMessages;
#endif

    private List<string> consoleHistory;
    private Queue<string> scrollback;

    private int historyIndex;
    private int CurrentHistoryIndex
    {
        get
        {
            return historyIndex;
        }
        set
        {
            historyIndex = Mathf.Clamp(value, 0, consoleHistory.Count);
        }
    }

    private int scrollPosition = 0;
    private int CurrentScrollPosition
    {
        get
        {
            return scrollPosition;
        }
        set
        {
            scrollPosition = Mathf.Clamp(value, 0, scrollback.Count);
        }
    }
    
    #region singleton

    public static Console Instance
    {
        get;
        private set;
    }

    private void SetSingleton()
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

    #endregion

    private void Awake()
    {
        SetSingleton();

        historyCapacity = Cvar.Get("con_history", "50");
        scrollbackCapacity = Cvar.Get("con_scrollback", "200");
        scrollSpeed = Cvar.Get("con_scrollspeed", "3");
        consoleOpen = Cvar.Get("console_open", ConsolePanel.activeSelf ? "1" : "0", null, CvarType.HIDDEN);

#if DEBUG
        logDebugMessages = Cvar.Get("log_debug", "1");
#endif

        consoleHistory = new List<string>(historyCapacity.Integer);
        scrollback = new Queue<string>(scrollbackCapacity.Integer);
    }

    private void Start()
    {
        CommandManager.RegisterCommand("toggleconsole", ToggleConsole, "", CommandType.hidden);
        CommandManager.RegisterCommand("clear", ClearConsole, "clears the console log", CommandType.normal);

        //BindManager.SetBind(KeyCode.BackQuote, "toggleconsole", BindType.toggle, true);

        ToggleConsole(null);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Autocomplete();
        }

        float scrollAmount = Input.GetAxis("Mouse ScrollWheel");
        if(scrollAmount < 0)
        {
            ScrollDown();
        }
        else if(scrollAmount > 0)
        {
            ScrollUp();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            RecallLastCommand();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            RecallNextCommand();
        }

        if(Input.GetKeyDown(KeyCode.Return) && InputField.text != "")
        {
            RunCommandFromField();
        }

        //hack to make input field focused
        InputField.Select();
        InputField.ActivateInputField();
    }

    private void ConsoleSet(bool active)
    {
        ConsolePanel.SetActive(active);
        consoleOpen.Integer = active ? 1 : 0;
        if (active)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InputField.text = "";
            ResetScrollPosition();
        }
        else if(Cvar.Boolean("menuopen"))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        //quickview
        QuickView.ToggleField(!active);
    }

    public void ToggleConsole(string[] args)
    {
        ConsoleSet(!ConsolePanel.activeSelf);
    }

    public void ClearConsole(string[] args)
    {
        scrollback = new Queue<string>();
        RefreshLogText();
    }

    private void RunCommandFromField()
    {
        LogCommand(InputField.text);

        if (!CommandManager.IsCommandHidden(InputField.text))
        {
            CommandManager.RunCommandString(InputField.text);
        }
        else
        {
            CommandManager.LogInvalidCommand(InputField.text);
        }

        consoleHistory.Add(InputField.text);
        ResetHistoryIndex();
        ResetScrollPosition();
        UpdateInputFieldFromHistory();
    }

    private void RefreshLogText()
    {
        string[] log = scrollback.ToArray();
        if(log == null || log.Length == 0)
        {
            LogField.text = "";
            return;
        }

        string text = string.Join("\n", log, 0, log.Length - CurrentScrollPosition);

        if(CurrentScrollPosition != 0)
        {
            text += "\n  <color=red>- - -</color>";
        }
        LogField.text = text;
    }

    #region scrolling

    private void ScrollUp()
    {
        CurrentScrollPosition += scrollSpeed.Integer;
        RefreshLogText();
    }

    private void ScrollDown()
    {
        CurrentScrollPosition -= scrollSpeed.Integer;
        RefreshLogText();
    }

    private void ResetScrollPosition()
    {
        CurrentScrollPosition = 0;
        RefreshLogText();
    }

    #endregion

    #region command history

    private void RecallLastCommand()
    {
        CurrentHistoryIndex--;
        UpdateInputFieldFromHistory();
    }

    private void RecallNextCommand()
    {
        CurrentHistoryIndex++;
        UpdateInputFieldFromHistory();
    }

    private void ResetHistoryIndex()
    {
        CurrentHistoryIndex = consoleHistory.Count;
    }

    private void UpdateInputFieldFromHistory()
    {
        if(CurrentHistoryIndex == consoleHistory.Count)
        {
            InputField.text = "";
        }
        else
        {
            InputField.text = consoleHistory[CurrentHistoryIndex];
        }
        InputField.caretPosition = InputField.text.Length;
    }

    #endregion

    #region autcomplete

    private void Autocomplete()
    {
        if(InputField.text == "")
        {
            return;
        }
        if(InputField.text.StartsWith("cvar "))
        {
            AutocompleteCvar();
            return;
        }
        else if(InputField.text.StartsWith("bind "))
        {
            AutocompleteBind();
            return;
        }
        else if(InputField.text.StartsWith("map "))
        {
            AutocompleteMap();
        }

        string[] completions = CommandManager.GetCommandAutocompletions(InputField.text);

        //try to autocomplete
        if(completions.Length == 1)
        {
            InputField.text = completions[0] + " ";
            InputField.caretPosition = InputField.text.Length;
        }
        else if(completions.Length > 0)
        {
            ListCompletions(completions, "Command", completions[0]);
        }
        ResetScrollPosition();
    }

    private void AutocompleteCvar()
    {
        string[] split = InputField.text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if(split.Length != 2)
        {
            return;
        }

        string[] completions = Cvar.GetCvarCompletions(split[1]).Select(i => i.Name).ToArray();

        if(completions.Length == 1)
        {
            InputField.text = split[0] + " " + completions[0] + " ";
            InputField.caretPosition = InputField.text.Length;
        }
        else if(completions.Length > 0)
        {
            ListCompletions(completions, "Cvar", split[0]);
        }
        ResetScrollPosition();
    }

    private void AutocompleteBind()
    {
        string[] split = InputField.text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if(split.Length == 1)
        {
            string[] completions = BindManager.GetBindKeyAutocompletions("");

            ListCompletions(completions.Where(i => !i.StartsWith("Joystick")).ToArray(), "Bind key", split[0]);
        }
        if (split.Length == 2)
        {
            string[] completions = BindManager.GetBindKeyAutocompletions(split[1]);

            if (completions.Length == 1)
            {
                InputField.text = split[0] + " " + completions[0] + " ";
                InputField.caretPosition = InputField.text.Length;
            }
            else if (completions.Length > 0)
            {
                ListCompletions(completions, "Bind key", split[0]);
            }
            ResetScrollPosition();
        }
        else if(split.Length == 3)
        {
            string[] completions = CommandManager.GetCommandAutocompletions(split[2]);

            if (completions.Length == 1)
            {
                InputField.text = split[0] + " " + split[1] + " " + completions[0] + " ";
                InputField.caretPosition = InputField.text.Length;
            }
            else if (completions.Length > 0)
            {
                ListCompletions(completions, "Bind command", split[0] + " " + split[1]);
            }
            ResetScrollPosition();
        }
    }

    private void AutocompleteMap()
    {
        string[] split = InputField.text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if(split.Length > 2)
        {
            return;
        }

        string[] completions = ResourceLoader.GetFileNamesFromPrefix("maps/" + (split.Length == 2 ? split[1] : ""));

        if (completions.Length == 1)
        {
            InputField.text = split[0] + " " + completions[0];
            InputField.caretPosition = InputField.text.Length;
        }
        else if (completions.Length > 0)
        {
            ListCompletions(completions, "Map name", split[0]);
        }
        ResetScrollPosition();
    }

    private void ListCompletions(string[] completions, string msg, string prefix)
    {
        int maxLength = completions.OrderByDescending(item => item.Length).First().Length + 1;
        int fullLines = completions.Length / 3;
        bool addline = completions.Length % 3 > 0;
        string[] lines = new string[fullLines + (addline ? 1 : 0)];

        for(int i = 0; i < fullLines; i++)
        {
            int m = i * 3;

            lines[i] = completions[m] + new string(' ', maxLength - completions[m].Length);
            lines[i] += completions[m + 1] + new string(' ', maxLength - completions[m + 1].Length);
            lines[i] += completions[m + 2];
        }
        if (addline)
        {
            int count = completions.Length - fullLines * 3;
            for(int i = count; i > 0; i--)
            {
                lines[lines.Length - 1] += completions[completions.Length - i] + new string(' ', maxLength - completions[completions.Length - i].Length);
            }
        }

        LogInfo(msg + " completions: ");
        foreach(string s in lines)
        {
            Log(s);
        }
        InputField.text = prefix + " " + CommonPrefix(completions);
        InputField.caretPosition = InputField.text.Length;
    }

    private string CommonPrefix(string[] array)
    {
        return array.Aggregate(PairPrefix);
    }

    private string PairPrefix(string s1, string s2)
    {
        int i;
        int count = Mathf.Min(s1.Length, s2.Length);

        for(i = 0; i < count; i++)
        {
            if(s1[i] != s2[i])
            {
                break;
            }
        }

        return s1.Substring(0, i);
    }

    #endregion

    #region log messages

    private static void LogCommand(string message)
    {
        AppendLineToScrollback("<color=purple>>" + message + "</color>");
    }

    public static void LogInfo(string message)
    {
        AppendLineToScrollback("<color=#00ffe9>" + message + "</color>");
    }

    public static void LogError(string message)
    {
        Log("<color=red>" + message + "</color>");
#if DEBUG
        string stack = Environment.StackTrace;
        Log("<color=red>" + stack + "</color>");
#endif
    }

    public static void LogWarning(string message)
    {
        Log("<color=yellow>" + message + "</color>");
    }

    public static void LogIOException(string caller, System.IO.IOException e)
    {
        LogError(caller + ": " + e.Message);
    }

    public static void DebugLog(string message)
    {
#if DEBUG
        if (logDebugMessages != null)
        {
            if (logDebugMessages.Boolean)
            {
                Log("<color=orange>-->DEBUG:</color> " + message);
            }
        }
        else
        {
            Log("<color=orange>-->DEBUG:</color> " + message);
        }
#endif
    }

    public static void Log(string message)
    {
        string[] messages = message.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach(string msg in messages)
        {
            AppendLineToScrollback(msg);
        }
        Instance.RefreshLogText();
    }

    public static void AddSeparator(int length)
    {
        string s = new string('-', length);
        AppendLineToScrollback(s);
        Instance.RefreshLogText();
    }
    
    private static void AppendLineToScrollback(string line)
    {
        //support multiple lines in a message
        string[] lines = line.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if(Instance.scrollback.Count + lines.Length > scrollbackCapacity.Integer)
        {
            for(int i = 0; i < lines.Length; i++)
            {
                Instance.scrollback.Dequeue();
            }
        }
        for(int i = 0; i < lines.Length; i++)
        {
            Instance.scrollback.Enqueue(lines[i]);
            Instance.QuickView.AddNewLine(lines[i]);
        }
    }

    #endregion
}
