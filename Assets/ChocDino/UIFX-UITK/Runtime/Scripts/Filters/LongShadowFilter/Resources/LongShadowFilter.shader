Shader "UIFX/UITK/LongShadowFilter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Angle ("Angle", Range(0.0, 360.0)) = 0
		_Radius ("Radius", Range(0.0, 1024.0)) = 8
		_ColorFront ("Color Front", Color) = (0, 0, 0, 1)
		_ColorBack ("Color Back", Color) = (0, 0, 0, 1)
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Blend One OneMinusSrcAlpha
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _UIE_OUTPUT_LINEAR

			#include "UnityCG.cginc"

#if UNITY_VERSION >= 60030000
			#include "UnityUIEFilter.cginc"

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				uint rectIndex : TEXCOORD1;
				float2 pixelStep : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			float _Angle;
			float _Radius;
			float4 _ColorFront;
			float4 _ColorBack;

			v2f vert (FilterVertexInput v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.rectIndex = GetFilterRectIndex(v);
				o.pixelStep = _MainTex_TexelSize.xy * -float2(sin(-_Angle), cos(_Angle));
				return o;
			}

			float2 NormalizeUVs(float2 uv, float4 uvRect)
			{
				// Normalize UV coordinates based on the atlas rect
				return float2(
					(uv.x - uvRect.x) / uvRect.z,
					(uv.y - uvRect.y) / uvRect.w
				);
			}

			float2 MapToUVRect(float2 uv, float4 uvRect)
			{
				// Map UV coordinates to the atlas rect
				return float2(
					uv.x * uvRect.z + uvRect.x,
					uv.y * uvRect.w + uvRect.y
				);
			}

			float4 GetLongShadow(float2 uv, float length, float4 colorFront, float4 colorBack, float2 pixelStep, float4 uvRect)
			{
				float alphaMask = 0.0;
				float distT = 0.0;
				float distance = 0.0;
		
				// Allow minimum 1 sample so that Shadow comp mode can display something at length == 0
				length = max(1, length);
		
				[loop]
				for (int ii = 1; ii <= length; ii++)
				{
					// Early-out, marched out of UV space
					if (uv.y > (uvRect.y + uvRect.w) || 
						uv.y < (uvRect.y) || 
						uv.x > (uvRect.x + uvRect.z) || 
						uv.x < (uvRect.x))
					{
						break;
					}

					float mask = tex2Dlod(_MainTex, float4(uv, 0.0, 0.0)).a;
		
					float t = 1.0 - saturate(distance / length);
					distT = max(distT, mask * t);
					alphaMask = max(alphaMask, mask);
		
					if (alphaMask >= 1.0) break;
		
					distance += 1.0;
					uv += pixelStep;
				}
		
				return lerp(colorBack, colorFront, distT) * alphaMask;
			}

			float4 AlphaComp_Over(float4 src, float4 dst)
			{
				float alpha = src.a + (1.0 - src.a) * dst.a;
				float3 color = src.rgb + (1.0 - src.a) * dst.rgb;
				return float4(color, alpha);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 uvRect = GetFilterUVRect(i.rectIndex);

				float2 uv = NormalizeUVs(i.uv, uvRect);
				uv = MapToUVRect(uv, uvRect);

				float4 frontColor = _ColorFront;
				float4 backColor = _ColorBack;

				#if _UIE_OUTPUT_LINEAR
					frontColor.rgb = LinearToGammaSpace(frontColor.rgb);
					backColor.rgb = LinearToGammaSpace(backColor.rgb);
				#endif

				// Visualise margin
				/*float2 endUV = uv + i.pixelStep * length;
				if (endUV.y > (uvRect.y + uvRect.w) || 
				endUV.y < (uvRect.y) || 
				endUV.x > (uvRect.x + uvRect.z) || 
				endUV.x < (uvRect.x))
				{
					return 1;
				}*/

				// clamp length to edge of uvRect

				float4 color = tex2D(_MainTex, uv);



				float4 shadow = GetLongShadow(uv, _Radius, frontColor, backColor, i.pixelStep, uvRect);
				color = AlphaComp_Over(color, shadow);

				#if _UIE_OUTPUT_LINEAR
				color.rgb = GammaToLinearSpace(color.rgb);
				#endif
				
				return color;
			}

		#else

			void vert()
			{
			}

			fixed4 frag() : SV_Target
			{
				return 1;
			}
			
		#endif
			ENDCG
		}
	}

	Fallback "Unlit/Color"
}