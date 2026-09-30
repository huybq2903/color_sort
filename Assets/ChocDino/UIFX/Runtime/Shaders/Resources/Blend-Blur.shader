Shader "Hidden/ChocDino/UIFX/Blend-Blur"
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
		_ColorMask			("Color Mask", Float) = 15
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local _ UNITY_UI_ALPHACLIP
	#pragma multi_compile_local _ RENDER_MASK

	#include "BlendUtils.cginc"
	#include "CompUtils.cginc"
	#include "ColorUtils.cginc"
	#include "Common/BlendModes.cginc"

	uniform int _BlendMode;
	#define RENDER_MASK 1

	float4 fragBlur(v2f i) : SV_Target
	{
		// Note: This is already pre-multiplied alpha
		float4 result = tex2D(_ResultTex, i.uv);

//		#if RENDER_MASK
		//float4 source = tex2D(_SourceTex, i.uv);
		//result = AlphaComp_Xor(result, source);

		//result *= (1.0-source.a);

		//if (source)

		//if (source.a < 0.01 || source.a > 0.9)
		{
			///return source;
		}

		//result = lerp(result, source, source.a);
//		#endif

		_BlendMode = 0;

		//fill = ToPremultiplied(fill);
		//half4 blend = DoBlend(_BlendMode, result, source);
		//fixed4 color = lerp(source, blend, 1.0);

		float4 color = result;

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
		Blend One OneMinusSrcAlpha // Premultiplied transparency
		ColorMask [_ColorMask]

		Pass
		{
			Name "Blend-Blur"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragBlur
			ENDCG
		}
	}
}