//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Wipe Filter")]
	public class WipeFilter : FilterBase, IEffectStrength
	{
		[SerializeField] bool _invertDirection = false;
		[SerializeReference] WipeBase _wipeInstance;
		[SerializeField] Easing _easing;

		public WipeBase WipeInstance { get => _wipeInstance; }
		public bool InvertDirection { get { return _invertDirection; } set { ChangeProperty(ref _invertDirection, value); } }
		public Easing Easing { get { return _easing; } set { ChangePropertyRef(ref _easing, value); } }

		private WipeBase _wipe;

		static new class ShaderProp
		{
			public readonly static int Wipe = Shader.PropertyToID("_Wipe");
			public readonly static int InvertDirection = Shader.PropertyToID("_InvertDirection");
		}

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(WipeFilter), "Wipe", ShaderUsageCategory.Filters, new[]
			{
				Compositor.CompositeShaderPath, FilterBase.ResolveShaderPath,
			});
		}
#endif

		protected override string GetDisplayShaderPath()
		{
			if (_wipe != null)
			{
				return _wipe.Shader.name;
			}
			return null;
		}

		public void SetWipe(WipeBase wipe)
		{
			_wipeInstance = wipe;
			CheckForNewWipe();
		}

		private void CheckForNewWipe()
		{
			if (_wipe != _wipeInstance)
			{
				if (_wipe != null)
				{
					_wipe.OnValuesChanged -= OnWipeValuesChanged;
				}
				_wipe = null;
				if (_wipeInstance != null)
				{
					_wipe = _wipeInstance;
					_wipe.OnValuesChanged += OnWipeValuesChanged;
				}
			}

			if (_wipe != null)
			{
				_expand = _wipe.IsExpandable ? FilterExpand.Expand : FilterExpand.None;
				if (_displayMaterial != null && _displayMaterial.shader != _wipe.Shader)
				{
					ObjectHelper.Destroy(ref _displayMaterial);
					_displayMaterial = new Material(_wipe.Shader);
					ForceUpdate();
				}
			}
		}

		protected override void OnEnable()
		{
			CheckForNewWipe();
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			if (_wipe != null)
			{
				_wipe.OnValuesChanged -= OnWipeValuesChanged;
				_wipe = null;
			}
			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			if (_wipe != null)
			{
				_wipe.OnValuesChanged -= OnWipeValuesChanged;
				_wipe = null;
			}
			base.OnDestroy();
		}

		protected override bool DoParametersModifySource()
		{
			if (_displayMaterial == null) return false;
			if (_wipe == null) return false;
			return base.DoParametersModifySource();
		}

		internal override FilterOutputResult GetOutputResult()
		{
			if (_strength >= 1f && CanApplyFilter())
			{
				return FilterOutputResult.Null;
			}
			return base.GetOutputResult();
		}

		void OnWipeValuesChanged()
		{
			OnPropertyChange();
		}

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			if (_wipe != null)
			{
				_wipe.GetFilterAdjustSize(this, ref leftDown, ref rightUp);
			}
		}

		protected override void Update()
		{
			// If undo is pressed the _wipeInstance can change.
			CheckForNewWipe();
			base.Update();
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			_displayMaterial.SetFloat(ShaderProp.InvertDirection, _invertDirection ? 1f : 0f);

			float easedStrength = _easing.Evalulate(_strength);
			_displayMaterial.SetFloat(ShaderProp.Wipe, easedStrength);

			if (_wipe != null)
			{
				_wipe.Apply(_displayMaterial, ResolutionScalingFactor);
			}

			base.SetupDisplayMaterial(source, result);
		}
	}
}