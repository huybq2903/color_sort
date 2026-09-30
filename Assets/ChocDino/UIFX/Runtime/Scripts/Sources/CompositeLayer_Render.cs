//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

namespace ChocDino.UIFX
{
	public partial class CompositeLayer
	{
		// NOTE: This must be FilterRenderSpace.Screen
		private FilterRenderSpace _renderSpace = FilterRenderSpace.Screen;

		protected ScreenRectFromMeshes _screenRect = new ScreenRectFromMeshes();
		internal Compositor _composite = new Compositor();
		protected Material _displayMaterial;
		internal float ResolutionScalingFactor { get; private set; }
		protected RectAdjustOptions _rectAdjustOptions = new RectAdjustOptions();
		private bool _isRenderDirty = true;

		internal const string DefaultBlendShaderPath = "Hidden/ChocDino/UIFX/Blend";

		protected static class ShaderProp
		{
			public readonly static int ResultTex = Shader.PropertyToID("_ResultTex");
		}

		protected bool GenerateRenderGeometry(Camera renderCamera)
		{
			bool result = false;

			// Calculate screen area
			{
				// TODO: only recalculate when mesh/camera changes
				_screenRect.Start(_renderSpace == FilterRenderSpace.Canvas ? null : renderCamera, _renderSpace);

				if (_childList.Count > 0)
				{
					for (int i = 0; i < _childList.Count; i++)
					{
						ICompositeChild child = _childList[i];
						if (child != null && child.IsRenderable())
						{
							var childMesh = _childList[i].GetMesh();
							Debug.Assert(childMesh != null);
							if (childMesh != null)
							{
								_screenRect.AddMeshBounds(_renderSpace == FilterRenderSpace.Canvas ? null : _childList[i].GetTransform(), childMesh);
							}
						}
					}
				}
				_screenRect.End();
				_screenRect.OptimiseRects(_rectAdjustOptions);
			}

			if (_screenRect.GetRect().width > 0 && _screenRect.GetRect().height > 0)
			{
				if (_screenRect.GetRect().width <= Filters.GetMaxiumumTextureSize() && _screenRect.GetRect().height <= Filters.GetMaxiumumTextureSize())
				{
					result = true;
				}
				else
				{
					// NOTE: If texture size limit has been reached, then the previously generates textures are used.
					// This is incorrect but better than a crash or undefined behaviour.
					result = true;
				}
			}
			else
			{
				// There is nothing to render since the rect area is zero
			}
			return result;
		}

		private void RenderToTexture(Camera renderCamera)
		{
			//Debug.Log(Time.frameCount + " <color=red>RenderToTexture</color> " + this.name, this);
			GenerateRenderGeometry(renderCamera);
			if (_composite.Start(_renderSpace == FilterRenderSpace.Canvas ? null : renderCamera, _screenRect.GetTextureRect(), false, _renderSpace == FilterRenderSpace.Canvas ? canvas.scaleFactor : 1f))
			{
				if (_childList.Count > 0)
				{
					for (int i = 0; i < _childList.Count; i++)
					{
						ICompositeChild child = _childList[i];
						if (child != null)
						{
							if (child.IsRenderable())
							{
								var childMaterial = child.GetMaterial();
								var childMesh = child.GetMesh();
								Debug.Assert(childMaterial != null);
								Debug.Assert(childMesh != null);
								if (childMaterial != null && childMesh != null)
								{
									bool materialOutputPremultipliedAlpha = MaterialHelper.MaterialOutputsPremultipliedAlpha(childMaterial);
									_composite.AddMesh(_renderSpace == FilterRenderSpace.Canvas ? null : child.GetTransform(), childMesh, childMaterial, materialOutputPremultipliedAlpha, false);
								}
							}
						}
					}
				}
				_composite.End();
			}
		}

		private bool _isRenderingChildren;

		private void RenderChildren()
		{
			_isRenderingChildren = true;
			// TODO: force all children to have material and mesh.
			foreach (var child in _childList)
			{
				if (child.IsRenderable())
				{
					child.UpdateMeshMaterial();
				}
			}
			_isRenderingChildren = false;
		}

