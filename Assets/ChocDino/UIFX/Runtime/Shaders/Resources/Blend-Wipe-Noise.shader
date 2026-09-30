Shader "Hidden/ChocDino/UIFX/Blend-Wipe-Noise"
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
		_NoiseScale("Noise Scale", Vector) = (1, 1, 1, 1)
		_Spread("Spread", Float) = 0.0
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
	#pragma multi_compile_local_fragment _ SHAPE_HORIZ SHAPE_VERT SHAPE_DIAG

	#include "BlendUtils.cginc"
	#include "Common/Wipe.cginc"

	uniform float2 _NoiseScale;
	uniform float _Spread;

	float rand(float2 co) {
		return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
	}

	float4 fragWipe(v2f i) : SV_Target
	{
		float mask = 0.0;
		float maskRange = 0.0;

		float2 aspect = float2(_ResultTex_TexelSize.z / _ResultTex_TexelSize.w, 1.0);
		float2 axisLength = _ResultTex_TexelSize.zw;

		float2 uv = i.uv * axisLength;

		{
			float2 invSnap = 1.0 / _NoiseScale;
			float2 closest = floor(uv * invSnap) / invSnap;

			#if SHAPE_HORIZ
			{
				mask += closest.x;
				maskRange += axisLength.x;
			}
			#elif SHAPE_VERT
			{
				mask += closest.y;
				maskRange += axisLength.y;
			}
			#elif SHAPE_DIAG
			{
				mask += closest.x + closest.y;
				maskRange += axisLength.x + axisLength.y;
			}
			#endif
		}

		float pixelBias = _Spread;

		float2 snappedUV = floor((uv / _NoiseScale)) * _NoiseScale;
		snappedUV /= axisLength;
		mask += rand(snappedUV) * pixelBias;
		maskRange += pixelBias;

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
			Name "Blend-Wipe-Noise"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWipe
			ENDCG
		}
	}
}