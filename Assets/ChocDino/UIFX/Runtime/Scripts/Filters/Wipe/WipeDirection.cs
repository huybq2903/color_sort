//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum WipeDirectionShape
	{
		Horizontal,
		Vertical,
		Diagonal,
		DiagonalFlipped,
	}

	[System.Serializable]
	public class WipeDirection : WipeBase
	{
		[SerializeField] WipeDirectionShape _shape;
		[SerializeField, Min(1f)] float _softness = 1f;

		public WipeDirectionShape Shape { get => _shape; set { if (_shape != value) { _shape = value; SignalDirty(); } } }
		public float Softness { get { return _softness; } set { if (_softness != value) { _softness = value; SignalDirty(); } } }

		static class ShaderProp
		{
			public readonly static int Softness = Shader.PropertyToID("_Softness");
			public readonly static int AxisMask = Shader.PropertyToID("_AxisMask");
		}

		private readonly static Vector3 AxisMaskHorizontal = new Vector3(1f, 0f, 0f);
		private readonly static Vector3 AxisMaskVertical = new Vector3(0f, 1f, 0f);
		private readonly static Vector3 AxisMaskDiagonal = new Vector3(1f, 1f, 0f);
		private readonly static Vector3 AxisMaskDiagonalFlipped = new Vector3(1f, 1f, 1f);

		private const string ShaderPath = "Hidden/ChocDino/UIFX/Blend-Wipe-Direction";
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

		public override void CopyFrom(WipeBase source)
		{
			base.CopyFrom(source);
			if (source is WipeDirection sourceWipe)
			{
				this.Shape = sourceWipe.Shape;
				this.Softness = sourceWipe.Softness;
			}
		}

		internal override void Apply(Material material, float resolutionFactor)
		{
			material.SetFloat(ShaderProp.Softness, _softness * resolutionFactor);
			switch (_shape)
			{
				case WipeDirectionShape.Horizontal:
					material.SetVector(ShaderProp.AxisMask, AxisMaskHorizontal);
					break;
				case WipeDirectionShape.Vertical:
					material.SetVector(ShaderProp.AxisMask, AxisMaskVertical);
					break;
				case WipeDirectionShape.Diagonal:
					material.SetVector(ShaderProp.AxisMask, AxisMaskDiagonal);
					break;
				case WipeDirectionShape.DiagonalFlipped:
					material.SetVector(ShaderProp.AxisMask, AxisMaskDiagonalFlipped);
					break;
			}
		}
	}
}