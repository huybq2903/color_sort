Shader "Hidden/ChocDino/UIFX/Blend-Wipe-Direction"
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
		float mask = 0.5;
		float maskRange = 1.0;
		float2 axisLength = _ResultTex_TexelSize.zw;

		float2 uv = i.uv;
		uv *= axisLength;
		
		mask = 0.0;
		maskRange = 0.0;

		// X-axis
		float xflip = _AxisMask.z;
		mask += lerp(uv.x, axisLength.x - uv.x, xflip) * _AxisMask.x;
		maskRange += axisLength.x * _AxisMask.x;

		// Y-axis
		mask += (axisLength.y - uv.y) * _AxisMask.y;
		maskRange += axisLength.y * _AxisMask.y;

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
			Name "Blend-Wipe-Direction"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWipe
			ENDCG
		}
	}
}