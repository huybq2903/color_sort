Shader "Hidden/ChocDino/UIFX/Blend-Wipe-Iris"
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
	#pragma multi_compile_local_fragment SHAPE_AXES SHAPE_BOX SHAPE_CROSS SHAPE_X SHAPE_DIAMOND SHAPE_CIRCLE

	#include "BlendUtils.cginc"
	#include "Common/Wipe.cginc"

	uniform float3 _AxisMask;
	
	float4 fragWipe(v2f i) : SV_Target
	{
		float mask = 0.5;
		float maskRange = 1.0;

		float2 axisLength = _ResultTex_TexelSize.zw;

		float2 uv = (i.uv - 0.5) * axisLength;

		// Halve the axis length because we're centering the UVs
		axisLength *= 0.5;

		#if SHAPE_AXES
		{
			// X-axis
			mask = lerp(uv.x, -uv.x, _AxisMask.z) * _AxisMask.x;
			maskRange = axisLength.x * _AxisMask.x;

			// Y-axis
			mask += uv.y * _AxisMask.y;
			maskRange += axisLength.y * _AxisMask.y;

			mask = abs(mask);
		}
		#elif SHAPE_BOX
		{
			mask = max(abs(uv.x), abs(uv.y));
			maskRange = max(axisLength.x, axisLength.y);
		}
		#elif SHAPE_CROSS
		{
			mask = min(abs(uv.x), abs(uv.y));
			maskRange = min(axisLength.x, axisLength.y);
		}
		#elif SHAPE_X
		{
			mask = abs(abs(uv.x) - abs(uv.y));
			maskRange = max(axisLength.x, axisLength.y);
		}
		#elif SHAPE_DIAMOND
		{
			mask = abs(uv.x) + abs(uv.y);
			maskRange = (axisLength.x + axisLength.y);
		}
		#elif SHAPE_CIRCLE
		{
			mask = length(uv.xy);
			maskRange = length(axisLength);
		}
		#endif

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
			Name "Blend-Wipe-Iris"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWipe
			ENDCG
		}
	}
}