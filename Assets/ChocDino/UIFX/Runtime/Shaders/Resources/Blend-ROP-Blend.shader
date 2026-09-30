Shader "Hidden/ChocDino/UIFX/Blend-ROP-Blend"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _ResultTex("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _SourceTex("Source Texture", 2D) = "white" {}

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_ColorMask			("Color Mask", Float) = 15

		_BlendMode("BlendMode", Float) = 0.0
		_BlendOp("Blend Op", Float) = 0.0
		_BlendSrc("Blend Src", Float) = 0.0
		_BlendDst("Blend DSt", Float) = 0.0
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP

	#include "BlendUtils.cginc"
	#include "CompUtils.cginc"
	#include "ColorUtils.cginc"
	#include "Common/BlendModes.cginc"

	float _BlendMode;
	float _Stength;
	float4 _Color;

	/// NOTE: Based on blog post "Photoshop Blend Modes Without Backbuffer Copy"
	/// by The Code Corsair: https://www.elopezr.com/photoshop-blend-modes-in-unity/
	/// 
	float4 frag(v2f i) : SV_Target
	{
		// Note: This is already pre-multiplied alpha
		float4 color = tex2D(_ResultTex, i.uv.xy);
		
		// Apply tint
		color *= _Color;

		if (_BlendMode == BLENDMODE_BLEND)
		{
			// Over
		}
		if (_BlendMode == BLENDMODE_DARKEN)
		{
			// Darken (not perfect for low alpha values)
			color.rgb = lerp(float3(1.0, 1.0, 1.0), color.rgb, color.a);
		}
		else if (_BlendMode == BLENDMODE_MULTIPLY)
		{
			// Multiply
			color.rgb *= color.a;
		}
		else if (_BlendMode == BLENDMODE_COLOR_BURN)
		{
			// Color Burn (looks more correct in gamma colorspace)
			// NOTE: this requires HDR buffer
			color.rgb = 1.0 - (1.0 / max(0.001, color.rgb * color.a + 1.0 - color.a)); // max to avoid infinity
		}
		else if (_BlendMode == BLENDMODE_LINEAR_BURN)
		{
			// Linear Burn (looks more correct in gamma colorspace)
			// NOTE: this requires HDR buffer
			color.rgb = (color.rgb  - 1.0) * color.a;
		}
		else if (_BlendMode == BLENDMODE_LIGHTEN)
		{
			// Lighten
			color.rgb *= color.a;
		}
		else if (_BlendMode == BLENDMODE_SCREEN)
		{
			// Screen (looks more correct in gamma colorspace)
			color.rgb *= color.a;
		}
		else if (_BlendMode == BLENDMODE_COLOR_DODGE)
		{
			// Color Dodge (looks more correct in gamma colorspace)
			// NOTE: this requires HDR buffer
			color.rgb = 1.0 / max(0.01, (1.0 - color.rgb * color.a));
		}
		else if (_BlendMode == BLENDMODE_LINEAR_DODGE)
		{
			// Addition / Linear Dodge (looks more correct in linear colorspace)
		}
		else if (_BlendMode == BLENDMODE_LINEAR_LIGHT)
		{
			//color.rgb = (2 * color.rgb - 1.0) * color.a;
			color.rgb = -(2.0 * color.rgb - 1.0) * color.a;
		}
		else if (_BlendMode == BLENDMODE_SUBTRACT)
		{
			// Subtract (looks more correct in linear colorspace)
			color.rgb *= color.a;
		}
		else if (_BlendMode == 1001)
		{
			// Edge
		}
		else if (_BlendMode == 1002)
		{
			// EdgeInvert
			color.rgb = -color.rgb;
		}
		else if (_BlendMode == 1003)
		{
			// AlphaWhite
			color.rgb = 1 - color.rgb;
		}
		else if (_BlendMode == 1004)
		{
			// Subtract Soft
		}
		else if (_BlendMode == 1005)
		{
			// Subtract Hard
		}
		else if (_BlendMode == 1006)
		{
			// Subtract Nice
		}
		else if (_BlendMode == 1007)
		{
			// Subtract Soft Outline
		}
		else if (_BlendMode == 10000)
		{
			// Experimental
			
			//color.rgb /= color.a;
			//color.a = 5.5;
			
			//color.a =  -color.a;
			//color.rgb = 1-color.rgb;
			//color.rgb = pow(color.rgb, 1.0 / color.a);
	//		color.rgb *= color.a;
			//color.rgb = -(3.0 * color.rgb - 1.0) * color.a;
			//color.rgb = 1.0 / max(0.01, (1.0 - color.rgb * color.a));
			//color.rgb = (1.0 - color.rgb) / color.a;
			//color.rgb = 1.0 - (1.0 / max(0.001, color.rgb * color.a + 1.0 - color.a)); // max to avoid infinity
		}
		else if (_BlendMode == 20000)
		{
			// Custom
		}

		// 2D rect clipping
		#ifdef UNITY_UI_CLIP_RECT
		color = ApplyClipRect(color, i.mask);
		#endif

		// Alpha clipping
		#ifdef UNITY_UI_ALPHACLIP
		clip (color.a - 0.001);
		#endif

		color.rgb *= i.color.a;
		color *= i.color;

		return color;
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
		ColorMask[_ColorMask]
		BlendOp[_BlendOp]
		Blend [_BlendSrc][_BlendDst]

		//BlendOp RevSub
		//Blend One OneMinusDstAlpha, SrcColor DstColor

		Pass
		{
			Name "Blend-ROP-Blend"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}