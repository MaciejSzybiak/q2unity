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
using UnityEngine.Rendering.PostProcessing;

/*
 * Manages graphics settings.
 */

public class GraphicsManager : MonoBehaviour
{
    public Camera mainCamera;
    public Camera skyboxCamera;

    public PostProcessLayer postLayer;
    public PostProcessVolume postVolume;

    private ConfVar afps;
    private ConfVar maxfps;

    #region singleton

    private static GraphicsManager Instance;

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

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        OnCvarFovSet(Cvar.Get("fov", "100", OnCvarFovSet, CvarType.DEFAULT));

        maxfps = Cvar.Get("maxfps", "120", OnCvarFpsSet, CvarType.DEFAULT);
        OnCvarFpsSet(maxfps);

        afps = Cvar.Get("afps", "500", OnCvarAfpsSet, CvarType.DEFAULT);
        OnCvarAfpsSet(afps);

        Globals.async = Cvar.Get("async", "1", OnCvarAsyncSet, CvarType.DEFAULT);
        OnCvarAsyncSet(Globals.async);

        Cvar.Add("vid_antialiasing", "0", OnCvarAntialiastingSet, CvarType.DEFAULT);
        Cvar.Add("vid_brightness", "0", OnCvarVidBrightnessSet, CvarType.DEFAULT);
        Cvar.Add("vid_gamma", "0", OnCvarVidGammaSet, CvarType.DEFAULT);
        Cvar.Add("vid_gain", "0", OnCvarVidGainSet, CvarType.DEFAULT);

        OnCvarLightmapSet(Cvar.Get("lightmap_mix", "1", OnCvarLightmapSet, CvarType.DEFAULT));
        OnCvarLightnessSet(Cvar.Get("lightsomeness", "15", OnCvarLightnessSet, CvarType.DEFAULT));
    }

    private void OnCvarAsyncSet(ConfVar c)
    {
        OnCvarFpsSet(maxfps);
        OnCvarAfpsSet(afps);
    }

    private void OnCvarAfpsSet(ConfVar v)
    {
        v.NoEventSet(Mathf.Clamp(v.Integer, 20, 1000));
        if (Globals.async != null && Globals.async.Boolean)
        {
            Application.targetFrameRate = v.Integer;
        }
    }

    private void OnCvarFpsSet(ConfVar v)
    {
        v.NoEventSet(Mathf.Clamp(v.Integer, 20, 200));
        if (Globals.async != null && Globals.async.Boolean)
        {
            Time.fixedDeltaTime = 1f / v.Integer;
        }
        else
        {
            Application.targetFrameRate = v.Integer;
        }
    }

    private void OnCvarLightmapSet(ConfVar c)
    {
        c.NoEventSet(Mathf.Clamp01(c.Value));
        Shader.SetGlobalFloat("_lightmapMix", c.Value);
    }

    private void OnCvarLightnessSet(ConfVar c)
    {
        c.NoEventSet(Mathf.Clamp(c.Value, 1, 25));
        Shader.SetGlobalFloat("_lightness", c.Value);
    }

    private void OnCvarVidBrightnessSet(ConfVar c)
    {
        if (postVolume.profile.TryGetSettings(out ColorGrading grading))
        {
            Vector4 newVal = grading.lift.value;
            newVal.w = c.Value;
            grading.lift.value = newVal;
        }
    }

    private void OnCvarVidGammaSet(ConfVar c)
    {
        if(postVolume.profile.TryGetSettings(out ColorGrading grading))
        {
            Vector4 newVal = grading.gamma.value;
            newVal.w = c.Value;
            grading.gamma.value = newVal;
        }
    }

    private void OnCvarVidGainSet(ConfVar c)
    {
        if (postVolume.profile.TryGetSettings(out ColorGrading grading))
        {
            Vector4 newVal = grading.gain.value;
            newVal.w = c.Value;
            grading.gain.value = newVal;
        }
    }

    private void OnCvarFovSet(ConfVar c)
    {
        c.NoEventSet(Mathf.Clamp(c.Value, 20, 150));

        mainCamera.fieldOfView = c.Value;

        skyboxCamera.fieldOfView = c.Value;
    }

    private void OnCvarAntialiastingSet(ConfVar c)
    {
        switch (c.Integer)
        {
            case 1:
                postLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                break;
            case 2:
                postLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                break;
            case 3:
                postLayer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
                break;
            default:
                postLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                break;
        }
    }
}
