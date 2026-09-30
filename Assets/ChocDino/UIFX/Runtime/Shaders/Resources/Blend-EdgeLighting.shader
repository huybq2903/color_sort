Shader "Hidden/ChocDino/UIFX/Blend-EdgeLighting"
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

		_LightColor("Light Color", Color) = (1, 1, 1, 1)
		_ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
		_ShadowOffset ("Shadow Offset", Vector) = (0, 0, 0, 0)

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_ColorMask			("Color Mask", Float) = 15
	}

	CGINCLUDE

	#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
	#pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
	#pragma multi_compile_local_fragment __ DROPSHADOW

	#include "BlendUtils.cginc"
	#include "CompUtils.cginc"
	#include "ColorUtils.cginc"

	sampler2D _ShadowTex;
	float4 _LightColor = float4(1.0, 1.0, 1.0, 1.0);
	float4 _ShadowColor = float4(0.0, 0.0, 0.0, 1.0);
	float2 _InsetOffset = float2(0.02, 0.02);
	float2 _ShadowOffset = float2(0.02, 0.02);

	float4 fragDropShadow(v2f i) : SV_Target
	{
		// Note: This is already pre-multiplied alpha
		float4 source = tex2D(_SourceTex, i.uv);

		//return saturate(source + _LightColor * source.a);

		float4 color = float4(0.0, 0.0, 0.0, 0.0);

		float4 lightColor = _LightColor;
		float4 shadowColor = _ShadowColor;

#define MODE_SIMPLE 0
	
#if 0

#if MODE_SIMPLE
		// Draw the shadow+lighting
		float2 offsetuv = i.uv + _ShadowOffset;
		// Have to check bounds otherwise could repeat samples from the edge
		//if (offsetuv.x >= 0.0 && offsetuv.y >= 0.0 && offsetuv.x <= 1.0 && offsetuv.y <= 1.0)
		{
			float4 blur = tex2D(_ResultTex, offsetuv);

			float4 shadow = AlphaComp_In(shadowColor, blur);
			shadow = saturate(shadow);
			color = AlphaComp_Xor(shadow, lightColor * source.a);
			color = saturate(source + color);

			//color = AlphaComp_Over(source, blur);
			//color = source;
			//color += source;
		}
		//else
		{
			// Draw nothing.
		}
#else
		float2 offsetuv = i.uv + _InsetOffset;
		float4 blur = tex2D(_ResultTex, offsetuv);
		float4 blur2 = tex2D(_ResultTex, offsetuv - _InsetOffset - _InsetOffset);

		float lightMask = saturate(max(0.0, source.a - blur.a));
		float innerMask = abs(blur.a * source.a);
		float inner1Mask = abs(blur.a * source.a);
		float inner2Mask = abs(blur2.a * source.a);

		 innerMask = (inner2Mask * innerMask);

		float inner3Mask = max(inner1Mask, innerMask);
		//return inner3Mask;
		float shadowMask = saturate(max(0.0, source.a - blur2.a));

		//lightMask = saturate(lightMask);
		//shadowMask = saturate(shadowMask);

		color = AlphaComp_Over(lightMask * lightColor, source * innerMask);

		//color = AlphaComp_Over(shadowMask * shadowColor, color);
		//return color;
		//return (source * innerMask) + shadowMask * shadowColor;
		//return blur;
		//return /*source * innerMask;*/ (lightMask * lightColor);


		float masky = max(0, source.a - blur.a);
		//return innerMask;
		float4 colLight = lerp(source, Blend_Add(source, _LightColor), lightMask);
		float4 colShadow = lerp(colLight, source * shadowColor, 1.0 - inner2Mask);

		// Note: This is already pre-multiplied alpha
		#if 1 && DROPSHADOW
		{
			color = colShadow;



//			color = colShadow;

//color = 0;

			float4 shadow = AlphaComp_In(_ShadowColor, tex2D(_ShadowTex, i.uv + _ShadowOffset));
			shadow = saturate(shadow * 1.0);
			color = AlphaComp_Over(color, shadow);

			float4 highlight = AlphaComp_In(_LightColor, tex2D(_ShadowTex, i.uv - _ShadowOffset));
			highlight = saturate(highlight * 0.1);
			color = AlphaComp_Over(color, highlight);
			//if (source.a < 1.0) color.a = shadow.a;


			//color = AlphaComp_Over(color, highlight + shadow);

			return color;
		}
		#endif

		return colShadow
		;
		//return lerp(source, Blend_Add(source, _LightColor), lightMask);
		//return AlphaComp_In(_LightColor * masky, source);

		return saturate(source + _LightColor * blur.a);

		//blur.rgb*=blur.a;
		//source.rgb*=source.a;

		float4 light = 0;
		//if (offsetuv.x >= 0.0 && offsetuv.y >= 0.0 && offsetuv.x <= 1.0 && offsetuv.y <= 1.0)
		{
			float mask = max(0, source.a - blur.a);
			float4 mask4 = lerp(float4(0, 0, 0, 1), float4(1, 1, 1, 1), mask);
			return mask * lightColor;
			return AlphaComp_ATop(lightColor, source);
			float4 inverse = AlphaComp_Out(source, blur);
			return source*float4(0, 0, 0, inverse.a);
			float4 shadow = AlphaComp_In(lightColor, inverse);
			shadow = saturate(shadow);
			color = AlphaComp_Over(shadow, color);
			light = saturate(color);
		}
		//else
		{
			//return 1;
		}
		float4 shading = 0;
		{
			offsetuv = i.uv - _ShadowOffset;
			blur = tex2D(_ResultTex, offsetuv);
			float4 inverse = AlphaComp_Out(source.a, blur);
			float4 shadow = AlphaComp_In(shadowColor, inverse);
			//shadow = saturate(shadow);
			//color = AlphaComp_Over(shadow, color);
			//color = AlphaComp_Over(shadow, source);
			shading = saturate(shadow);
			//color.a = source.a;
			//color = source;
		}

		color = source;
		//color = AlphaComp_ATop(light, color);
		//color = AlphaComp_ATop(shading, color);
		//color = shading;



#endif

#else

		{

			float4 blur = tex2D(_ResultTex, i.uv);
			float4 blurLight = tex2D(_ResultTex, i.uv + _InsetOffset);
			float4 blurShadow = tex2D(_ResultTex, i.uv - _InsetOffset);

			blurLight /= blur;
			blurShadow /= blur;

			color = source;

			// Light
			{
				float4 inverse = 1.0 - blurLight;
				float4 light = AlphaComp_In(_LightColor, inverse);
				light = saturate(light);
				//color = AlphaComp_ATop(light, color);
				//color = Blend_Add(color, light) * source.a;
				color = saturate(color + light*color.a);
			}

			// Dark
			{
				float4 inverse = 1.0 - blurShadow;
				float4 shadow = AlphaComp_In(_ShadowColor, inverse);
				shadow = saturate(shadow);
				//color = AlphaComp_ATop(shadow, color);
			}

#if DROPSHADOW
			{
				float4 shadow = AlphaComp_In(_ShadowColor, tex2D(_ShadowTex, i.uv + _ShadowOffset));
				shadow = saturate(shadow);
				color = AlphaComp_Over(color, shadow);

				//float4 highlight = AlphaComp_In(_LightColor, tex2D(_ShadowTex, i.uv - _ShadowOffset));
				//highlight = saturate(highlight * 0.1);
				//color = AlphaComp_Over(color, highlight);
				//if (source.a < 1.0) color.a = shadow.a;


				/*
				// Highlight
				shadow = _LightColor * tex2D(_ShadowTex, i.uv + _ShadowOffset);
				shadow = saturate(shadow);

				 {

					shadow = ToStraight(shadow);
					// 1. Calculate luminance (intensity)
					float luminance = dot(shadow.rgb, float3(0.299, 0.587, 0.114));

					// 2. Create greyscale vec3
					float3 grey = luminance;

					// 3. Mix original color with grey based on saturation factor
					// 0.0 = grey, 1.0 = original, >1.0 = hyper-saturated
					float3 saturatedColor = lerp(grey, shadow.rgb, 1.0);
					saturatedColor *= 1.0;

					shadow = ToPremultiplied(float4(saturatedColor, shadow.a));
				}
				color = AlphaComp_Over(color, shadow);*/


				//color = AlphaComp_Over(color, highlight + shadow);

				//return color;
			}
#endif
		}
#endif


/*
		// Draw the highlight
		float2 offsetuv = i.uv + _ShadowOffset;
		// Have to check bounds otherwise could repeat samples from the edge
		if (offsetuv.x >= 0.0 && offsetuv.y >= 0.0 && offsetuv.x <= 1.0 && offsetuv.y <= 1.0)
		{
			float4 blur = tex2D(_ResultTex, offsetuv);

			float4 inverse = AlphaComp_Out(source, blur);
			float4 shadow = AlphaComp_In(lightColor, inverse);
			shadow = saturate(shadow);
			color = AlphaComp_Over(shadow, color);
		}
		else
		{
			float4 shadow = lightColor * source.a;
			color = AlphaComp_Over(shadow, color);
		}

		// Draw the shadow
		offsetuv = i.uv - _ShadowOffset;
		// Have to check bounds otherwise could repeat samples from the edge
		if (offsetuv.x >= 0.0 && offsetuv.y >= 0.0 && offsetuv.x <= 1.0 && offsetuv.y <= 1.0)
		{
			float4 blur = tex2D(_ResultTex, offsetuv);

			//float4 shadow = AlphaComp_In(shadowColor, blur);
			//shadow = saturate(shadow);
			//color = AlphaComp_Over(color, shadow);

			//float4 inverse = AlphaComp_Out(source, blur);
			float4 shadow = AlphaComp_In(shadowColor, blur);
			shadow = saturate(shadow);
			color = AlphaComp_Xor(shadow, source.a);
			color = AlphaComp_Over(color, source);
		}
		else
		{
			// Draw nothing.
		}
	*/
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
			Name "Blend-DropShadow"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragDropShadow
			ENDCG
		}
	}
}
