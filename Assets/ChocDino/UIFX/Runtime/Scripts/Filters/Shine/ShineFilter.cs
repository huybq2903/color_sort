//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum ShineAnimationTrigger
	{
		Script,
		OnEnable,
		OnStrength,
	}

	public enum ShineBlendMode
	{
		Blend,
		Additive,
		Advanced = 100,
	}

	public enum ShineFillMode
	{
		Color,
		Gradient,
		Texture,
	}

	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Shine Filter")]
	public class ShineFilter : FilterBase
	{
		[Range(0f, 360f)]
		[Tooltip("The angle in degrees.")]
		[SerializeField] float _angle = 135f;

		[Range(0f, 1000f)]
		[Tooltip("The size in pixels.")]
		[SerializeField] float _size = 64f;

		[Range(0f, 1f)]
		[Tooltip("The softness.")]
		[SerializeField] float _softness = 0.8f;

		[Range(0f, 16f)]
		[Tooltip("The power.")]
		[SerializeField] float _power = 0f;

		[SerializeField] bool _mirror = true;

		[SerializeField] ShineFillMode _fillMode = ShineFillMode.Color;

		[Tooltip("Color")]
		[SerializeField] Color _color = Color.white;

		[Tooltip("Texture")]
		[SerializeField] Texture _texture = null;

		[Tooltip("The gradient to use in OutlineFillMode.Gradient mode.")]
		[SerializeField] Gradient _gradient = ColorUtils.GetBuiltInGradient(BuiltInGradient.PinkYellow);

		[Tooltip("Reverse Ramp")]
		[SerializeField] bool _reverseTexture = false;

		[Range(0f, 1f)]
		[Tooltip("The softness.")]
		[SerializeField] float _offset = 0.5f;

		[SerializeField] ShineAnimationTrigger _animationTrigger = ShineAnimationTrigger.Script;

		[Tooltip("The time delta to use when updating scrolling.")]
		[SerializeField] TimeDeltaMode _scrollDeltaTime = TimeDeltaMode.Normal;

		[Tooltip("The speed to scroll the offset.")]
		[SerializeField] float _scrollSpeed = 350f;

		[Tooltip("The delay between repeating the scroll the offset.")]
		[SerializeField] float _scrollDelay = 0f;

		[Tooltip("The number of times the scroll will repeat.")]
		[SerializeField] int _scrollCount = 1;

		[Tooltip("How to composite the fill with the source graphic.")]
		[SerializeField] ShineBlendMode _blendMode = ShineBlendMode.Additive;

		[Tooltip("How to composite the fill with the source graphic.")]
		[SerializeField] FillBlendMode _advancedBlendMode = FillBlendMode.LinearDodge;

		[Tooltip("Whether to force blend operations in happen gamma-space to match the default in software such as PhotoShop. Otherwise blend operations happen in the active color-space.")]
		[SerializeField] bool _advancedBlendModeGammaSpace = false;

		internal bool IsPreviewScroll 
		{
			get { return _isPreviewScroll; }
			set { if (_isPreviewScroll != value) { _isPreviewScroll = value; if (value) { _prevOffset = _offset; } else { _offset = _prevOffset; } } }
		}

		private float _scroll = 0f;
		private float _scrollDelayTimer = 0f;
		private float _scrollCounter = 0;
		private bool _doScroll;
		private bool _isPreviewScroll;
		private float _prevOffset;

		private int _gradientResolution = 128;
		private GradientTexture _textureFromGradient;

		static new class ShaderProp
		{
			public readonly static int Params = Shader.PropertyToID("_Params");
			public readonly static int Params2 = Shader.PropertyToID("_Params2");
			public readonly static int Color = Shader.PropertyToID("_Color");
			public readonly static int RampPower = Shader.PropertyToID("_RampPower");
			public readonly static int BlendMode = Shader.PropertyToID("_BlendMode");
			public readonly static int GradientTex = Shader.PropertyToID("_GradientTex");
			public readonly static int InvertFactor = Shader.PropertyToID("_InvertFactor");
			public readonly static int Mirror = Shader.PropertyToID("_Mirror");
		}
		static class ShaderKeyword
		{
			public const string FillTexture = "FILL_TEXTURE";
			public const string BlendBlend = "BLEND_BLEND";
			public const string BlendAdvanced = "BLEND_ADVANCED";
			public const string BlendForceGammaSpace = "BLEND_FORCEGAMMASPACE";
		}

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Shine";

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(ShineFilter), "Shine", ShaderUsageCategory.Filters, new[]
			{
				Compositor.CompositeShaderPath, FilterBase.ResolveShaderPath,
				BlendShaderPath
			});
		}
		#endif

		protected override string GetDisplayShaderPath()
		{
			return BlendShaderPath;
		}

		protected override bool DoParametersModifySource()
		{
			if (!base.DoParametersModifySource())
			{
				return false;
			}

			// NOTE: We could return false when (_offset <= 0f || _offset >= 1f) however this can result in the MaxScroll calculation not working so we'll leave out this optimisation for now.
			/*if (!_doScroll)
			{
				if (_offset <= 0f || _offset >= 1f) { return false; }
			}*/

			return true;
		}

		internal bool HasScrollSpeed()
		{
			return _scrollSpeed != 0f;
		}

		public void ResetScroll()
		{
			//_offset = 0f;
			_scrollDelayTimer = 0f;
			_scrollCounter = 0;
			if (_scroll != 0f)
			{
				_scroll = 0f;
				ForceUpdate();
			}
		}

		protected override void Awake()
		{
			_expand = FilterExpand.None;
			base.Awake();
		}

		protected override void OnEnable()
		{
			ResetScroll();
			if (_animationTrigger == ShineAnimationTrigger.OnEnable)
			{
				_doScroll = true;
			}

			Debug.Assert(_textureFromGradient == null);
			if (_fillMode == ShineFillMode.Gradient)
			{
				GradientTexture.Create(ref _textureFromGradient, _gradientResolution);
			}

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			ObjectHelper.Dispose(ref _textureFromGradient);
			base.OnDisable();
		}
		
		public void DoScroll()
		{
			ResetScroll();
			_doScroll = true;
		}

		public void DoScroll(float speed, int repeatCount = 1, float repeatDelay = 0f)
		{
			_scrollSpeed = speed;
			_scrollCount = repeatCount;
			_scrollDelay = repeatDelay;
			DoScroll();
		}

		protected override void Update()
		{
			bool editorPreview = false;
			#if UNITY_EDITOR
				editorPreview = (Application.isPlaying || IsPreviewScroll);
			#endif

			if (HasScrollSpeed()
			#if UNITY_EDITOR
				&& (Application.isPlaying || IsPreviewScroll)
			#endif
			)
			{
				if (_animationTrigger == ShineAnimationTrigger.OnStrength)
				{
					if (_strength > 0.0f)
					{
						if (!_doScroll)
						{
							ResetScroll();
							_doScroll = true;
						}
					}
					else if (_doScroll)
					{
						ResetScroll();
						_doScroll = false;
					}
				}

				if (IsPreviewScroll || _doScroll)
				{
					float deltaTime = 0f;
					if (_scrollDeltaTime == TimeDeltaMode.Normal)
					{
						deltaTime = Time.deltaTime;
					}
					else
					{
						if (Time.captureDeltaTime == 0f)
						{
							deltaTime = Time.unscaledDeltaTime;
						}
						else
						{
							deltaTime = Time.captureDeltaTime;
						}
					}

					if (!Application.isPlaying || IsPreviewScroll)
					{
						if (deltaTime >= Time.maximumDeltaTime)
						{
							// In the editor sometimes the deltaTime can be too large, so just skip this frame and wait for it to resolve.
							return;
						}
					}

					if (CalculateMaxScroll())
					{
						_scroll += Mathf.Abs(_scrollSpeed) * deltaTime;
						_scroll = Mathf.Min(_scroll, _scrollMax);
						if (_scroll >= _scrollMax)
						{
							_scrollDelayTimer += deltaTime;
							if (_scrollDelayTimer >= _scrollDelay)
							{
								_scrollCounter++;
								if (_scrollCount == 0 || _scrollCounter < _scrollCount)
								{
									_scrollDelayTimer = 0f;
									_scroll = 0f;
								}
							}
						}
						_offset = Mathf.Clamp(_scroll / _scrollMax, 0f, 1f);
						if (_scrollSpeed < 0f)
						{
							_offset = 1f - _offset;
						}
					}
					else
					{
						_offset = 0f;
					}
					ForceUpdate();
				}
			}
			base.Update();
		}

		private float _scrollMax;

		bool CalculateMaxScroll()
		{
			var localRect = _screenRect.GetTextureRect();
			if (localRect.width > 0 && localRect.height > 0)
			{
				Vector2 extent = new Vector2((localRect.width * 0.5f) + _size, (localRect.height * 0.5f) + _size);
				Vector2 direction = GetDirection();
				Vector2 dddMin = Vector2.Lerp(-extent, extent, 0.0f) * new Vector2(Mathf.Sign(direction.x), Mathf.Sign(direction.y));
				Vector2 dddMax = Vector2.Lerp(-extent, extent, 1.0f) * new Vector2(Mathf.Sign(direction.x), Mathf.Sign(direction.y));
				float offsetMin = Vector2.Dot(dddMin, direction);
				float offsetMax = Vector2.Dot(dddMax, direction);
				float minDist = -offsetMin - _size;
				float maxDist = -(offsetMax - _size);
				_scrollMax = Mathf.Abs(maxDist - minDist);
			}
			else
			{
				// The rectangle hasn't been calculated yet, so we can't calculate the maximum scroll.
				return false;
			}
			return true;
		}

		private Vector2 GetDirection()
		{
			return new Vector2(Mathf.Sin(-_angle * Mathf.Deg2Rad), Mathf.Cos(-_angle * Mathf.Deg2Rad + Mathf.PI));
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			Vector2 direction = GetDirection();
			_displayMaterial.SetVector(ShaderProp.Params, new Vector4(direction.x, direction.y, _size * ResolutionScalingFactor, Mathf.Max(1.0f, _size * _softness * ResolutionScalingFactor)));

			_displayMaterial.SetVector(ShaderProp.Params2, new Vector4(_offset, 0f, 0f, 0f));

			if (_fillMode == ShineFillMode.Color)
			{
				Color c = _color;
				// Premultiply by alpha
				c.r *= c.a;
				c.g *= c.a;
				c.b *= c.a;
				c *= _strength;
				_displayMaterial.SetColor(ShaderProp.Color, c);
				_displayMaterial.SetTexture(ShaderProp.GradientTex, null);
				_displayMaterial.DisableKeyword(ShaderKeyword.FillTexture);
			}
			if (_fillMode == ShineFillMode.Gradient)
			{
				GradientTexture.Create(ref _textureFromGradient, _gradientResolution);
				_textureFromGradient.Update(_gradient);
				_displayMaterial.SetTexture(ShaderProp.GradientTex, _textureFromGradient.Texture);
				_displayMaterial.SetColor(ShaderProp.Color, Color.white * _strength);
				_displayMaterial.SetFloat(ShaderProp.InvertFactor, 0f);
				_displayMaterial.EnableKeyword(ShaderKeyword.FillTexture);
			}
			else if (_fillMode == ShineFillMode.Texture)
			{
				_displayMaterial.SetTexture(ShaderProp.GradientTex, _texture);
				_displayMaterial.SetColor(ShaderProp.Color, Color.white * _strength);
				_displayMaterial.SetFloat(ShaderProp.InvertFactor, _reverseTexture ? 1f : 0f);
				_displayMaterial.EnableKeyword(ShaderKeyword.FillTexture);
			}

			_displayMaterial.SetFloat(ShaderProp.RampPower, Mathf.Max(0.01f, _power));

			_displayMaterial.SetFloat(ShaderProp.Mirror, _mirror ? 1f : 0f);

			if (_blendMode == ShineBlendMode.Blend)
			{
				_displayMaterial.EnableKeyword(ShaderKeyword.BlendBlend);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendAdvanced);
			}
			else if (_blendMode == ShineBlendMode.Additive)
			{
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendBlend);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendAdvanced);
			}
			else if (_blendMode == ShineBlendMode.Advanced)
			{
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendBlend);
				_displayMaterial.EnableKeyword(ShaderKeyword.BlendAdvanced);
				_displayMaterial.SetInt(ShaderProp.BlendMode, (int)_advancedBlendMode);
				if (!_advancedBlendModeGammaSpace)
				{
					_displayMaterial.DisableKeyword(ShaderKeyword.BlendForceGammaSpace);
				}
				else
				{
					_displayMaterial.EnableKeyword(ShaderKeyword.BlendForceGammaSpace);
				}
			}

			base.SetupDisplayMaterial(source, result);
		}
	}
}