Shader "Hidden/ChocDino/UIFX/Blend-Mask"
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

		_MaskRect("Mask Rect", Vector) = (0, 0, 1, 1)
		_MaskAlpha("Mask Alpha", Float) = 1.0
		_Invert("Invert", Float) = 0.0
		_Strength("Strength", Float) = 1.0
	
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_ColorMask			("Color Mask", Float) = 15
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
		
	#include "BlendUtils.cginc"
	#include "CompUtils.cginc"
	#include "ColorUtils.cginc"

	uniform float4 _MaskRect;
	uniform float _MaskAlpha;
	uniform float _Invert;
	uniform float _Strength;

	struct v2fMask
	{
		float4 vertex : SV_POSITION;
		float4 uv : TEXCOORD0;
		float4 color : TEXCOORD1;
		#ifdef UNITY_UI_CLIP_RECT
		float4 mask : TEXCOORD2;
		#endif

		UNITY_VERTEX_OUTPUT_STEREO // VR SUPPORT
	};

	v2fMask vertMask(appdata v)
	{
		v2fMask o;

		UNITY_SETUP_INSTANCE_ID(v); // VR SUPPORT
		UNITY_INITIALIZE_OUTPUT(v2fMask, o); // VR SUPPORT
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); // VR SUPPORT
	
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv.xy = TRANSFORM_TEX(v.uv, _ResultTex);
		
		float4 screenPos = ComputeScreenPos(o.vertex);
		o.uv.zw = screenPos.xy / screenPos.w; // Perspective-divide is needed to unify Canvas.RenderMode (Overlay and Camera)

		o.color = v.color;

		// 2D rect clipping
		#ifdef UNITY_UI_CLIP_RECT
		{
			float2 pixelSize = o.vertex.w;
			pixelSize /= float2(1.0, 1.0) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			#ifdef MASK_SOFTNESS_OLD
			half2 maskSoftness = half2(_MaskSoftnessX, _MaskSoftnessY);
			#else
			half2 maskSoftness = half2(_UIMaskSoftnessX, _UIMaskSoftnessY);
			#endif
			o.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * maskSoftness + abs(pixelSize.xy)));
		}
		#endif
		
		return o;
	}

	float4 fragMask(v2fMask i) : SV_Target
	{
		// Note: This is already pre-multiplied alpha
		float4 color = tex2D(_SourceTex, i.uv.xy);

		// Apply mask
		{
			// Convert screenPos to normalized UV coords for the Mask rectangle
			float2 maskUV = (i.uv.zw - _MaskRect.xy) / _MaskRect.zw;

			float mask = tex2D(_ResultTex, maskUV).a * _MaskAlpha;
			mask = lerp(mask, 1.0 - mask, _Invert);
			float4 masked = color * mask;

			color = lerp(color, masked, _Strength);
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
		Blend One OneMinusSrcAlpha // Premultiplied transparency
		ColorMask [_ColorMask]

		Pass
		{
			Name "Blend-Mask"
			CGPROGRAM
			#pragma vertex vertMask
			#pragma fragment fragMask
			ENDCG
		}
	}
}