		void Render()
		{
			if (IsDestroyed())
			{
				Debug.Log("this has been destroyed " + this, this);
				Debug.Log("this has been destroyed " + this.name, this);
				return;
			}
			if (_isRenderDirty)
			{
				RenderChildren();

				Camera renderCamera = GetRenderCamera(this.canvas);
				ResolutionScalingFactor = GetResolutionScalingFactor(this.canvas);

				RenderToTexture(renderCamera);

				//_displayMaterial.mainTexture = _composite.GetTexture();
				if (_displayMaterial)
				{
					_displayMaterial.SetTexture(ShaderProp.ResultTex, _composite.GetTexture());

					_isRenderDirty = false;
				}
			}
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			//Debug.Log(Time.frameCount + " <color=green>OnPopulateMesh</color> " + this.name, this);
			Render();
			BuildOutputQuad(vh);
		}

		private void BuildOutputQuad(VertexHelper verts)
		{
			verts.Clear();
			if (_renderSpace == FilterRenderSpace.Canvas)
			{
				_screenRect.BuildScreenQuad(null, null, this.color, verts);
			}
			else
			{
				Camera renderCamera = GetRenderCamera(this.canvas);
				_screenRect.BuildScreenQuad(renderCamera, GetOverrideRoot(), this.color, verts);
			}
		}

		public override Material GetModifiedMaterial(Material baseMaterial)
		{
			if (_displayMaterial)
			{
				// Copy the stencil properties to the display material
				if (_displayMaterial)
				{
					if (this.maskable)
					{
						var materialStencil = base.GetModifiedMaterial(baseMaterial);
						UnityShaderProp.CopyStencilProperties(materialStencil, _displayMaterial);
					}
					else
					{
						UnityShaderProp.ResetStencilProperties(_displayMaterial);
					}
				}
				return _displayMaterial;
			}
			return baseMaterial;
		}


		/*void OnPreWillRenderCanvas()
		{
			//bool isRebuilding = CanvasUpdateRegistry.IsRebuildingGraphics();
			//Debug.Log(Time.frameCount + " OnPreWillRenderCanvas " + this.name + " isRebuilding: " + isRebuilding, this);
			//Render();
		}*/

		void OnWillRenderCanvas()
		{
			//bool isRebuilding = CanvasUpdateRegistry.IsRebuildingGraphics();
			//Debug.Log(Time.frameCount + " OnWillRenderCanvas " + this.name + " isRebuilding: " + isRebuilding, this);
			Render();
		}

		void Update()
		{
			bool forceUpdate = false;

			#if UIFX_FILTERS_FORCE_UPDATE_PLAYMODE
			if (Application.isPlaying)
			{
				forceUpdate = true;
			}
			#endif
			#if UIFX_FILTERS_FORCE_UPDATE_EDITMODE
			if (!Application.isPlaying)
			{
				forceUpdate = true;
			}
			#endif
			
			if (forceUpdate)
			{
				SetAllDirty();
			}
		}

		protected static float GetResolutionScalingFactor(Canvas canvas)
		{
			float canvasScale = 1f;

			// TODO: only use  Canvas.scaleFactor in either screen/canvas space??
			//if (_renderSpace == FilterRenderSpace.Screen)
			{
				Debug.Assert(canvas != null);
				if (canvas != null)
				{
					canvasScale = canvas.scaleFactor;

#if UNITY_EDITOR
					// Handle cases where we're in "in-context" prefab mode, in this case Canvas.ScaleFactor is not set, but
					// scale needs to be derived from the transform
					if (canvasScale == 1f && canvas.worldCamera == null && canvas.renderMode == RenderMode.WorldSpace)
					{
						if (EditorHelper.IsInContextPrefabMode())
						{
							canvasScale = canvas.transform.localScale.x;
						}
					}
#endif
				}
			}

			return canvasScale;
		}

		protected override void UpdateMaterial()
		{

			base.UpdateMaterial();
		}

		private static Camera GetRenderCamera(Canvas canvas)
		{
			Camera camera = null;
			if (canvas)
			{
				camera = canvas.worldCamera;
				if (camera == null && canvas.renderMode == RenderMode.WorldSpace)
				{
					camera = Camera.main;

#if UNITY_EDITOR
					// NOTE: if we're in the "in-context" prefab editing mode, it uses a World Space canvas with no camera set.
					// when the original scene camera for the canvas was in Overlay mode, it would cause the filter to not render,
					// because the Camera.main camera would be used, and it wouldn't be looking at the UI component.  So instead
					// we detect this case and just use null camera as if it were in overlay mode.  Not sure how robust this is...
					if (EditorHelper.IsInContextPrefabMode())
					{
						camera = null;
					}
#endif
				}
				else if (camera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					camera = null;
				}
			}

			return camera;
		}
	}
}