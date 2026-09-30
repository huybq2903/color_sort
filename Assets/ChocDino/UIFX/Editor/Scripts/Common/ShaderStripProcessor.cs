//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace ChocDino.UIFX.Editor
{
	class ShaderStripProcessor : IPreprocessShaders
	{
		private List<string> _shadersToKeep;
		private List<string> _shadersStripped;

		public ShaderStripProcessor()
		{
			if (!EditorUserBuildSettings.development)
			{
				var settings = ShaderStripSettings.FindAsset();
				if (settings != null && settings.Enabled && settings.ShaderStripEffectNames.Length > 0)
				{
					_shadersToKeep = settings.GetShadersToKeep();
					_shadersStripped = new List<string>(64);
				}
			}
		}

		public int callbackOrder { get { return 0; } }

		public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
		{
			if (_shadersToKeep != null)
			{
				var shaderName = shader.name;
				// Check this is a UIFX shader.
				bool isShaderUIFX = shaderName.Contains("ChocDino/UIFX");
				if (isShaderUIFX)
				{
					// Check
					if (!_shadersToKeep.Contains(shaderName))
					{
						// Clear all variants.
						data.Clear();

						// Log unique shader strips.
						if (!_shadersStripped.Contains(shaderName))
						{
							_shadersStripped.Add(shaderName);
							Debug.Log("[UIFX] Stripped shader '" + shaderName + "' from build.");
						}
					}
				}
			}
		}
	}
}