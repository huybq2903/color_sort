//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum WipeIrisShape
	{
		Horizontal,
		Vertical,
		Diagonal,
		DiagonalFlipped,
		Circle,
		Box,
		Diamond,
		Cross,
		X,
	}

	[System.Serializable]
	public class WipeIris : WipeBase
	{
		[SerializeField] WipeIrisShape _shape;
		[SerializeField, Min(1f)] float _softness = 1f;

		public WipeIrisShape Shape { get => _shape; set { if (_shape != value) { _shape = value; SignalDirty(); } } }
		public float Softness { get { return _softness; } set { if (_softness != value) { _softness = value; SignalDirty(); } } }

		static class ShaderProp
		{
			public readonly static int Softness = Shader.PropertyToID("_Softness");
			public readonly static int AxisMask = Shader.PropertyToID("_AxisMask");
		}
		static class ShaderKeyword
		{
			public const string ShapeAxes = "SHAPE_AXES";
			public const string ShapeCircle = "SHAPE_CIRCLE";
			public const string ShapeBox = "SHAPE_BOX";
			public const string ShapeX = "SHAPE_X";
			public const string ShapeCross = "SHAPE_CROSS";
			public const string ShapeDiamond= "SHAPE_DIAMOND";
		}

		private readonly static Vector3 AxisMaskHorizontal = new Vector3(1f, 0f, 0f);
		private readonly static Vector3 AxisMaskVertical = new Vector3(0f, 1f, 0f);
		private readonly static Vector3 AxisMaskDiagonal = new Vector3(1f, 1f, 0f);
		private readonly static Vector3 AxisMaskDiagonalFlipped = new Vector3(1f, 1f, 1f);

		private const string ShaderPath = "Hidden/ChocDino/UIFX/Blend-Wipe-Iris";
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
			if (source is WipeIris sourceWipe)
			{
				this.Shape = sourceWipe.Shape;
				this.Softness = sourceWipe.Softness;
			}
		}

		internal override void Apply(Material material, float resolutionFactor)
		{
			material.SetFloat(ShaderProp.Softness, _softness * resolutionFactor);

			material.DisableKeyword(ShaderKeyword.ShapeAxes);
			material.DisableKeyword(ShaderKeyword.ShapeBox);
			material.DisableKeyword(ShaderKeyword.ShapeX);
			material.DisableKeyword(ShaderKeyword.ShapeCross);
			material.DisableKeyword(ShaderKeyword.ShapeDiamond);
			switch (_shape)
			{
				case WipeIrisShape.Horizontal:
					material.EnableKeyword(ShaderKeyword.ShapeAxes);
					material.SetVector(ShaderProp.AxisMask, AxisMaskHorizontal);
					break;
				case WipeIrisShape.Vertical:
					material.EnableKeyword(ShaderKeyword.ShapeAxes);
					material.SetVector(ShaderProp.AxisMask, AxisMaskVertical);
					break;
				case WipeIrisShape.Diagonal:
					material.EnableKeyword(ShaderKeyword.ShapeAxes);
					material.SetVector(ShaderProp.AxisMask, AxisMaskDiagonal);
					break;
				case WipeIrisShape.DiagonalFlipped:
					material.EnableKeyword(ShaderKeyword.ShapeAxes);
					material.SetVector(ShaderProp.AxisMask, AxisMaskDiagonalFlipped);
					break;
				case WipeIrisShape.Circle:
					material.EnableKeyword(ShaderKeyword.ShapeCircle);
					break;
				case WipeIrisShape.Box:
					material.EnableKeyword(ShaderKeyword.ShapeBox);
					break;
				case WipeIrisShape.Diamond:
					material.EnableKeyword(ShaderKeyword.ShapeDiamond);
					break;
				case WipeIrisShape.Cross:
					material.EnableKeyword(ShaderKeyword.ShapeCross);
					break;
				case WipeIrisShape.X:
					material.EnableKeyword(ShaderKeyword.ShapeX);
					break;
			}
		}
	}
}