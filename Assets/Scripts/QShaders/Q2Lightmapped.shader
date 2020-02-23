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

/*
* Surface shader that combines Q2 lightmaps and textures.
* Takes two textures and two sets of UVs as an input.
*/

Shader "Q2/Q2Lightmapped"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LightmapTex ("Lightmap", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv_MainTex : TEXCOORD0;
                float2 uv2_LightmapTex : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
			sampler2D _LightmapTex;
            float4 _MainTex_ST;
			float _lightmapMix;
			float _lightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv_MainTex = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv2_LightmapTex = TRANSFORM_TEX(v.uv2, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the color and lightmap textures
                fixed4 col = tex2D(_MainTex, i.uv_MainTex) * lerp(fixed4(1, 1, 1, 1), tex2D(_LightmapTex, i.uv2_LightmapTex) * _lightness, _lightmapMix);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
