//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.Rendering;
using UnityBlendMode = UnityEngine.Rendering.BlendMode;

namespace ChocDino.UIFX
{
	public enum ROPBlendOp
	{
		//
		// Summary:
		//     Add (s + d).
		Add = BlendOp.Add,
		//
		// Summary:
		//     Subtract.
		Subtract = BlendOp.Subtract,
		//
		// Summary:
		//     Reverse subtract.
		ReverseSubtract = BlendOp.ReverseSubtract,
		//
		// Summary:
		//     Min.
		Min = BlendOp.Min,
		//
		// Summary:
		//     Max.
		Max = BlendOp.Max,
	}

	public enum ROPBlendMode
	{
		Over = 1,
		Darken = 10,
		Multiply = 11,

		// Requires HDR
		[InspectorName("Color Burn (Requires HDR)")]
		ColorBurn = 12,

		// Requires HDR
		[InspectorName("Linear Burn (Requires HDR)")]
		LinearBurn = 13,

		Lighten = 20,
		Screen = 21,

		// Requires HDR
		[InspectorName("Color Dodge (Requires HDR)")]
		ColorDodge = 22,

		[InspectorName("Add | Linear Dodge")]
		Add = 23,
		LinearLight = 34,

		SubtractSoft = 49,
		SubtractSoftOutline = 50,
		SubtractNice = 51,
		Subtract = 52,
		SubtractHard = 53,

		Edge = 1001,

		// Requires HDR
		[InspectorName("Edge Invert (Requires HDR)")]
		EdgeInvert = 1002,

		AlphaWhite = 1003,
		Experimental = 10000,
		Custom = 20000,
	}

	/// <summary>
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - ROP Blend Filter")]
	public class ROPBlendFilter : FilterBase
	{
		[SerializeField] Color _color = Color.white;
		[SerializeField] ROPBlendMode _blendMode = ROPBlendMode.Subtract;
		[SerializeField] ROPBlendOp _customOp;
		[SerializeField] UnityBlendMode _customSrc;
		[SerializeField] UnityBlendMode _customDst;

		/// <summary></summary>
		public Color Color { get { return _color; } set { ChangeProperty(ref _color, value); } }
		public ROPBlendMode BlendMode { get { return _blendMode; } set { ChangeProperty(ref _blendMode, value); } }
		public ROPBlendOp CustomOp { get { return _customOp; } set { ChangeProperty(ref _customOp, value); } }
		public UnityBlendMode CustomSrc { get { return _customSrc; } set { ChangeProperty(ref _customSrc, value); } }
		public UnityBlendMode CustomDst { get { return _customDst; } set { ChangeProperty(ref _customDst, value); } }

		static new class ShaderProp
		{
			public readonly static int Color= Shader.PropertyToID("_Color");
			public readonly static int BlendMode = Shader.PropertyToID("_BlendMode");
			public readonly static int BlendOp = Shader.PropertyToID("_BlendOp");
			public readonly static int BlendSrc = Shader.PropertyToID("_BlendSrc");
			public readonly static int BlendDst = Shader.PropertyToID("_BlendDst");
		}

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-ROP-Blend";

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(ROPBlendFilter), "ROP Blend", ShaderUsageCategory.Filters, new[]
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
			if (_color == Color.white)
			{
				if (_blendMode == ROPBlendMode.Over) return false;
				if (_blendMode == ROPBlendMode.Custom && _customOp == ROPBlendOp.Add && _customSrc == UnityBlendMode.One && _customDst == UnityBlendMode.OneMinusSrcAlpha) return false;
			}
			return true;
		}

		internal static bool IsModeRequiresHdr(ROPBlendMode blendMode)
		{
			switch (blendMode)
			{
				case ROPBlendMode.ColorBurn:
				case ROPBlendMode.LinearBurn:
				case ROPBlendMode.ColorDodge:
				case ROPBlendMode.EdgeInvert:
					return true;
			}
			return false;
		}

