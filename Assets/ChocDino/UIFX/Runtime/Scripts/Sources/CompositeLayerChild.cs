//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ChocDino.UIFX
{
	/// <summary>
	/// CompositeChild must be added to the GameObject after all FilterBase components.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(CanvasRenderer), typeof(Graphic))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Sources/UIFX - Composite Layer Child", 151)]
	public class CompositeLayerChild : UIBehaviour, ICompositeChild, IMaterialModifier, IMeshModifier
	{
		[SerializeField, HideInInspector] GameObject _compositeParentGo = null;

		private readonly static Vector4 Alpha8TextureAdd = new Vector4(1f, 1f, 1f, 0f);

		private Graphic _graphic;
		private Graphic GraphicComponent { get { if (_graphic == null) _graphic = GetComponent<Graphic>(); return _graphic; } }

		private CanvasRenderer _canvasRenderer;
		private CanvasRenderer CanvasRenderComponent { get { if (!_canvasRenderer) { if (GraphicComponent) { _canvasRenderer = _graphic.canvasRenderer; } else { _canvasRenderer = GetComponent<CanvasRenderer>(); } } return _canvasRenderer; } }

		private Material _material;
		private Mesh _mesh;
		private List<Color> _vertexColors;

		private Matrix4x4 _previousLocalToWorldMatrix;
		private bool _previousRenderable;
		private Color _previousColor = Color.white;

		private ICompositeParent _compositeParent;

		void Update()
		{
			if (Debug.isDebugBuild)
			{
				CheckIsComponentOrderCorrect();
			}

			// Detect a change to the matrix (this also detects changes to the camera and viewport)
			if (MathUtils.HasMatrixChanged(_previousLocalToWorldMatrix, this.transform.localToWorldMatrix, ignoreTranslation: false))
			{
				_previousLocalToWorldMatrix = this.transform.localToWorldMatrix;
				MarkParentRenderDirty();
			}

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

			bool isRenderable = IsRenderable();
			if (isRenderable)
			{ 
				Color color = this.CanvasRenderComponent.GetColor();
				if (_previousColor != color)
				{
					forceUpdate = true;
				}
			}

			if (_previousRenderable != isRenderable)
			{
				if (!isRenderable)
				{
					// If the component is disabled, dirtying the component will have not effect, so
					// just tell the parent directly.
					MarkParentRenderDirty();
				}
				else
				{
					forceUpdate = true;
				}
				_previousRenderable = isRenderable;
			}

			if (forceUpdate)
			{
				GraphicComponent.SetAllDirty();
			}
		}

		internal bool CheckIsComponentOrderCorrect()
		{
			// NOTE: Technically it only needs to be the last IMeshModifier and IMaterialModifier component,
			// but this is a simpler check and will do for now.
			if (!IsLastUIComponentOnGameObject(this))
			{
				// TODO: put this warning in the Inspector too
				Debug.LogWarning(string.Format("[UIFX] CompositeLayerChild is not the last UI component on GameObject '{0}' and this can cause incorrect rendering.", this.gameObject.name), this);
				return false;
			}
			return true;
		}

		private static List<Component> _componentsCache = new List<Component>(4);

		internal static bool IsLastUIComponentOnGameObject(Component component)
		{
			// TODO: For later versions of Unity use GameObject.GetComponentCount() and GameObject.GetComponentAtIndex()
			component.gameObject.GetComponents(_componentsCache);
			// Sanity check, these must be at least some components
			Debug.Assert(_componentsCache != null);
			Debug.Assert(_componentsCache.Count > 0);
			// From back to front, search components to make sure this component is found BEFORE
			// any FilterBase, IMeshModifier or IMaterialModifier components.
			for (int i = _componentsCache.Count - 1; i > 0; i--)
			{
				var c = _componentsCache[i];
				if (c == component)
				{
					return true;
				}
				else if (c is FilterBase || c is IMeshModifier || c is IMaterialModifier)
				{
					return false;
				}
			}
			// In theory, should never get here.
			Debug.Assert(false);
			return false;
		}

		protected override void Start()
		{
			//Debug.Log("start " + this.name + " " + this.enabled);
			if (_compositeParent == null && _compositeParentGo != null)
			{
				_compositeParent = _compositeParentGo.GetComponent<CompositeLayer>();
				//Debug.Log("start _compositeParent " + (_compositeParent == null));
				MarkParentRenderDirty();
			}
			base.Start();
		}

		protected override void OnEnable()
		{
			//Debug.Log("OnEnable " + this + " " + (_compositeParent == null), this);
			//Debug.Log("OnEnable " + this.name, this);
			_previousRenderable = false;
			GraphicComponent.RegisterDirtyVerticesCallback(OnDirtyVertices);
			GraphicComponent.RegisterDirtyMaterialCallback(OnDirtyVertices);
			GraphicComponent.RegisterDirtyLayoutCallback(OnDirtyVertices);
			Canvas.willRenderCanvases += OnWillRenderCanvas;
			MarkParentRenderDirty();
			GraphicComponent.SetAllDirty();
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			Canvas.willRenderCanvases -= OnWillRenderCanvas;
			GraphicComponent.UnregisterDirtyLayoutCallback(OnDirtyVertices);
			GraphicComponent.UnregisterDirtyMaterialCallback(OnDirtyVertices);
			GraphicComponent.UnregisterDirtyVerticesCallback(OnDirtyVertices);
			MarkParentRenderDirty();
			GraphicComponent.canvasRenderer.cull = false;
			GraphicComponent.SetAllDirty();
			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			//Debug.Log("destroy1 " + this, this);
			//Debug.Log("destroy2 " + this.name, this);
			ObjectHelper.Destroy(ref _mesh);
			ObjectHelper.Destroy(ref _material);
			base.OnDestroy();
		}

		void OnDirtyVertices()
		{
			MarkParentRenderDirty();
		}

		void OnWillRenderCanvas()
		{
			if (!IsDestroyed())
			{
//				Debug.Log(Time.frameCount + " OnWillRenderCanvas " + this.name, this);
			}
		}

		void MarkParentRenderDirty()
		{
			if (_compositeParent != null)
			{
				_compositeParent.MarkDirty(this, false);
			}
		}

		#region ICompositeChild

		public void SetCompositeParent(ICompositeParent parent)
		{
			if (!this.IsDestroyed())
			{
				if (parent != _compositeParent)
				{
					_compositeParent = parent;
					if (_compositeParent != null)
					{
						_compositeParentGo = (parent as Component).gameObject;
						this.enabled = true;
					}
					else
					{
						this.enabled = false;
					}
				}
			}
		}

		/// <summary>
		/// Returns whether this child layer needs to be rendered by the parent, otherwise it's considered invisible.
		/// </summary>
		public bool IsRenderable()
		{
			if (!this.IsDestroyed() && this.isActiveAndEnabled && this.GraphicComponent.enabled)
			{
				return true;
				//return (GetMaterial() != null && GetMesh() != null);
			}
			return false;
		}

		private Material _tempMaterial;

		public void UpdateMeshMaterial()
		{
			Debug.Assert(IsRenderable());

			if (!this.IsDestroyed())
			{

				//Debug.Log("UpdateMeshMaterial()1" + GraphicComponent.canvasRenderer.cull);

				{
					GraphicComponent.canvasRenderer.cull = false;
					// NOTE: We have to SetVerticesDirty() otherwise Rebuild won't work, but SetverticesDirty() also registers the component with
					// CanvasUpdateRegistry so we have to unregister it after the Rebuild().
					if (!CanvasUpdateRegistry.IsRebuildingGraphics())
					{
						//Debug.Log("UpdateMeshMaterial()2 ");
						GraphicComponent.SetVerticesDirty();
						GraphicComponent.Rebuild(CanvasUpdate.PreRender);
						CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(GraphicComponent);
					}
					else
					{
						//Debug.Log("UpdateMeshMaterial()3 ");
						GraphicComponent.Rebuild(CanvasUpdate.PreRender);
					}
					//GraphicComponent.canvasRenderer.cull = _compositeParent.IsFilterEnabled();

					// NOTE: This forces the GetModifiedMaterial() list to be called, effectivly updating the materials. 
					_tempMaterial = GraphicComponent.materialForRendering;
				}

				// By the end of this method material and mesh should both be valid.
				//Debug.Log(_material + " " + _mesh, this);
				Debug.Assert(_material != null);
				Debug.Assert(_mesh != null);
			}
		}

		public Material GetMaterial()
		{
			Debug.Assert(!this.IsDestroyed());
			return _material;
		}
		public Mesh GetMesh()
		{
			Debug.Assert(!this.IsDestroyed());
			return _mesh;
		}

		public Transform GetTransform()
		{
			Debug.Assert(!this.IsDestroyed());
			return this.transform;
		}

		#endregion

		#region IMeshModifier

		public void ModifyMesh(VertexHelper verts)
		{
			if (_compositeParent == null)
			{
				return;
			}
			if (!_compositeParent.IsFilterEnabled())
			{
				CanvasRenderComponent.cull = false;
				return;
			}

			//Debug.Log(Time.frameCount + " <color=magenta>GrabMesh</color> " + this.name, this);
			if (_mesh == null)
			{
				_mesh = new Mesh();
#if UNITY_EDITOR
				_mesh.name = "CompositeLayerChild-" + this.gameObject.name;
#endif
				_mesh.hideFlags = HideFlags.HideAndDontSave;
			}
			verts.FillMesh(_mesh);
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				ColorUtils.ConvertMeshVertexColorsToLinear(_mesh, ref _vertexColors);

				Color color = CanvasRenderComponent.GetColor();
				for (int i = 0; i < _vertexColors.Count; i++)
				{
					_vertexColors[i] *= color;
					_mesh.SetColors(_vertexColors);
				}
				_previousColor = color;

			}
			verts.Clear();

			//CanvasRenderComponent.cull = true;

			//MarkParentRenderDirty();
		}

		public void ModifyMesh(Mesh mesh)
		{
			throw new System.NotImplementedException();
		}

#endregion

#region IMaterialModifier

		public Material GetModifiedMaterial(Material baseMaterial)
		{
			if (_compositeParent == null)
			{
				return baseMaterial;
			}
			if (!_compositeParent.IsFilterEnabled())
			{
				return baseMaterial;
			}

			//Debug.Log(Time.frameCount + " GetModifiedMaterial " + this.name, this);
			if (_material == null || _material.shader != baseMaterial.shader)
			{
				ObjectHelper.Destroy(ref _material);
				_material = new Material(baseMaterial);
			}

			if (_material)
			{
				_material.CopyPropertiesFromMaterial(baseMaterial);
			}
			//if (!_material || _material.mainTexture == null)
			{
				SetupMaterial(baseMaterial);
			}

			//MarkParentRenderDirty();

			return baseMaterial;
		}

#endregion

		private void SetupMaterial(Material baseMaterial)
		{
			if (!_material)
			{
				//if (GraphicComponent.material != null)
				{
					_material = new Material(baseMaterial);
				}
			}
			if (_material)
			{
				
				// Copy material properties (and enabled keywords) for rendering
				_material.CopyPropertiesFromMaterial(baseMaterial);

				_material.mainTexture = GraphicComponent.mainTexture;
				if (_material.mainTexture)
				{
					if (_material.mainTexture is Texture2D && ((Texture2D)_material.mainTexture).format == TextureFormat.Alpha8)
					{
						_material.SetVector(UnityShaderProp.TextureAddSample, Alpha8TextureAdd);
					}
					else
					{
						_material.SetVector(UnityShaderProp.TextureAddSample, Vector4.zero);
					}
				}
			}
		}
	}
}