//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Applies a mask (via the MaskSourceFilter component) to the Graphic.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Mask Filter")]
	public class MaskFilter : FilterBase
	{
		[SerializeField] MaskSourceFilter _mask;
		[SerializeField, Range(0f, 1f)] float _invertMask = 0f;

		public MaskSourceFilter Mask { get => _mask; set { ChangePropertyRef(ref _mask, value); } }
		public float InvertMask { get => _invertMask; set { ChangeProperty(ref _invertMask, value); } }

		private MaskSourceFilter _joinedMaskSource;

		static new class ShaderProp
		{
			public readonly static int MaskRect = Shader.PropertyToID("_MaskRect");
			public readonly static int MaskAlpha = Shader.PropertyToID("_MaskAlpha");
			public readonly static int Invert = Shader.PropertyToID("_Invert");
		}

		private Texture _maskSourceTexture;
		private Rect _maskSourceRect;
		private float _maskSourceAlpha;
		private float _maskSourceInvert;
		private float _maskSourceStrength;
		private int _maskSourceDirtyFrame;
		private bool _isMasking;

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Mask";

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(MaskFilter), "Mask", ShaderUsageCategory.Filters, new[]
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
			if (_mask)
			{
				return _mask.IsFiltered();
			}
			return false;
		}

		protected override void Awake()
		{
			_renderSpace = FilterRenderSpace.Screen;
			_rectAdjustOptions.clampToScreen = false;
			base.Awake();
		}

		protected override void OnEnable()
		{
			JoinMask();
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			_isMasking = false;
			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			LeaveMask();
			base.OnDestroy();
		}

		private void JoinMask()
		{
			if (_mask != null && _mask != _joinedMaskSource)
			{
				_mask.AddTarget(this);
				_joinedMaskSource = _mask;
			}
		}

		private void LeaveMask()
		{
			if (_joinedMaskSource != null)
			{
				_joinedMaskSource.RemoveTarget(this);
				_joinedMaskSource = null;
			}
		}

		void LateUpdate()
		{
			// Detect changes to the mask, and then force a render.
			bool maskChanged = false;
			{
				bool isMasking = false;
				if (_mask != null)
				{
					if (_mask.isActiveAndEnabled)
					{
						if (_joinedMaskSource != _mask)
						{
							LeaveMask();
							JoinMask();
						}
						isMasking = true;
						var rect = _mask.GetMaskScreenRect();
						var texture = _mask.GetMaskTexture();
						var alpha = _mask.GetAlpha();
						var invert = _mask.InvertMask;
						var strength = _mask.Strength;
						var dirtyFrame = _mask.DirtyFrame;
						if (texture != _maskSourceTexture || rect != _maskSourceRect || invert != _maskSourceInvert || strength != _maskSourceStrength || dirtyFrame != _maskSourceDirtyFrame || alpha != _maskSourceAlpha)
						{
							maskChanged = true;
						}
					}
				}

				if (isMasking != _isMasking)
				{
					maskChanged = true;
				}
			}

			if (maskChanged)
			{
				this.GraphicComponent.SetMaterialDirty();
				this.GraphicComponent.SetVerticesDirty();
			}
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			if (_mask != null && _mask.isActiveAndEnabled)
			{
				_isMasking = true;
				_maskSourceRect = _mask.GetMaskScreenRect();
				_maskSourceTexture = _mask.GetMaskTexture();
				_maskSourceAlpha = _mask.GetAlpha();
				_maskSourceInvert = _mask.InvertMask;
				_maskSourceStrength = _mask.Strength;
				_maskSourceDirtyFrame = _mask.DirtyFrame;
				_displayMaterial.SetVector(ShaderProp.MaskRect, new Vector4(_maskSourceRect.x, _maskSourceRect.y, _maskSourceRect.width, _maskSourceRect.height));
				_displayMaterial.SetFloat(ShaderProp.MaskAlpha, _maskSourceAlpha);
				_displayMaterial.SetFloat(ShaderProp.Invert, Mathf.Abs(_maskSourceInvert - _invertMask));
				_displayMaterial.SetFloat(FilterBase.ShaderProp.Strength, _maskSourceStrength * _strength);
			}
			else
			{
				_isMasking = false;
				_maskSourceRect = Rect.zero;
				_maskSourceTexture = null;
				_maskSourceAlpha = -1f;
				_maskSourceInvert = -1f;
				_maskSourceStrength = -1f;
				_maskSourceDirtyFrame = -1;
				_displayMaterial.SetFloat(FilterBase.ShaderProp.Strength, 0f);
			}
			base.SetupDisplayMaterial(source, _maskSourceTexture);
		}
	}
}