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

using System.Collections;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    ReplayLoader replayLoader;
    ReplayFile replay;

    public Loader loader;

    public Camera cam;

    private long framecount;
    private int currentframe;
    private float currenttime;
    private float frametime = 0.1f; //10fps

    #region singleton

    public static ReplayManager Instance
    {
        get;
        private set;
    }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    #endregion

    private void Start()
    {
        CommandManager.RegisterCommand("replay", Replay_c, "plays a replay (if available)", CommandType.normal);
        BindManager.SetBind(KeyCode.F3, "replay", BindType.toggle, false);
    }

    private void Replay_c(string[] args)
    {
        if (replay != null && cam)
        {
            StopAllCoroutines();
            StartCoroutine(ReplayPlayer());
        }
        else
        {
            Console.DebugLog("No replay for map " + Globals.map.Text);
        }
    }

    public void LoadReplay()
    {
        replayLoader = new ReplayLoader();

        string replayName = loader.gamepath + "/" + Globals.modname.Text + "/jumpdemo/" + Globals.map.Text + "_388.dj3";
        replay = replayLoader.LoadReplay(replayName, Globals.scale.Value);

        if (replay == null)
        {
            Console.DebugLog("Goblin replay not found! " + replayName);
        }
    }

    private IEnumerator ReplayPlayer()
    {
        float fraction;
        framecount = replay.replayFrames.Length;
        currenttime = 0;
        currentframe = 0;
        Vector3 lastangle = replay.replayFrames[0].angle;
        Quaternion lastrot = Quaternion.Euler(lastangle);

        while(currentframe < framecount - 2)
        {
            currentframe = Mathf.FloorToInt(currenttime / frametime);
            fraction = (currenttime % frametime) / frametime;

            cam.transform.position = Vector3.Lerp(replay.replayFrames[currentframe].origin, replay.replayFrames[currentframe + 1].origin, fraction) + Vector3.up * 0.32f;

            Vector3 diff = replay.replayFrames[currentframe + 1].angle - replay.replayFrames[currentframe].angle;

            for (int i = 0; i < 3; i++)
            {
                if (diff[i] > 180)
                {
                    diff[i] = -360f + diff[i];
                }
                if (diff[i] < -180)
                {
                    diff[i] = 360f + diff[i];
                }
                diff[i] *= fraction;
            }

            lastangle = replay.replayFrames[currentframe].angle + diff;
            lastrot = Quaternion.Euler(lastangle);
            Vector3 rot = cam.transform.position + lastangle;
            cam.transform.rotation = lastrot;

            currenttime += Time.deltaTime;

            yield return null;
        }
    }
}
