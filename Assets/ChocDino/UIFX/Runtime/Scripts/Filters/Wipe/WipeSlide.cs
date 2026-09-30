//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum WipeSlideDirection
	{
		Horizontal,
		Vertical,
		Diagonal,
		DiagonalFlipped,
	}

	[System.Serializable]
	public class WipeSlide : WipeBase
	{
		[SerializeField] WipeSlideDirection _direction;
		[SerializeField, Min(1f)] float _softness = 1f;

		public WipeSlideDirection Direction { get => _direction; set { if (_direction != value) { _direction = value; SignalDirty(); } } }
		public float Softness { get { return _softness; } set { if (_softness != value) { _softness = value; SignalDirty(); } } }

		private const string ShaderPath = "Hidden/ChocDino/UIFX/Blend-Wipe-Slide";
		protected override string GetShaderPath() { return ShaderPath; }

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(WipeFilter), "Wipe", ShaderUsageCategory.Filters, new[]
			{
				ShaderPath
			});
		}
		#endif

		static class ShaderProp
		{
			public readonly static int Softness = Shader.PropertyToID("_Softness");
			public readonly static int SoftnessPower = Shader.PropertyToID("_FalloffPower");
			public readonly static int AxisMask = Shader.PropertyToID("_AxisMask");
		}

		private readonly static Vector3 AxisMaskHorizontal = new Vector3(1f, 0f, 0f);
		private readonly static Vector3 AxisMaskVertical = new Vector3(0f, 1f, 0f);
		private readonly static Vector3 AxisMaskDiagonal = new Vector3(1f, 1f, 0f);
		private readonly static Vector3 AxisMaskDiagonalFlipped = new Vector3(1f, 1f, 1f);

		internal override bool IsExpandable => true;

		public override void CopyFrom(WipeBase source)
		{
			base.CopyFrom(source);
			if (source is WipeSlide sourceWipe)
			{
				this.Direction = sourceWipe.Direction;
				this.Softness = sourceWipe.Softness;
			}
		}

		internal override void GetFilterAdjustSize(FilterBase filter, ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			Vector3 axis = GetAxisFromDirection(_direction);

			// TODO: Optimisation - only expand in the direction needed (need to know _InvertDirection).
			float paddingX = _softness * filter.ResolutionScalingFactor;
			float paddingY = paddingX;
			paddingX *= axis.x;
			paddingY *= axis.y;

			// Ensure there's at least 1 pixel empty border to prevent sampler clamping or repeating.
			paddingX = Mathf.Max(1f, paddingX);
			paddingY = Mathf.Max(1f, paddingY);

			leftDown += new Vector2Int(Mathf.RoundToInt(paddingX), Mathf.RoundToInt(paddingY));
			rightUp += new Vector2Int(Mathf.RoundToInt(paddingX), Mathf.RoundToInt(paddingY));
		}

		private static Vector3 GetAxisFromDirection(WipeSlideDirection direction)
		{
			switch (direction)
			{
				case WipeSlideDirection.Horizontal:
					return AxisMaskHorizontal;
				case WipeSlideDirection.Vertical:
					return AxisMaskVertical;
				case WipeSlideDirection.Diagonal:
					return AxisMaskDiagonal;
				case WipeSlideDirection.DiagonalFlipped:
					return AxisMaskDiagonalFlipped;
			}
			return AxisMaskHorizontal;
		}

		internal override void Apply(Material material, float resolutionFactor)
		{
			material.SetFloat(ShaderProp.Softness, _softness * resolutionFactor);
			material.SetVector(ShaderProp.AxisMask, GetAxisFromDirection(_direction));
		}
	}
}