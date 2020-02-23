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
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public AudioSource clickSound;

    public Transform mainParent;
    public Image background;
    public Button backButton;
    public Button applyButton;
    public GameObject pressKeyPanel;
    public GameObject keyPanelMask;
    public GameObject welcomePanel;

    public Button mainButtonPrefab;
    public Button mapButtonPrefab;
    public GameObject menuPanelPrefab;
    public SettingsGroupManager settingGroupPanelPrefab;
    public MapPanel mapPanelPrefab;

    public SettingItemPanel holdItemPrefab;
    public SettingItemPanel toggleItemPrefab;
    public SettingItemPanel textItemPrefab;
    public SettingItemPanel integerItemPrefab;
    public SettingItemPanel floatItemPrefab;
    public SettingItemPanel booleanItemPrefab;

    public SettingItemPanel integerItemSliderPrefab;
    public SettingItemPanel floatItemSliderPrefab;

    public List<SettingsGroup> settingsGroups;

    private GameObject mainPanel;
    private MapPanel mapPanel;
    private GameObject settingsPanel;
    private List<SettingsGroupManager> settingGroupPanels = new List<SettingsGroupManager>();

    //state
    private bool isSettingGroup = false;

    private bool listenForKey = false;
    private bool listenForKeyCancelled = false;
    private SettingItemPanel listenRequestant;

    #region singleton

    public static MenuManager Instance
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

        Globals.menuopen = Cvar.Get("menuopen", "1", null, CvarType.HIDDEN);
    }

    #endregion

    private void OnGUI()
    {
        if (!listenForKey)
        {
            return;
        }
        Event e = Event.current;
        if (e.isKey)
        {
            PressKeyGetKeyCode(e.keyCode);
        }
    }

    private void PressKeyGetKeyCode(KeyCode code)
    {
        listenForKey = false;
        pressKeyPanel.SetActive(false);
        keyPanelMask.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        if (code == KeyCode.Escape || code == KeyCode.BackQuote)
        {
            listenForKeyCancelled = true;
            return;
        }
        listenRequestant.GetKey(code);
    }

    public void WaitForKey(SettingItemPanel req)
    {
        pressKeyPanel.SetActive(true);
        keyPanelMask.SetActive(true);
        listenRequestant = req;
        listenForKey = true;
    }

    private bool started = false;
    private int listenWaited = 0;
    private void Update()
    {
        if (listenForKey)
        {
            if (listenWaited < 5)
            {
                listenWaited++;
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    PressKeyGetKeyCode(KeyCode.Mouse0);
                    listenWaited = 0;
                }
                else if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    PressKeyGetKeyCode(KeyCode.Mouse1);
                    listenWaited = 0;
                }
                else if (Input.GetKeyDown(KeyCode.Mouse2))
                {
                    PressKeyGetKeyCode(KeyCode.Mouse2);
                    listenWaited = 0;
                }
                else if (Input.GetKeyDown(KeyCode.Mouse3))
                {
                    PressKeyGetKeyCode(KeyCode.Mouse3);
                    listenWaited = 0;
                }
                else if (Input.GetKeyDown(KeyCode.Mouse4))
                {
                    PressKeyGetKeyCode(KeyCode.Mouse4);
                    listenWaited = 0;
                }
            }
        }

        if (started)
        {
            return;
        }
        LateStart();
        started = true;
    }

    private void LateStart()
    {
        //check for configs
        bool hasConfig = false;

        if (ConfigManager.DefaultConfigExists())
        {
            //load default config
            Console.LogInfo("Loading default configuration.");
            ConfigManager.LoadConfig(new string[] { });
            hasConfig = true;
        }

        //generate menus

        //generate main panel
        mainPanel = Instantiate(menuPanelPrefab, mainParent);
        Button playBtn = Instantiate(mainButtonPrefab, mainPanel.transform);
        playBtn.onClick.AddListener(PlayBtnClick);
        playBtn.GetComponentInChildren<TMP_Text>().text = "Play";

        Button settingsBtn = Instantiate(mainButtonPrefab, mainPanel.transform);
        settingsBtn.onClick.AddListener(SettingBtnClick);
        settingsBtn.GetComponentInChildren<TMP_Text>().text = "Settings";

        Button quitBtn = Instantiate(mainButtonPrefab, mainPanel.transform);
        quitBtn.onClick.AddListener(QuitBtnClick);
        quitBtn.GetComponentInChildren<TMP_Text>().text = "Quit";

        //generate settings panel
        settingsPanel = Instantiate(menuPanelPrefab, mainParent);

        //generate settings groups
        for(int i = 0; i < settingsGroups.Count; i++)
        {
            SettingsGroupManager mng = Instantiate(settingGroupPanelPrefab, mainParent);

            mng.Generate(settingsGroups[i], this);

            Button sb = Instantiate(mainButtonPrefab, settingsPanel.transform);
            sb.onClick.AddListener(() => SettingsGroupButtonClick(mng));
            sb.GetComponentInChildren<TMP_Text>().text = settingsGroups[i].label;

            settingGroupPanels.Add(mng);
        }

        //generate map panel
        GenerateMapPanel();

        //add button listeners
        applyButton.onClick.AddListener(ApplySettingsButtonClick);
        backButton.onClick.AddListener(BackButtonClick);

        DisableAllPanels(); //lazy
        mainPanel.SetActive(true);

        CommandManager.RegisterCommand("togglemenu", Togglemenu_c, "", CommandType.hidden);
        
        if(!hasConfig)
        {
            //run first startup thing
            welcomePanel.SetActive(true);
            welcomePanel.transform.SetAsLastSibling();
        }
    }

    private void PlaySound()
    {
        clickSound.Play();
    }

    public void Deactivate()
    {
        if (gameObject.activeSelf)
        {
            Togglemenu_c(null);
        }
    }

    public void Activate()
    {
        if (!gameObject.activeSelf)
        {
            Togglemenu_c(null);
        }
    }

    private void Togglemenu_c(string[] args)
    {
        if (listenForKeyCancelled || !Cvar.Boolean("maploaded"))
        {
            listenForKeyCancelled = false;
            return;
        }

        gameObject.SetActive(!gameObject.activeSelf);
        mainParent.gameObject.SetActive(gameObject.activeSelf);
        Globals.menuopen.Value = gameObject.activeSelf ? 1 : 0;

        if (gameObject.activeSelf)
        {
            if (!Cvar.Boolean("console_open"))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            DisableAllPanels(); //lazy
            mainPanel.SetActive(true);

            if (Cvar.Boolean("maploaded"))
            {
                background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
            }
            else
            {
                background.color = new Color(background.color.r, background.color.g, background.color.b, 1f);
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            listenForKey = false;
            pressKeyPanel.SetActive(false);
            keyPanelMask.SetActive(false);
        }
    }

    //allows regeneration when gamedir/modname is changed
    public void GenerateMapPanel()
    {
        if(mainPanel == null)
        {
            //called too early
            return;
        }
        if (mapPanel)
        {
            DestroyImmediate(mapPanel);
        }

        mapPanel = Instantiate(mapPanelPrefab, mainParent);

        string[] maps = ResourceLoader.GetFileNamesFromPrefix("maps/");
        
        foreach(string name in maps)
        {
            Button btn = Instantiate(mapButtonPrefab, mapPanel.viewport);
            btn.onClick.AddListener(() => MapBtnClick(name));
            btn.GetComponentInChildren<TMP_Text>().text = name;
        }

        DisableAllPanels();
        mainPanel.SetActive(true);

        //make sure keycode thing is always on top
        pressKeyPanel.transform.SetAsLastSibling();
        keyPanelMask.transform.SetAsLastSibling();

        Console.DebugLog("Populated map menu with " + maps.Length + " map entries");
    }

    public void UpdateSettingsValues()
    {
        foreach(SettingsGroupManager mng in settingGroupPanels)
        {
            mng.RefreshValues();
        }
    }

    private void DisableAllPanels()
    {
        mainPanel.SetActive(false);
        if (mapPanel.gameObject.activeSelf)
        {
            mapPanel.gameObject.SetActive(false);
        }
        settingsPanel.SetActive(false);

        foreach(SettingsGroupManager o in settingGroupPanels)
        {
            o.gameObject.SetActive(false);
        }

        isSettingGroup = false;

        applyButton.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
    }

    #region button clicks

    private void PlayBtnClick()
    {
        PlaySound();

        //go to map menu
        DisableAllPanels();

        mapPanel.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);
    }

    private void MapBtnClick(string mapname)
    {
        PlaySound();

        //load map
        CommandManager.RunCommand("map", new string[] { mapname });
    }

    private void QuitBtnClick()
    {
        PlaySound();

        CommandManager.RunCommand("quit", null);
    }

    private void SettingBtnClick()
    {
        PlaySound();

        //open settings panel
        DisableAllPanels();
        settingsPanel.SetActive(true);
        backButton.gameObject.SetActive(true);
        applyButton.gameObject.SetActive(true);
    }

    private void SettingsGroupButtonClick(SettingsGroupManager panel)
    {
        PlaySound();

        //open settings group panel
        DisableAllPanels();

        panel.Generate(panel.group, this); //HACK: keeps things fairly updated

        panel.gameObject.SetActive(true);
        applyButton.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);

        isSettingGroup = true;
    }

    private void ApplySettingsButtonClick()
    {
        PlaySound();

        Console.DebugLog("Applying binds...");
        foreach(SettingsGroupManager m in settingGroupPanels)
        {
            m.ApplySettings();
        }
        Console.DebugLog("Writing configuration file...");
        CommandManager.RunCommand("writeconf", null);

    }

    private void BackButtonClick()
    {
        PlaySound();

        bool settings = isSettingGroup;
        DisableAllPanels();

        if (settings)
        {
            settingsPanel.SetActive(true);
            backButton.gameObject.SetActive(true);
            applyButton.gameObject.SetActive(true);
        }
        else
        {
            mainPanel.SetActive(true);
        }
    }

    #endregion
}
