Shader "Hidden/ChocDino/UIFX/Blend-Wipe-Radial"
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
		_Spokes("Spokes", Float) = 1.0
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
	#pragma multi_compile_local_fragment SHAPE_RADIAL SHAPE_RADIAL_SPOKES

	#include "BlendUtils.cginc"
	#include "Common/Wipe.cginc"

	uniform int _Spokes;

	float4 fragWipe(v2f i) : SV_Target
	{
		float mask = 0.5;
		float maskRange = 360.0;

		//float2 aspect = float2(_ResultTex_TexelSize.w / _ResultTex_TexelSize.z, 1.0);
		float2 aspect = float2(_ResultTex_TexelSize.y / _ResultTex_TexelSize.x, 1.0);

		float2 uv = i.uv;
		uv = ((i.uv - 0.5) * aspect);

		float PI = 3.141592654;
		float TWOPI = 3.141592654 * 2.0;

		float angleOffset = 0.0;//_Time.x * 10.0;

		#if SHAPE_RADIAL
		{
			//float2 uv = ((1.0 - i.uv.xy) - 0.5);
			mask = ((PI + atan2(uv.x, uv.y) + angleOffset) % TWOPI) / TWOPI;
		}
		#elif SHAPE_RADIAL_SPOKES
		{
			//float2 uv = ((1.0 - i.uv.xy) - 0.5);
			mask = ((PI + atan2(uv.x, uv.y) + angleOffset) % TWOPI) / TWOPI;
			mask = (mask * 2.0) - 1.0;

			mask = (mask * _Spokes) - floor(mask * _Spokes * 0.5) * 2.0;
			mask = 1.0 - abs(mask - 1.0);

			maskRange /= _Spokes;
		}
		#endif

		mask *= maskRange;

		#ifdef UNITY_UI_CLIP_RECT
		return FinishWipe(mask, maskRange, tex2D(_SourceTex, i.uv.xy), i.color, i.mask);
		#else
		return FinishWipe(mask, maskRange, tex2D(_SourceTex, i.uv.xy), i.color, 0.0);
		#endif
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
			Name "Blend-Wipe-Radial"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWipe
			ENDCG
		}
	}
}