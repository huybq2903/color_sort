//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_UITK_FILTERS && UNITY_6000_3_OR_NEWER

using UnityEngine;
using UnityEngine.UIElements;

namespace ChocDino.UIFX.UITK
{
	public class LongShadowFilter
	{
		class Prop
		{
			internal static readonly int Angle = Shader.PropertyToID("_Angle");
			internal static readonly int Radius = Shader.PropertyToID("_Radius");
			internal static readonly int ColorFront = Shader.PropertyToID("_ColorFront");
			internal static readonly int ColorBack = Shader.PropertyToID("_ColorBack");
		}

		private static FilterFunctionDefinition _ffd = null;

		#if UNITY_EDITOR && false
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register("UITK." + nameof(LongShadowFilter), "Long Shadow", ShaderUsageCategory.UIToolkitFilters, new[]
			{
				"UIFX/UITK/LongShadowFilter"
			});
		}
		#endif

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#else
		[RuntimeInitializeOnLoadMethod]
		#endif
		static void InitFilter()
		{
			if (_ffd == null)
			{
				_ffd = Resources.Load<FilterFunctionDefinition>("UIFX-Long-Shadow");
				if (_ffd != null)
				{
					_ffd.passes[0].computeRequiredReadMarginsCallback = ComputeReadMargins;
					_ffd.passes[0].computeRequiredWriteMarginsCallback = ComputeMargins;
					_ffd.passes[0].applySettingsCallback = ApplyFilterPass;
				}
			}
		}

		static void ApplyFilterPass(MaterialPropertyBlock mpb, FilterPassContext context)
		{
			PrepareMaterial(mpb, context.filterFunction);
		}

		static void PrepareMaterial(MaterialPropertyBlock mpb, FilterFunction func)
		{
			var paramRadius = func.GetParameter(1);
			float radius = Mathf.Min(Mathf.Abs(paramRadius.floatValue), 256f);

			float angle = func.GetParameter(0).floatValue;
			angle *= -Mathf.Deg2Rad;
			if (paramRadius.floatValue < 0f)
			{
				angle += Mathf.PI;
			}

			mpb.SetFloat(Prop.Angle, angle);
			mpb.SetFloat(Prop.Radius, radius);

			Color color = func.GetParameter(2).colorValue;
			mpb.SetColor(Prop.ColorFront, new Color(color.r * color.a, color.g * color.a, color.b * color.a, color.a));
			color = func.GetParameter(3).colorValue;
			mpb.SetColor(Prop.ColorBack, new Color(color.r * color.a, color.g * color.a, color.b * color.a, color.a));
		}

		static PostProcessingMargins ComputeReadMargins(FilterFunction func)
		{
			return new PostProcessingMargins()
			{
				left = 0f,
				top = 0f,
				right = 0f,
				bottom = 0f
			};
		}

		static PostProcessingMargins ComputeMargins(FilterFunction func)
		{
			float radius = Mathf.Clamp(func.GetParameter(1).floatValue, 0f, 256f);

			float left, right, up, down;

			/*
									float angle = Mathf.Deg2Rad * func.GetParameter(0).floatValue;
									Vector2 dir = -new Vector2(Mathf.Sin(-angle) * radius, Mathf.Cos(-angle) * radius);
									//radius *= 0.5f;

									float left = Mathf.CeilToInt(Mathf.Abs(Mathf.Min(0f, dir.x)));
									float right = Mathf.CeilToInt(Mathf.Max(0f, dir.x));
									float down = Mathf.CeilToInt(Mathf.Abs(Mathf.Min(0f, dir.y)));
									float up = Mathf.CeilToInt(Mathf.Max(0f, dir.y));

									left = 0f;
									right = 0f;
									down = 0f;
									up = 20f;

									Debug.Log("compute margins LR: " + left + " " + right + " DU: " + down + " " + up);*/

			// For now we just use radius, until we can figure out how to more optimally set the margins
			radius = Mathf.CeilToInt(radius * 1.0f);
			left = radius;
			right = radius;
			up = radius;
			down = radius;

			return new PostProcessingMargins()
			{
				left = left,
				top = up,
				right = right,
				bottom = down
			};
		}
	}
}

#endif