//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Defines the Graphic to use as the source of the mask.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Mask Source Filter")]
	public class MaskSourceFilter : FilterBase
	{
		[SerializeField] bool _showMaskGraphic = true;
		[SerializeField, Range(0f, 1f)] float _invertMask = 0f;

		public bool ShowMaskGraphic { get => _showMaskGraphic; set { ChangeProperty(ref _showMaskGraphic, value); } }
		public float InvertMask { get => _invertMask; set { ChangeProperty(ref _invertMask, value); } }
		internal int DirtyFrame { get; private set; }

		private List<MaskFilter> _targets = new List<MaskFilter>();
		private Texture2D _clearTexture;

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(MaskFilter), "Mask", ShaderUsageCategory.Filters, new[]
			{
				Compositor.CompositeShaderPath, FilterBase.ResolveShaderPath, DefaultBlendShaderPath
			});
		}
		#endif

		public Rect GetMaskScreenRect()
		{
			GenerateScreenRect();
			return GetScreenRect();
		}

		public Texture GetMaskTexture()
		{
			return _composite.GetTexture();
		}

		protected override void Awake()
		{
			_renderSpace = FilterRenderSpace.Screen;
			_rectAdjustOptions.clampToScreen = false;
			base.Awake();
		}

		protected override void OnEnable()
		{
			if (_clearTexture == null)
			{
				_clearTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
				_clearTexture.SetPixel(0, 0, Color.clear);
				_clearTexture.Apply();
			}
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			DirtyFrame = -1;
			UpdateTargets();
			ObjectHelper.Destroy(ref _clearTexture);
			base.OnDisable();
		}

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			var onePixel = new Vector2Int(1, 1);
			leftDown += onePixel;
			rightUp += onePixel;
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			if (_showMaskGraphic)
			{
				base.SetupDisplayMaterial(source, result);
			}
			else
			{
				Debug.Assert(_clearTexture != null);
				base.SetupDisplayMaterial(_clearTexture, _clearTexture);
			}

			DirtyFrame = Time.frameCount + 1;
			// In some cases (such as toggling enable state of another Filter in the same GameObject (eg BlurFilter), 
			// when running in edit mode, the rendering doesn't update - so we force it.
			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				UnityEditor.EditorApplication.delayCall += DeferTargetUpdate;
			}
			#endif
		}

		#if UNITY_EDITOR
		private void DeferTargetUpdate()
		{
			// Always unsubscribe to safeguard against rare edge-case double triggers
			UnityEditor.EditorApplication.delayCall -= DeferTargetUpdate;

			UpdateTargets();

			Canvas.ForceUpdateCanvases();
		}
		#endif

		internal void AddTarget(MaskFilter filter)
		{
			if (!_targets.Contains(filter))
			{
				_targets.Add(filter);
			}
		}

		internal void RemoveTarget(MaskFilter filter)
		{
			_targets.Remove(filter);
		}

		internal override float GetAlpha()
		{
			return this.CanvasRenderComponent.GetInheritedAlpha();
		}

		private void UpdateTargets()
		{
			foreach (var filter in _targets)
			{
				if (filter != null && filter.isActiveAndEnabled)
				{
					filter.ForceUpdate(true);
				}
			}
		}
	}
}