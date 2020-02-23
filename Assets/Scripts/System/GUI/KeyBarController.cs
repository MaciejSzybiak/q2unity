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
using UnityEngine.UI;
using TMPro;

/*
 * This script manages the key, framerate and speed display on the GUI.
 */

public class KeyBarController : MonoBehaviour
{
    public Image KeyForward;
    public Image KeyBack;
    public Image KeyLeft;
    public Image KeyRight;

    public Image KeyJump;
    public Image KeyDuck;

    public TMP_Text speedText;
    public TMP_Text fpsText;

    ConfVar speedUpdateFreq;
    ConfVar fpsUpdateFreq;

    private readonly GradientColorKey btmColor = new GradientColorKey(new Color(1, 1, 0), 0f);
    private readonly GradientColorKey lowColor = new GradientColorKey(Color.yellow, 0.10f);
    private readonly GradientColorKey medColor = new GradientColorKey(Color.green, 0.3f);
    private readonly GradientColorKey hiColor = new GradientColorKey(Color.cyan, 1f);
    private Gradient valueGradient;

    private Vector2 fpsClamp = new Vector2(20, 240);

    private float fpsTimer = 0f;
    private float speedTimer = 0f;

    private int frameCount = 0;

    public static KeyBarController Instance
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

        speedUpdateFreq = Cvar.Get("keybar_speed_freq", "10");
        fpsUpdateFreq = Cvar.Get("keybar_fps_freq", "2");

        valueGradient = new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                btmColor, lowColor, medColor, hiColor
            }
        };
    }

    private void FixedUpdate()
    {
        if (Globals.async.Boolean)
        {
            //time and update values
            fpsTimer += Time.fixedDeltaTime;
            frameCount++;

            if (fpsTimer >= 1 / fpsUpdateFreq.Value)
            {
                float fps = frameCount / fpsTimer;
                Color col = valueGradient.Evaluate((Mathf.Clamp(fps, fpsClamp.x, fpsClamp.y) - 20) / (fpsClamp.y - fpsClamp.x));

                fpsText.text = Mathf.RoundToInt(fps).ToString();
                fpsText.color = col;

                fpsTimer = 0;
                frameCount = 0;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!Globals.async.Boolean)
        {
            //time and update values
            fpsTimer += Time.deltaTime;
            frameCount++;

            if(fpsTimer >= 1 / fpsUpdateFreq.Value)
            {
                float fps = frameCount / fpsTimer;
                Color col = valueGradient.Evaluate((Mathf.Clamp(fps, fpsClamp.x, fpsClamp.y) - 20) / (fpsClamp.y - fpsClamp.x));

                fpsText.text = Mathf.RoundToInt(fps).ToString();
                fpsText.color = col;

                fpsTimer = 0;
                frameCount = 0;
            }
        }

        speedTimer += Time.deltaTime;

        if(speedTimer >= 1 / speedUpdateFreq.Value)
        {
            speedText.text = Mathf.RoundToInt(PlayerState.currentSpeed).ToString();

            speedTimer = 0;
        }

        //HACK: console open means no keys pressed
        bool cOpen = Console.Instance.gameObject.activeSelf;

        //check keys
        if (InputContainer.KeyFwd && !cOpen)
        {
            KeyForward.gameObject.SetActive(true);
        }
        else
        {
            KeyForward.gameObject.SetActive(false);
        }
        if (InputContainer.KeyBack && !cOpen)
        {
            KeyBack.gameObject.SetActive(true);
        }
        else
        {
            KeyBack.gameObject.SetActive(false);
        }
        if (InputContainer.KeyLeft && !cOpen)
        {
            KeyLeft.gameObject.SetActive(true);
        }
        else
        {
            KeyLeft.gameObject.SetActive(false);
        }
        if (InputContainer.KeyRight && !cOpen)
        {
            KeyRight.gameObject.SetActive(true);
        }
        else
        {
            KeyRight.gameObject.SetActive(false);
        }

        if (InputContainer.KeyJump && !cOpen)
        {
            KeyJump.gameObject.SetActive(true);
        }
        else
        {
            KeyJump.gameObject.SetActive(false);
        }
        if (InputContainer.KeyDuck && !cOpen)
        {
            KeyDuck.gameObject.SetActive(true);
        }
        else
        {
            KeyDuck.gameObject.SetActive(false);
        }
    }
}
