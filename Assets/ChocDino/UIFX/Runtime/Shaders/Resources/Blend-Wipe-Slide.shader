Shader "Hidden/ChocDino/UIFX/Blend-Wipe-Slide"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _ResultTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _SourceTex ("Source Texture", 2D) = "white" {}

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_ColorMask ("Color Mask", Float) = 15

		_Wipe("Wipe", Float) = 0.0
		_Softness("Softness", Float) = 0.0
		_InvertDirection("Invert Direction", Float) = 0.0
		_AxisMask("Axis Mask", Vector) = (0, 0, 0, 0)
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
	
	#include "BlendUtils.cginc"
	#include "Common/Wipe.cginc"

	uniform float3 _AxisMask; // XMask, YMask, XFlip

	float4 fragWipe(v2f i) : SV_Target
	{
		float2 axisLength = _ResultTex_TexelSize.zw;

		float2 uv = i.uv.xy;
		uv *= axisLength;

		// Calculate fading
		float fade = 1.0;
		{
			float fadeLeft = saturate((uv.x) / _Softness);
			float fadeRight = saturate((axisLength.x - uv.x) / _Softness);
			float fadeDown = saturate((uv.y) / _Softness);
			float fadeUp = saturate((axisLength.y - uv.y) / _Softness);

			float fadeX = min(fadeLeft, fadeRight);
			fadeX = lerp(1.0, fadeX, _AxisMask.x);

			float fadeY = min(fadeUp, fadeDown);
			fadeY = lerp(1.0, fadeY, _AxisMask.y);

			fade = min(fadeX, fadeY);

			// Allow more pleasing falloff.
			fade = pow(fade, 2.2);
		}

		float invertY = sign(_InvertDirection - 0.5);
		float invertX = lerp(invertY, -invertY, _AxisMask.z);
		uv.x += (axisLength.x - _Softness * 1.0) * _AxisMask.x * invertX * _Wipe.x;
		uv.y += (axisLength.y - _Softness * 1.0) * _AxisMask.y * invertY * _Wipe.x;

		uv /= axisLength;

		return tex2D(_SourceTex, uv) * fade;
	}

	ENDCG

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
			"OutputsPremultipliedAlpha"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend One OneMinusSrcAlpha // Premultiplied transparency
		ColorMask [_ColorMask]

		Pass
		{
			Name "Blend-Wipe-Slide"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWipe
			ENDCG
		}
	}
}