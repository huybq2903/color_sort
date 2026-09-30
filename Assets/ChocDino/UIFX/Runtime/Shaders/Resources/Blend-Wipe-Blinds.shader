Shader "Hidden/ChocDino/UIFX/Blend-Wipe-Blinds"
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
		_InvertDirection ("Invert Direction", Float) = 0.0
		_BlindsSize ("Blinds Size", Float) = 32.0
		_CascadeFactor("Cascade Factor", Float) = 32.0
		_EdgeFactor ("Edge Factor", Vector) = (0.0, 0.5, 0.0, 0.0)
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
	#pragma multi_compile_local_fragment SHAPE_HORIZONTAL SHAPE_VERTICAL SHAPE_DIAGONAL

	#include "BlendUtils.cginc"
	#include "Common/Wipe.cginc"

	uniform float _BlindsSize;
	uniform float _CascadeFactor;
	uniform float2 _EdgeFactor;

	float4 fragWipe(v2f i) : SV_Target
	{
		float mask = 0.5;
		float maskRange = 1.0;
		float2 axisLength = 1.0;

		float xyAspectRatio = _ResultTex_TexelSize.z / _ResultTex_TexelSize.w;
		if (xyAspectRatio > 1.0)
		{
			axisLength.x = xyAspectRatio;
		}
		else
		{
			axisLength.y = 1.0 / xyAspectRatio;
		}

		float2 uv = i.uv;
		uv *= axisLength;

		#if SHAPE_HORIZONTAL
		{
			axisLength = _ResultTex_TexelSize.zw;
			uv = i.uv * axisLength;

			float m = _BlindsSize;

			float edgeOffset = _EdgeFactor.x;
			float maxDist = m * _EdgeFactor.y;
			float closestEdge = round((uv + (m * edgeOffset)) / m) * m;
			mask = abs(uv.x - closestEdge);
			maskRange = maxDist;

			mask += (i.uv.x * _CascadeFactor); // cascade factor
			maskRange += _CascadeFactor;
		}
		#elif SHAPE_VERTICAL
		{
			axisLength = _ResultTex_TexelSize.zw;
			uv = i.uv * axisLength;

			float m = _BlindsSize;

			float edgeOffset = _EdgeFactor.x;
			float maxDist = m * _EdgeFactor.y;
			float closestEdge = round((uv.y + (m * edgeOffset)) / m) * m;
			mask = abs(uv.y - closestEdge);
			maskRange = maxDist;

			mask += (i.uv.y * _CascadeFactor); // cascade factor
			maskRange += _CascadeFactor;
		}
		#elif SHAPE_DIAGONAL
		{
			axisLength = _ResultTex_TexelSize.zw;

			float m = (_BlindsSize + _BlindsSize);

			float edgeOffset = _EdgeFactor.x;
			float maxDist = m * _EdgeFactor.y;
			float diagonalPos = i.uv.x * axisLength.x + i.uv.y * axisLength.y;
			float snappedPos = round((diagonalPos + (m * edgeOffset)) / m) * m;
			float axisDistance = abs(diagonalPos - snappedPos);
			mask = axisDistance;
			maskRange = maxDist;

			mask += (i.uv.x * _CascadeFactor); // cascade factor
			mask += (i.uv.y * _CascadeFactor / xyAspectRatio); // cascade factor
			maskRange += _CascadeFactor;
			maskRange += _CascadeFactor / xyAspectRatio;
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
			Name "Blend-Wipe-Blinds"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWipe
			ENDCG
		}
	}
}