		internal bool IsHdrRenderingSupported()
		{
			if (GraphicComponent)
			{
				var canvas = GraphicComponent.canvas;
				if (canvas)
				{
					if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
					{
						if (canvas.worldCamera)
						{
							if (canvas.worldCamera.allowHDR)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			var op = BlendOp.Add;
			var src = UnityBlendMode.One;
			var dst = UnityBlendMode.OneMinusSrcAlpha;

			switch (_blendMode)
			{
				case ROPBlendMode.Darken:
					op = BlendOp.Min;
					break;
				case ROPBlendMode.Multiply:
					op = BlendOp.Add;
					src = UnityBlendMode.DstColor;
					dst = UnityBlendMode.OneMinusSrcAlpha;
					break;
				case ROPBlendMode.ColorBurn:
					op = BlendOp.Add;
					src = UnityBlendMode.One;
					dst = UnityBlendMode.OneMinusSrcColor;
					break;
				case ROPBlendMode.LinearBurn:
					op = BlendOp.Add;
					src = UnityBlendMode.One;
					dst = UnityBlendMode.One;
					break;
				case ROPBlendMode.Lighten:
					op = BlendOp.Max;
					break;
				case ROPBlendMode.Screen:
					op = BlendOp.Add;
					src = UnityBlendMode.One;
					dst = UnityBlendMode.OneMinusSrcColor;
					break;
				case ROPBlendMode.ColorDodge:
					op = BlendOp.Add;
					src = UnityBlendMode.DstColor;
					dst = UnityBlendMode.Zero;
					break;
				case ROPBlendMode.Add:
					op = BlendOp.Add;
					src = UnityBlendMode.SrcAlpha;
					dst = UnityBlendMode.One;
					break;
				case ROPBlendMode.LinearLight:
					//op = BlendOp.Add;
					//src = UnityBlendMode.One;
					//dst = UnityBlendMode.One;
					op = BlendOp.ReverseSubtract;
					src = UnityBlendMode.One;
					dst = UnityBlendMode.One;
					break;
				case ROPBlendMode.Subtract:
					op = BlendOp.ReverseSubtract;
					src = UnityBlendMode.SrcAlpha;
					dst = UnityBlendMode.One;
					break;
				case ROPBlendMode.Edge:
				case ROPBlendMode.EdgeInvert:
				case ROPBlendMode.AlphaWhite:
					op = BlendOp.Add;
					src = UnityBlendMode.OneMinusSrcAlpha;
					dst = UnityBlendMode.One;
					break;
				case ROPBlendMode.SubtractSoft:
					op = BlendOp.ReverseSubtract;
					src = UnityBlendMode.Zero;
					dst = UnityBlendMode.OneMinusSrcColor;
					break;
				case ROPBlendMode.SubtractHard:
					op = BlendOp.ReverseSubtract;
					src = UnityBlendMode.One;
					dst = UnityBlendMode.OneMinusSrcColor;
					break;
				case ROPBlendMode.SubtractNice:
					op = BlendOp.ReverseSubtract;
					src = UnityBlendMode.DstColor;
					dst = UnityBlendMode.OneMinusSrcColor;
					break;
				case ROPBlendMode.SubtractSoftOutline:
					op = BlendOp.ReverseSubtract;
					src = UnityBlendMode.OneMinusSrcAlpha;
					dst = UnityBlendMode.OneMinusSrcColor;
					break;
				case ROPBlendMode.Custom:
					op = (BlendOp)_customOp;
					src = _customSrc;
					dst = _customDst;
					break;
			}

			_displayMaterial.SetColor(ShaderProp.Color, _color);
			_displayMaterial.SetFloat(ShaderProp.BlendMode, (float)_blendMode);
			_displayMaterial.SetFloat(ShaderProp.BlendOp, (float)op);
			_displayMaterial.SetFloat(ShaderProp.BlendSrc, (float)src);
			_displayMaterial.SetFloat(ShaderProp.BlendDst, (float)dst);

			base.SetupDisplayMaterial(source, result);
		}
	}
}