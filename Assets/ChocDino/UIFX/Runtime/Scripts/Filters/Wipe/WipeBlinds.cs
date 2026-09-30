//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum WipeBlindsShape
	{
		Horizontal,
		Vertical,
		Diagonal,
	}

	/// <summary>
	/// The edge of the blind to move forwards.
	/// </summary>
	public enum WipeBlindsEdge
	{
		Front,
		Middle,
		Back,
	}

	[System.Serializable]
	public class WipeBlinds : WipeBase
	{
		[SerializeField] WipeBlindsShape _shape;
		[SerializeField] WipeBlindsEdge _edge;
		[SerializeField, Min(0f)] float _size = 32f;
		[SerializeField, Min(0f)] float _cascade = 32f;
		[SerializeField, Min(1f)] float _softness = 1f;

		public WipeBlindsShape Shape { get => _shape; set { if (_shape != value) { _shape = value; SignalDirty(); } } }
		public WipeBlindsEdge Edge { get => _edge; set { if (_edge != value) { _edge = value; SignalDirty(); } } }
		public float Size { get => _size; set { if (_size != value) { _size = value; SignalDirty(); } } }
		public float Cascade { get => _cascade; set { if (_cascade != value) { _cascade = value; SignalDirty(); } } }
		public float Softness { get { return _softness; } set { if (_softness != value) { _softness = value; SignalDirty(); } } }

		static class ShaderProp
		{
			public readonly static int Softness = Shader.PropertyToID("_Softness");
			public readonly static int BlindsSize = Shader.PropertyToID("_BlindsSize");
			public readonly static int Cascade = Shader.PropertyToID("_CascadeFactor");
			public readonly static int EdgeFactor = Shader.PropertyToID("_EdgeFactor");
		}
		static class ShaderKeyword
		{
			public const string ShapeHorizontal = "SHAPE_HORIZONTAL";
			public const string ShapeVertical = "SHAPE_VERTICAL";
			public const string ShapeDiagonal = "SHAPE_DIAGONAL";
		}

		private readonly static Vector2 EdgeFront = new Vector3(-0.5f, 1f);
		private readonly static Vector2 EdgeMiddle = new Vector3(0f, 0.5f);
		private readonly static Vector2 EdgeBack = new Vector3(0.5f, 1f);

		private const string ShaderPath = "Hidden/ChocDino/UIFX/Blend-Wipe-Blinds";
		protected override string GetShaderPath() { return ShaderPath; }

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
			if (source is WipeBlinds sourceWipe)
			{
				this.Shape = sourceWipe.Shape;
				this.Edge = sourceWipe.Edge;
				this.Size = sourceWipe.Size;
				this.Cascade = sourceWipe.Cascade;
				this.Softness = sourceWipe.Softness;
			}
		}

		internal override void Apply(Material material, float resolutionFactor)
		{
			material.SetFloat(ShaderProp.Softness, _softness * resolutionFactor);
			material.SetFloat(ShaderProp.BlindsSize, _size);
			material.SetFloat(ShaderProp.Cascade, _cascade);

			switch (_edge)
			{
				case WipeBlindsEdge.Front:
					material.SetVector(ShaderProp.EdgeFactor, EdgeFront);
					break;
				case WipeBlindsEdge.Middle:
					material.SetVector(ShaderProp.EdgeFactor, EdgeMiddle);
					break;
				case WipeBlindsEdge.Back:
					material.SetVector(ShaderProp.EdgeFactor, EdgeBack);
					break;
			}

			material.DisableKeyword(ShaderKeyword.ShapeHorizontal);
			material.DisableKeyword(ShaderKeyword.ShapeVertical);
			material.DisableKeyword(ShaderKeyword.ShapeDiagonal);
			switch (_shape)
			{
				case WipeBlindsShape.Horizontal:
					material.EnableKeyword(ShaderKeyword.ShapeHorizontal);
					break;
				case WipeBlindsShape.Vertical:
					material.EnableKeyword(ShaderKeyword.ShapeVertical);
					break;
				case WipeBlindsShape.Diagonal:
					material.EnableKeyword(ShaderKeyword.ShapeDiagonal);
					break;
			}
		}
	}
}