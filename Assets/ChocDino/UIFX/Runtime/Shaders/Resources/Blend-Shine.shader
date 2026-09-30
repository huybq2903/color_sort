Shader "Hidden/ChocDino/UIFX/Blend-Shine"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _ResultTex ("Sprite Texture", 2D) = "white" {}
		[PerRendererData] _GradientTex("Gradient Texture", 2D) = "white" {}

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_Color("Color", Color) = (1, 1, 1, 1)

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_ColorMask			("Color Mask", Float) = 15
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
	#pragma multi_compile_local_fragment _ BLEND_BLEND BLEND_ADVANCED
	#pragma multi_compile_local_fragment _ BLEND_FORCEGAMMASPACE
	#pragma multi_compile_local_fragment _ FILL_TEXTURE

	#include "BlendUtils.cginc"
	#include "ColorUtils.cginc"
	#include "CompUtils.cginc"
	#include "Common/BlendModes.cginc"

	uniform float4 _Params; //direction.xy, distance, softness
	uniform float4 _Params2; // offset
	uniform float4 _Color;
	uniform float _RampPower;
	uniform sampler2D _GradientTex;
	uniform int _BlendMode;
	uniform float _InvertFactor;
	uniform float _Mirror;

	float4 frag(v2f i) : SV_Target
	{
		float4 color = tex2D(_ResultTex, i.uv);

		float2 textureSize = _ResultTex_TexelSize.zw;

		// Scale UV by to pixels
		float2 uv = i.uv.xy * textureSize;

		// Middle of the texture
		float2 uvCenter = float2(0.5, 0.5) * textureSize;

		float2 direction = _Params.xy;
		float size = _Params.z;

		// Signed distance from middle of ramp
		float dist = dot(uv - uvCenter, direction);

		// Work out distance range
		float textureWidth = _ResultTex_TexelSize.z;
		float textureHeight = _ResultTex_TexelSize.w;
		float2 extent = float2((textureWidth * 0.5) + size, (textureHeight * 0.5) + size);
		float2 ddd = lerp(-extent, extent, _Params2.x) * sign(direction);
		float offset = dot(ddd, direction);
		float minDist = -offset - size;
		float maxDist = -(offset - size);

		// Convert distance to 0..1 ramp (for mapping to texture/gradient)
		float ramp = saturate(1.0 - ((dist - minDist) / (maxDist - minDist)));
		if (_Mirror > 0.5)
		{
			ramp = saturate((size - abs(dist + offset)) / ((maxDist - minDist) * 0.5));
		}

		float softness = _Params.w;

		/*
		float distanceFalloff = 1.0 - saturate(abs(dist + offset) / (size * 0.5));
		
		// Improve look of the ramp
		float dd= abs(dist + offset);
		ramp = pow(55.0 / dd, _RampPower);
		ramp *= distanceFalloff;
		//ramp = saturate(ramp);
		ramp = 1.0 - exp(-ramp);
		//ramp = saturate(1.0 / (ramp * 10));
		*/
		
		 
		// Convert distance to 0..1..0 mask (to soften edges of ramp)
		float edgeDistance = size - abs(dist + offset);
		float aaf = saturate(edgeDistance + 0.5) * softness;
		float mask = smoothstep(0.0, aaf, edgeDistance);

		//ramp = pow(ramp, 4.0);

		
		// Shine color
		float shine = (mask);
		float4 shineColor = _Color * shine;

		#if FILL_TEXTURE
		ramp = abs(_InvertFactor - ramp);
		shineColor = tex2D(_GradientTex, float2(ramp, 0.5));
		shineColor *= shineColor.a * mask * _Color;
		#endif

		// Blend
		#if BLEND_ADVANCED
		color = DoBlend(_BlendMode, color, shineColor);
		#elif BLEND_BLEND
		color = AlphaComp_ATop(shineColor, color);
		#else
		//shineColor.a = 0.0;
		//color = color + shineColor * color.a;
		color.rgb += shineColor.rgb * color.a;
		#endif

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
			Name "Blend-Shine"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}