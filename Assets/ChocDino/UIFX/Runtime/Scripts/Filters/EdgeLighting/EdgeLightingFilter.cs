//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// A filter for uGUI components that applies a fake edge lighting/shading effect.
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Edge Lighting Filter")]
	public class EdgeLightingFilter : FilterBase
	{
		[Tooltip("The clockwise angle the light and shadow are cast at. Range is [0..360]. Default is 135.0")]
		[Range(0f, 360f)]
		[SerializeField] float _angle = 135f;

		[Tooltip("The distance the light is cast.")]
		[Range(0f, 32f)]
		[SerializeField] float _distance = 2f;

		[Tooltip("The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.")]
		[Range(0f, 32f)]
		[SerializeField] float _blur = 2f;

		[Tooltip("The color of the light")]
		[SerializeField] Color _lightColor = Color.white;

		[Tooltip("The color of the shadow")]
		[SerializeField] Color _shadowColor = Color.black;

		[Tooltip("")]
		[SerializeField] bool _shadowEnabled = true;

		[Tooltip("")]
		[Range(0f, 128f)]
		[SerializeField] float _shadowBlur = 4f;

		[Tooltip("The distance the shadow is cast.")]
		[Range(0f, 64f)]
		[SerializeField] float _shadowDistance = 2f;

		/// <summary>The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0</summary>
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }

		/// <summary>The distance the shadow is cast. Range is [0..1]. Default is 0.03</summary>
		public float Distance { get { return _distance; } set { ChangeProperty(ref _distance, value); } }

		/// <summary>The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.</summary>
		public float Blur { get { return _blur; } set { ChangeProperty(ref _blur, value); } }

		/// <summary>The color of the light</summary>
		public Color LightColor { get { return _lightColor; } set { ChangeProperty(ref _lightColor, value); } }

		/// <summary></summary>
		public bool ShadowEnabled { get { return _shadowEnabled; } set { ChangeProperty(ref _shadowEnabled, value); } }

		/// <summary></summary>
		public float ShadowDistance { get { return _shadowDistance; } set { ChangeProperty(ref _shadowDistance, value); } }

		/// <summary></summary>
		public float ShadowBlur { get { return _shadowBlur; } set { ChangeProperty(ref _shadowBlur, value); } }

		/// <summary>The color of the shadow</summary>
		public Color ShadowColor { get { return _shadowColor; } set { ChangeProperty(ref _shadowColor, value); } }

		private const string BlendEdgeLightingShaderPath = "Hidden/ChocDino/UIFX/Blend-EdgeLighting";

		private GaussianBlurReference _blurEdge = null;
		private BoxBlurReference _blurShadow = null;

		static new class ShaderProp
		{
			public readonly static int InsetOffset = Shader.PropertyToID("_InsetOffset");
			public readonly static int LightColor = Shader.PropertyToID("_LightColor");
			public readonly static int ShadowColor = Shader.PropertyToID("_ShadowColor");
			public readonly static int ShadowTex = Shader.PropertyToID("_ShadowTex");
			public readonly static int ShadowOffset = Shader.PropertyToID("_ShadowOffset");
		}
		static class ShaderKeyword
		{
			public const string DropShadow = "DROPSHADOW";
		}

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(EdgeLightingFilter), "Edge Lighting", ShaderUsageCategory.Filters, new[]
			{
				Compositor.CompositeShaderPath, FilterBase.ResolveShaderPath,
				BlendEdgeLightingShaderPath, BoxBlurReference.BlurShader.Id, GaussianBlurReference.BlurShader.Id
			});
		}
		#endif

		internal override bool CanApplyFilter()
		{
			if (_blurEdge == null) return false;
			if (_blurShadow == null) return false;
			return base.CanApplyFilter();
		}

		protected override bool DoParametersModifySource()
		{
			if (_lightColor.a <= 0f && ShadowColor.a <= 0f) return false;
			if (this.Strength <= 0f) return false;
			//if (_distance > 0f) return true;
			return true;
			//if (_blur <= 0f) return false;
			//return base.DoParametersModifySource();
		}

		protected override string GetDisplayShaderPath()
		{
			return BlendEdgeLightingShaderPath;
		}

		protected override void OnEnable()
		{
			_blurShadow = new BoxBlurReference();
			_blurEdge = new GaussianBlurReference();
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			if (_blurEdge != null)
			{
				_blurEdge.FreeResources();
				_blurEdge = null;
			}
			if (_blurShadow != null)
			{
				_blurShadow.FreeResources();
				_blurShadow = null;
			}
			base.OnDisable();
		}
		
		#if UNITY_EDITOR
		protected override void OnValidate()
		{
			// OnValidate is called when the scene is saved, which causes the material (fields without properties) to lose their properties, so we force update them here.
			if (_blurEdge != null)
			{
				_blurEdge.ForceDirty();
			}
			if (_blurShadow != null)
			{
				_blurShadow.ForceDirty();
			}
	
			base.OnValidate();
		}
		#endif
		
		private static Vector2 AngleToOffset(float angle, Vector2 scale)
		{
			return new Vector2(Mathf.Sin(-angle * Mathf.Deg2Rad) * scale.x, Mathf.Cos(-angle * Mathf.Deg2Rad + Mathf.PI) * scale.y);
		}

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			if (_shadowEnabled)
			{
				float maxOffsetDistance = _shadowDistance * _strength * ResolutionScalingFactor;
				if (maxOffsetDistance > 0f)
				{
					Vector2 offset = -AngleToOffset(_angle, Vector2.one) * maxOffsetDistance;
					offset.x = Mathf.Abs(offset.x);
					offset.y = Mathf.Abs(offset.y);
					leftDown += new Vector2Int(Mathf.CeilToInt(Mathf.Abs(Mathf.Max(0f, offset.x))), Mathf.CeilToInt(Mathf.Abs(Mathf.Max(0f, offset.y))));
					rightUp += new Vector2Int(Mathf.CeilToInt(Mathf.Max(0f, offset.x)), Mathf.CeilToInt(Mathf.Max(0f, offset.y)));
				}
			}

			if (_blurEdge != null)
			{
				SetupFilterParams();
				//_blurfx.AdjustBoundsSize(ref leftDown, ref rightUp);
				_blurEdge.AdjustBoundsSize(ref leftDown, ref rightUp);
			}
			if (_blurShadow != null)
			{
				_blurShadow.AdjustBoundsSize(ref leftDown, ref rightUp);
			}

			leftDown += new Vector2Int(4, 4);
			rightUp += new Vector2Int(4, 4);
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			if (_shadowEnabled)
			{
				_displayMaterial.EnableKeyword(ShaderKeyword.DropShadow);

				Vector2 shadowPixelOffset = AngleToOffset(_angle, Vector2.one);
				shadowPixelOffset *= _shadowDistance * _strength;
				shadowPixelOffset *= ResolutionScalingFactor;
				Vector2 texelStep = new Vector2(1f / source.width, 1f / source.height);
				shadowPixelOffset *= texelStep;
				_displayMaterial.SetVector(ShaderProp.ShadowOffset, shadowPixelOffset);
			}
			else
			{
				_displayMaterial.DisableKeyword(ShaderKeyword.DropShadow);
			}

			{
				Vector2 shadowPixelOffset = AngleToOffset(_angle, Vector2.one);
				shadowPixelOffset *= _distance * _strength;
				shadowPixelOffset *= ResolutionScalingFactor;
				Vector2 texelStep = new Vector2(1f / source.width, 1f / source.height);
				shadowPixelOffset *= texelStep;
				_displayMaterial.SetVector(ShaderProp.InsetOffset, shadowPixelOffset);
			}

			{
				//color.a = Mathf.LerpUnclamped(0f, color.a, this.Strength);
				Color premultiplied = _lightColor;
				premultiplied.r *= premultiplied.a;
				premultiplied.g *= premultiplied.a;
				premultiplied.b *= premultiplied.a;
				_displayMaterial.SetColor(ShaderProp.LightColor, premultiplied);

				premultiplied = _shadowColor;
				premultiplied.r *= premultiplied.a;
				premultiplied.g *= premultiplied.a;
				premultiplied.b *= premultiplied.a;
				_displayMaterial.SetColor(ShaderProp.ShadowColor, _shadowColor);
			}

			base.SetupDisplayMaterial(source, result);
		}

		private void SetupFilterParams()
		{
			_blurShadow.IterationCount = 2;
			_blurShadow.Downsample = Downsample.Auto;
			_blurShadow.SetBlurSize(_shadowBlur * _strength * ResolutionScalingFactor);

			_blurEdge.Downsample = Downsample.None;
			_blurEdge.SetBlurSize(_blur * _strength * ResolutionScalingFactor);
		}

		protected override RenderTexture RenderFilters(RenderTexture source)
		{
			if (_blur > 0f || (_shadowEnabled && _shadowBlur > 0f))
			{
				SetupFilterParams();
				if (_shadowEnabled)
				{
					var shadowTexture = _blurShadow.Process(source);
					_displayMaterial.SetTexture(ShaderProp.ShadowTex, shadowTexture);
				}
				if (_blur > 0f)
				{
					return _blurEdge.Process(source);
				}
			}
			return source;
		}
	}
}