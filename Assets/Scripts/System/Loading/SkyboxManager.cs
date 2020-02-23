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

/*
 * Loads the required skybox files.
 */

public class SkyboxManager : MonoBehaviour
{
    public Material skymat;

    private readonly string[] suffixes =
    {
        "bk.tga", "ft.tga",
        "dn.tga", "up.tga",
        "lf.tga", "rt.tga"
    };

    private readonly Guid[] guids = new Guid[6];
    private readonly Texture2D[] texs = new Texture2D[6];

    private Material matInstance;

    public void LoadSkyboxForMap(string skyname)
    {
        int i;

        if(matInstance == null)
        {
            matInstance = Instantiate(skymat);
        }

        for(i = 0; i < 6; i++)
        {
            guids[i] = TextureLoader.LoadTexture("env/" + skyname + suffixes[i]);
            if(guids[i] == Guid.Empty)
            {
                Console.DebugLog("Sky " + skyname + suffixes[i] + " not found!");
                LoadEmptySkybox();
                return;
            }
        }

        //set skybox in a separate loop, after validating skybox images
        for (i = 0; i < 6; i++)
        {
            texs[i] = TextureLoader.GetTexture(guids[i]);
            if (!texs[i])
            {
                Console.DebugLog("What? Couldn't find loaded texture file...");
                return;
            }
        }

        matInstance.SetTexture("_DownTex", texs[2]);
        matInstance.SetTexture("_UpTex", texs[3]);
        matInstance.SetTexture("_BackTex", texs[4]);
        matInstance.SetTexture("_FrontTex", texs[5]);
        matInstance.SetTexture("_LeftTex", texs[1]);
        matInstance.SetTexture("_RightTex", texs[0]);

        RenderSettings.skybox = matInstance;

        Console.DebugLog("Loaded skybox \"" + skyname + "\".");
    }

    private void LoadEmptySkybox()
    {
        Texture2D tex = new Texture2D(8, 8, TextureFormat.R8, false);

        tex.LoadRawTextureData(TextureLoader.emptyTex);
        tex.filterMode = FilterMode.Trilinear;
        tex.Apply();

        matInstance.SetTexture("_FrontTex", tex);
        matInstance.SetTexture("_BackTex", tex);
        matInstance.SetTexture("_LeftTex", tex);
        matInstance.SetTexture("_RightTex", tex);
        matInstance.SetTexture("_DownTex", tex);
        matInstance.SetTexture("_UpTex", tex);

        RenderSettings.skybox = matInstance;
    }

    //this doesn't make the actual skybox disappear - it clears the data that was referenced before
    public void UnloadSkyboxData()
    {
        for(int i = 0; i < 6; i++)
        {
            texs[i] = null;
            TextureLoader.UnloadTexture(guids[i]);
        }
    }
}
