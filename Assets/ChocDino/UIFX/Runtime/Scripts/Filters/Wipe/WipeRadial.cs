//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum WipeRadialhape
	{
		Radial,
		Spokes,
	}

	[System.Serializable]
	public class WipeRadial : WipeBase
	{
		[SerializeField] WipeRadialhape _shape;
		[SerializeField, Min(1)] int _spokes = 10;
		[SerializeField, Min(1f)] float _softness = 1f;

		public WipeRadialhape Shape { get => _shape; set { if (_shape != value) { _shape = value; SignalDirty(); } } }
		public int Spokes { get => _spokes; set { if (_spokes != value) { _spokes = value; SignalDirty(); } } }
		public float Softness { get { return _softness; } set { if (_softness != value) { _softness = value; SignalDirty(); } } }

		private const string ShaderPath = "Hidden/ChocDino/UIFX/Blend-Wipe-Radial";
		protected override string GetShaderPath() { return ShaderPath; }

		static class ShaderProp
		{
			public readonly static int Softness = Shader.PropertyToID("_Softness");
			public readonly static int Spokes = Shader.PropertyToID("_Spokes");
		}
		static class ShaderKeyword
		{
			public const string ShapeRadial = "SHAPE_RADIAL";
			public const string ShapeSpokes = "SHAPE_RADIAL_SPOKES";
		}

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(WipeFilter), "Wipe", ShaderUsageCategory.Filters, new[]
			{
				ShaderPath,
			});
		}
		#endif

		public override void CopyFrom(WipeBase source)
		{
			base.CopyFrom(source);
			if (source is WipeRadial sourceWipe)
			{
				this.Shape = sourceWipe.Shape;
				this.Spokes = sourceWipe.Spokes;
				this.Softness = sourceWipe.Softness;
			}
		}

		internal override void Apply(Material material, float resolutionFactor)
		{
			material.SetFloat(ShaderProp.Softness, _softness * resolutionFactor);
			switch (_shape)
			{
				case WipeRadialhape.Radial:
					material.EnableKeyword(ShaderKeyword.ShapeRadial);
					material.DisableKeyword(ShaderKeyword.ShapeSpokes);
					break;
				case WipeRadialhape.Spokes:
					material.EnableKeyword(ShaderKeyword.ShapeSpokes);
					material.DisableKeyword(ShaderKeyword.ShapeRadial);
					material.SetInt(ShaderProp.Spokes, _spokes);
					break;
			}
		}
	}
}