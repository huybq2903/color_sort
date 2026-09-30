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
	/// <summary>
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(CanvasRenderer))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Sources/UIFX - Composite Layer", 150)]
	public partial class CompositeLayer : MaskableGraphic, ICompositeParent, IMaterialModifier//, IMeshModifier,
	{
		[SerializeField] Transform _overrideRoot = null;
		[SerializeField] Graphic[] _childGraphics = null;

		private static List<Graphic> _scratchGraphicList = new List<Graphic>(16);
		private Transform _childRoot;
		private List<ICompositeChild> _childList = new List<ICompositeChild>(8);

		internal List<ICompositeChild> ChildList { get => _childList; }

		public Transform Root { get => _overrideRoot; set { SetRoot(value, false); } }

		public override Texture mainTexture => _composite.GetTexture();

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void RegisterUsedShaders()
		{
			ShaderUsageRegistry.Register(nameof(CompositeLayer), "Composite Layer", ShaderUsageCategory.Sources, new[]
			{
				DefaultBlendShaderPath
			});
		}
		#endif
		
		private Transform GetOverrideRoot()
		{
			if (_overrideRoot != null)
			{
				return _overrideRoot;
			}
			return this.transform;
		}

		private void SetRoot(Transform newRoot, bool delayDestroy)
		{
			if (newRoot == null)
			{
				newRoot = this.transform;
			}
			if (newRoot != _childRoot)
			{
				ChangeRoot(newRoot, delayDestroy);
			}
		}

		private void ChangeRoot(Transform root, bool delayDestroy)
		{
			Debug.Assert(root != null);
			Debug.Assert(root != _childRoot);

			// First remove the previous children from this layer
			if (_childRoot != null && _childList != null)
			{
				foreach (var child in _childList)
				{
					child.SetCompositeParent(null);
					var component = child as Component;
					if (component)
					{
						ObjectHelper.Destroy(component, delayDestroy);
					}
				}
			}

			if (root != this.transform)
			{
				_overrideRoot = root;
			}

			BuildChildList();
		}

		public void BuildChildList()
		{
			_childList.Clear();
			_childRoot = GetOverrideRoot();
			_childRoot.GetComponentsInChildren<Graphic>(_scratchGraphicList);
			foreach (Graphic graphic in _scratchGraphicList)
			{
				// Skip components that are on this gameObject
				if (graphic.gameObject != this.gameObject)
				{
					AddChildToLayer(graphic);
				}
			}

			// Add children to serialised list so they are loaded/saved with scene.
			int childCount = _childList.Count;
			_childGraphics = new Graphic[childCount];
			for (int i = 0; i < childCount; i++)
			{
				_childGraphics[i] = _childList[i].GetTransform().gameObject.GetComponent<Graphic>();
			}
		}

#if UIFX_TMPRO
		private static readonly string TextMeshProGraphicTypeName = "TMPro.TextMeshProUGUI";
		private static readonly string TextMeshProSubMeshTypeName = "TMPro.TMP_SubMeshUI";
		private static readonly string FilterStackTextMeshProFullTypeName = "ChocDino.UIFX.FilterStackTextMeshPro, ChocDino.UIFX.TMP";
#endif

		private void AddChildToLayer(Graphic graphic)
		{
			var child = graphic.gameObject.GetComponent<ICompositeChild>();
			if (child == null)
			{
#if UIFX_TMPRO
				var graphicTypeName = graphic.GetType().ToString();
				if (graphicTypeName.Contains(TextMeshProGraphicTypeName))
				{
					var filterStackType = System.Type.GetType(FilterStackTextMeshProFullTypeName, false);
					if (filterStackType != null)
					{
						child = graphic.gameObject.AddComponent(filterStackType) as ICompositeChild;
					}
				}
				// Skip TMP submeshes
				else if (graphicTypeName.Contains(TextMeshProSubMeshTypeName))
				{
					return;
				}
#endif
				child = graphic.gameObject.AddComponent<CompositeLayerChild>();
			}
			child.SetCompositeParent(this);
			_childList.Add(child);
		}

		protected override void Awake()
		{
			this.useLegacyMeshGeneration = false;
			base.Awake();
		}

		void CreateDisplayMaterial()
		{
			if (_displayMaterial == null)
			{
				string shaderPath = DefaultBlendShaderPath;
				if (!string.IsNullOrEmpty(shaderPath))
				{
					var shader = Shader.Find(shaderPath);
					if (shader)
					{
						_displayMaterial = new Material(shader);
#if UNITY_EDITOR
						_displayMaterial.name = "Filter-DisplayMaterial";
#endif
						Debug.Assert(_displayMaterial != null);
					}
				}
			}
		}

		protected override void Start()
		{
			base.Start();
		}

		protected override void OnEnable()
		{
			_rectAdjustOptions.roundToNextMultiple = 128;
			BuildChildList();
			CreateDisplayMaterial();
			//Canvas.preWillRenderCanvases += OnPreWillRenderCanvas;
			Canvas.willRenderCanvases += OnWillRenderCanvas;
			MarkDirty(null, false);
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			{
				ObjectHelper.Destroy(ref _displayMaterial);
			}
			_composite.FreeResources();
			Canvas.willRenderCanvases -= OnWillRenderCanvas;
			//Canvas.preWillRenderCanvases -= OnPreWillRenderCanvas;
			//MarkDirty(null, false);
			foreach (var child in _childList)
			{
				if (child != null)
				{
					child.SetCompositeParent(null);
				}
			}
			//RenderChildren();
			SetVerticesDirty();
			base.OnDisable();
		}

		protected override void OnDestroy()
		{
			foreach (var child in _childList)
			{
				var component = child as Component;
				ObjectHelper.Destroy(component);
			}
			_childList = null;
			base.OnDestroy();
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			SetAllDirty();
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			if (this.isActiveAndEnabled)
			{
				if (GetOverrideRoot() != _childRoot)
				{
					// NOTE: Can't call DestroyImmediate() from OnValidate so set delayDestroy.
					ChangeRoot(GetOverrideRoot(), delayDestroy: true);
				}
				SetAllDirty();
			}
		}
#endif

		public void MarkDirty(ICompositeChild child, bool force)
		{
			if (_isRenderingChildren)
			{
				return;
			}

			//Debug.Log(Time.frameCount + " <color=blue>MarkDirty</color> " + this.name, this);
			_isRenderDirty = true;

			// We need to force OnPopulateMesh() to be called so that output geometry is generated.
			if (!CanvasUpdateRegistry.IsRebuildingGraphics())
			{
				SetVerticesDirty();
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				// NOTE: We have to call RecordObject() to force Unity to call Update() and OnPopulateMesh() etc in cases when
				// this GameObject is not selected. This can happen when a child is selected and being modified, the FilterLayer will not 
				// be updated.  This is only needed in edit mode. This was a problem with FilterStackTextMeshPro and toggling the filters on/off
				// (and also with FillGRadientFilter toggling the Shape enum),
				// as this component calls MarkDirty() from WillRenderCanvases() which happens after Update(), so we have to force it.
				if (force)
				{
					UnityEditor.Undo.RecordObject(this, string.Empty);
				}
				else
				{
					UnityEditor.Undo.RecordObject(this, string.Empty);
					//this.SetVerticesDirty();
					//UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
					UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
				}
			}
#endif
		}

		public bool IsFilterEnabled()
		{
			return this.isActiveAndEnabled;
		}
	}
}