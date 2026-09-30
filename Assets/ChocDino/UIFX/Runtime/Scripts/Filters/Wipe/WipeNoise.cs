//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum WipeNoiseShape
	{
		None,
		Horizontal,
		Vertical,
		Diagonal,
	}

	[System.Serializable]
	public class WipeNoise : WipeBase
	{
		[SerializeField] WipeNoiseShape _shape;
		[SerializeField, Min(1f)] float _size = 8f;
		[SerializeField, Range(0f, 2048f)] float _spread = 256f;
		[SerializeField, Min(1f)] float _softness = 1f;

		public float Size { get => _size; set { if (_size != value) { _size = value; SignalDirty(); } } }
		public WipeNoiseShape Shape { get => _shape; set { if (_shape != value) { _shape = value; SignalDirty(); } } }
		public float Spread { get => _spread; set { if (_spread != value) { _spread = value; SignalDirty(); } } }
		public float Softness { get { return _softness; } set { if (_softness != value) { _softness = value; SignalDirty(); } } }

		static class ShaderProp
		{
			public readonly static int Softness = Shader.PropertyToID("_Softness");
			public readonly static int NoiseScale = Shader.PropertyToID("_NoiseScale");
			public readonly static int Spread = Shader.PropertyToID("_Spread");
		}
		static class ShaderKeyword
		{
			public const string ShapeHoriz = "SHAPE_HORIZ";
			public const string ShapeVert = "SHAPE_VERT";
			public const string ShapeDiag = "SHAPE_DIAG";
		}

		private const string ShaderPath = "Hidden/ChocDino/UIFX/Blend-Wipe-Noise";
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
			if (source is WipeNoise sourceWipe)
			{
				this.Shape = sourceWipe.Shape;
				this.Size = sourceWipe.Size;
				this.Spread = sourceWipe.Spread;
				this.Softness = sourceWipe.Softness;
			}
		}

		internal override void Apply(Material material, float resolutionFactor)
		{
			material.DisableKeyword(ShaderKeyword.ShapeHoriz);
			material.DisableKeyword(ShaderKeyword.ShapeVert);
			material.DisableKeyword(ShaderKeyword.ShapeDiag);

			switch (_shape)
			{
				case WipeNoiseShape.Horizontal:
					material.EnableKeyword(ShaderKeyword.ShapeHoriz);
					break;
				case WipeNoiseShape.Vertical:
					material.EnableKeyword(ShaderKeyword.ShapeVert);
					break;
				case WipeNoiseShape.Diagonal:
					material.EnableKeyword(ShaderKeyword.ShapeDiag);
					break;
			}

			material.SetVector(ShaderProp.NoiseScale, new Vector2(_size * resolutionFactor, _size * resolutionFactor));
			material.SetFloat(ShaderProp.Spread, _spread);
			material.SetFloat(ShaderProp.Softness, _softness * resolutionFactor);
		}
	}
}