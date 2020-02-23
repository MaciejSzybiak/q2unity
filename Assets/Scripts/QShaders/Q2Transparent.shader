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
* Simple transparent shader for Q2 transparent surfaces.
*/

Shader "Q2/Q2Transparent"
{
	Properties
	{
		_Color("Main Color (A=Opacity)", Color) = (1,1,1,1)
		_MainTex("Base (A=Opacity)", 2D) = ""
	}

	Category
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True"}
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader 
		{
			Pass 
			{
				GLSLPROGRAM
				varying mediump vec2 uv;

				#ifdef VERTEX
				uniform mediump vec4 _MainTex_ST;
				void main() 
				{
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
					uv = gl_MultiTexCoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				}
				#endif

				#ifdef FRAGMENT
				uniform lowp sampler2D _MainTex;
				uniform lowp vec4 _Color;

				void main() 
				{
					gl_FragColor = texture2D(_MainTex, SineWave(uv)) * _Color * 3;
				}
				#endif     
				ENDGLSL
			}
		}

		SubShader 
		{
			Pass 
			{
				SetTexture[_MainTex] {Combine texture * constant ConstantColor[_Color]}
			}
		}
	}

}