//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

//#define CLIP_TO_SCREEN	// TODO: fix this, it doesn't clip correctly when things are moving fast - suspect an out-by-one frame issue in the calculations.
#if UIFX_TMPRO
#define SKIP_ZERO_AREA_MESHES
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityInternal = UnityEngine.Internal;
using TMPro;

namespace ChocDino.UIFX
{
	/// <summary>
	/// The MotionBlurRealTMP component is a visual effect that can be applied to a TextMeshPro Text component
	/// to create an accurate motion blur effect when the UI components are in motion.
	/// </summary>
	/// <remark>
	/// </remark>
	//[ExecuteAlways]
	[RequireComponent(typeof(TMP_Text))]
	[HelpURL("https://www.chocdino.com/products/unity-assets/")]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Effects/UIFX - Motion Blur (Real) TMP")]
	public class MotionBlurRealTMP : UIBehaviour
	{
		[Tooltip("Which vertex modifiers are used to calculate the motion blur.")]
		[SerializeField] VertexModifierSource _mode = VertexModifierSource.Transform;

		[Tooltip("The number of motion blur steps to calculate.  The higher the number the more expensive the effect.  Set to 1 means the effect is not applied.")]
		[SerializeField, Range(1f, 64f)] int _sampleCount = 16;

		[Tooltip("The strength of the effect. Zero means the effect is not applied.  Greater than one means the effect is exagerated.")]
		[SerializeField, Range(0f, 4f)] float _strength = 1f;

		[Tooltip("If masking support is required then enable this, but it requires more memory.")]
		[SerializeField] bool _supportMasking = false;

		[Tooltip("The shader to use for the additive pass")]
		[SerializeField] Shader _shaderAdd = null;

		[Tooltip("The shader to use for the resolve pass")]
		[SerializeField] Shader _shaderResolve = null;

		class VertexData
		{
			public Vector3[] positions;
			public Vector2[] uvs0;
			public Vector2[] uvs1;
			public Color32[] colors;

			public VertexData(int vertexCount)
			{
				positions = new Vector3[vertexCount];
				// TODO: support newer TMP that uses Vector4 for UV
				uvs0 = new Vector2[vertexCount];
				uvs1 = new Vector2[vertexCount];
				colors = new Color32[vertexCount];
			}

			private VertexData() {}

			public int VertexCount { get { return positions.Length; } }

			public void CopyTo(VertexData dst)
			{
				Debug.Assert(positions.Length == dst.positions.Length);
				positions.CopyTo(dst.positions, 0);
				uvs0.CopyTo(dst.uvs0, 0);
				uvs1.CopyTo(dst.uvs1, 0);
				colors.CopyTo(dst.colors, 0);
			}

			public void CopyTo(VertexData dst, int dstOffset, int count)
			{
				Debug.Assert(positions.Length >= count);
				Debug.Assert(dst.positions.Length >= (dstOffset + count));
				System.Array.Copy(positions, 0, dst.positions, dstOffset, count);
				System.Array.Copy(uvs0, 0, dst.uvs0, dstOffset, count);
				System.Array.Copy(uvs1, 0, dst.uvs1, dstOffset, count);
				System.Array.Copy(colors, 0, dst.colors, dstOffset, count);
			}

			public void CopyTo(int srcOffset, VertexData dst, int dstOffset, int count)
			{
				Debug.Assert(positions.Length >= (srcOffset + count));
				Debug.Assert(dst.positions.Length >= (dstOffset + count));
				System.Array.Copy(positions, srcOffset, dst.positions, dstOffset, count);
				System.Array.Copy(uvs0, srcOffset, dst.uvs0, dstOffset, count);
				System.Array.Copy(uvs1, srcOffset, dst.uvs1, dstOffset, count);
				System.Array.Copy(colors, srcOffset, dst.colors, dstOffset, count);
			}
		}

		// Graphic geometry
		private bool _isPrimed;
		private int _graphicActiveVertexCount;
		private VertexData _graphicVerticesNow;
		private VertexData _graphicVerticesPast;
		private Rect _graphicWorldBoundsNow;
		private Rect _graphicWorldBoundsPast;
		private Matrix4x4 _localToWorldPast;

		// Blur geometry
		private int _blurVertexCount;
		private Vector3[] _blurVertexPositions;
		private Vector2[] _blurVertexUV0s;
		private Vector2[] _blurVertexUV1s;
		private Color[] _blurVertexColors;
		private int[] _blurVertexIndices;
		internal bool _isBlurredLastFrame;
		private Mesh _blurMesh;

		// Rendering params
		private float _screenWidth, _screenHeight;
		private int _textureWidth, _textureHeight;
		private float _worldHeight;
		private Vector3 _center;
		private Rect _clampedRect;
		private Matrix4x4 _screenToWorldPast;

		// Rendering
		private Material _materialAdd;
		internal Material _materialResolve;
		private CommandBuffer _cb;
		private RenderTexture[] _rts;
		private int _frameIndex;
		private RenderTexture RtRender { get { return _rts[_frameIndex % 2]; } set { _rts[_frameIndex % 2] = value; } }
		private RenderTexture RtDisplay { get { return _rts[(_frameIndex + 1) % 2]; } }

		private Graphic _graphic;
		private Graphic GraphicComponent { get { if (_graphic == null) _graphic = GetComponent<Graphic>(); return _graphic; } }
	
		private readonly static Color32 WhiteColor32 = new Color32(255, 255, 255, 255);
		private readonly static int PropTextureAddSample = Shader.PropertyToID("_TextureSampleAdd");
		private readonly static int PropMainTex2 = Shader.PropertyToID("_MainTex2");
		private readonly static int PropInvSampleCount = Shader.PropertyToID("_InvSampleCount");

		/// <summary>Property <c>UpdateMode</c> sets which vertex modifiers are used to calculate the motion blur</summary>
		/// <value>Set to <c>Mode.Transform</c> by default</value>
		public VertexModifierSource UpdateMode { get { return _mode; } set { _mode = value; ForceMeshModify(); } }

		/// <summary>Property <c>SampleCount</c> sets the number of motion blur steps to calculate.  The higher the number the more expensive the effect.</summary>
		/// <value>Set to 16 by default</value>
		public int SampleCount { get { return _sampleCount; } set { _sampleCount = value; ForceMeshModify(); } }

		/// <summary>Property <c>Blend</c> controls how large the motion blur effect is.</summary>
		/// <value>Set to 1.0 by default.  Zero means the effect is not applied.  Greater than one means the effect is exagerated.</value>
		public float Strength { get { return _strength; } set { _strength = value; ForceMeshModify(); } }

		/// <summary>Property <c>SupportMasking</c> can be enabled if masking support is required, however it requires more memory.</summary>
		/// <value>Set to <c>false</c> by default</value>
		public bool SupportMasking { get { return _supportMasking; } set { _supportMasking = value; ForceMeshModify(); } }

		private TMP_Text _textMeshPro;

		[UnityInternal.ExcludeFromDocs]
		protected override void Awake()
		{
			_textMeshPro = GetComponent<TMP_Text>();
			Debug.Assert(_textMeshPro != null);
			base.Awake();
		}

		void CreateComponents()
		{
			if (_materialAdd == null && _shaderAdd != null)
			{
				_materialAdd = new Material(_shaderAdd);
			}
			if (_materialResolve == null && _shaderResolve != null)
			{
				_materialResolve = new Material(_shaderResolve);
			}

			if (_blurMesh == null)
			{
				_blurMesh = new Mesh();
				_blurMesh.name = "MotionBlurReal";
				_blurMesh.MarkDynamic();
			}

			if (_cb == null)
			{
				_cb = new CommandBuffer();
				_cb.name = "MotionBlurReal";
			}

			_frameIndex = 0;
		}

		void DestroyComponents()
		{
			if (_cb != null)
			{
				_cb.Release(); _cb = null;
			}

			if (_rts != null)
			{
				for (int i = 0; i < _rts.Length; i++)
				{
					RenderTextureHelper.ReleaseTemporary(ref _rts[i]);
				}
				_rts = null;
			}

			ObjectHelper.Destroy(ref _blurMesh);
			ObjectHelper.Destroy(ref _materialAdd);
			ObjectHelper.Destroy(ref _materialResolve);
		}

		private static void DestroySafe<T>(ref T obj) where T : Object
		{
			if (obj)
			{
				if (Application.isPlaying) Material.Destroy(obj); else Object.DestroyImmediate(obj); obj = null;
			}
		}

		private void RenderMeshToTexture()
		{
			#if SKIP_ZERO_AREA_MESHES
			if (_clampedRect.width <= 0f || _clampedRect.height <= 0f)
			{
				return;
			}
			#endif

			int textureWidth = Mathf.NextPowerOfTwo(Mathf.CeilToInt(_clampedRect.width));
			int textureHeight = Mathf.NextPowerOfTwo(Mathf.CeilToInt(_clampedRect.height));
			textureWidth = Mathf.Min(textureWidth, 4096);
			textureHeight = Mathf.Min(textureHeight, 4096);
			//int textureWidth = _textureWidth;
			//int textureHeight = _textureHeight;

			if (textureWidth > 0 && textureHeight > 0)
			{
			}
			else
			{
				return;
			}

			if (_rts == null || _rts.Length == 0)
			{
				_rts = new RenderTexture[2];
			}

			if (RtRender)
			{
				if (textureWidth > 0 && textureHeight > 0 && (RtRender.width != textureWidth || RtRender.height != textureHeight))
				{
					RenderTexture.ReleaseTemporary(RtRender); RtRender = null;
				}
			}
			if (RtRender == null)
			{
				// NOTE: Using 24-bit depth buffer so that we get stencil buffer for mask support
				int depthBufferBits = _supportMasking ? 24 : 0;

				RtRender = RenderTexture.GetTemporary(textureWidth, textureHeight, depthBufferBits, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
			}

			if (_screenWidth <= 0)
			{
				Debug.LogError("Skipping rendering frame");
				return;
			}

			{
				// Copy material properties (and enabled keywords) for rendering
				_materialAdd.CopyPropertiesFromMaterial(_textMeshPro.fontSharedMaterial);
			}

			_cb.Clear();
			_cb.SetRenderTarget(RtRender);
			_cb.SetViewport(new Rect(0f, 0f, Mathf.Ceil(_clampedRect.width), Mathf.Ceil(_clampedRect.height)));
			_cb.ClearRenderTarget(false, true, Color.clear, 1f);
			float aspect = (_clampedRect.width / _clampedRect.height);

			float w = (_worldHeight * aspect) / 2f;
			float h = (_worldHeight) / 2f;
			_cb.SetProjectionMatrix(Matrix4x4.Ortho(-w, w, -h, h, 0.01f, 1000f));

			// Matrix that looks from camera's position, along the forward axis.
			var lookMatrix = Matrix4x4.LookAt(_center, _center + Vector3.forward, Vector3.up);
			// Matrix that mirrors along Z axis, to match the camera space convention.
			var scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
			// Final view matrix is inverse of the LookAt matrix, and then mirrored along Z.
			var viewMatrix = scaleMatrix * lookMatrix.inverse;

			_cb.SetViewMatrix(viewMatrix);
			_cb.DrawMesh(_blurMesh, Matrix4x4.TRS(new Vector3(0f, 0f, 10f), Quaternion.identity, Vector3.one), _materialAdd);
			Graphics.ExecuteCommandBuffer(_cb);
			_frameIndex++;

			//_materialAdd.SetVector("_ClipRect")
			//UNITY_UI_CLIP_RECT is defined, and _ClipRect
			//GraphicComponent.materialForRendering.CopyPropertiesFromMaterial(_materialAdd);
			_materialResolve.mainTexture = RtDisplay;
			_materialResolve.SetTexture(PropMainTex2, RtDisplay);
			_materialResolve.SetFloat(PropInvSampleCount, 1f / _sampleCount);
		}

		void OnGUI()
		{
			if (_rts != null && _rts.Length > 0)
			{
				if (_rts[0] != null)
				{
					GUI.DrawTexture(new Rect(0f, 0f, Screen.width / 2f, Screen.height / 2f), _rts[0], ScaleMode.StretchToFill);
				}

				if (_rts[1] != null)
				{
					GUI.DrawTexture(new Rect(Screen.width / 2f, 0f, Screen.width / 2f, Screen.height / 2f), _rts[1], ScaleMode.StretchToFill);
				}
			}
		}

		[UnityInternal.ExcludeFromDocs]
		protected override void Start()
		{
			Debug.Assert(_shaderAdd != null);
			Debug.Assert(_shaderResolve != null);
			CreateComponents();
			base.Start();
		}

		[UnityInternal.ExcludeFromDocs]
		protected override void OnEnable()
		{
			if (_textMeshPro != null)  { _textMeshPro = GetComponent<TMP_Text>(); Debug.Assert(_textMeshPro != null); }
			_textMeshPro.renderMode = TextRenderFlags.DontRender;

			_isPrimed = false;
			CreateComponents();
			GraphicComponent.RegisterDirtyVerticesCallback(OnDirtyVertices);
			#if UNITY_2019_4_OR_NEWER
			Canvas.preWillRenderCanvases += OnPreWillRenderCanvas;
			#endif
			Canvas.willRenderCanvases += OnWillRenderCanvas;
			ForceMeshModify();
			ForceMaterialModify();
			base.OnEnable();
		}

		[UnityInternal.ExcludeFromDocs]
		protected override void OnDisable()
		{
			if (_textMeshPro) _textMeshPro.renderMode = TextRenderFlags.Render;

			Canvas.willRenderCanvases -= OnWillRenderCanvas;
			#if UNITY_2019_4_OR_NEWER
			Canvas.preWillRenderCanvases -= OnPreWillRenderCanvas;
			#endif

			GraphicComponent.UnregisterDirtyVerticesCallback(OnDirtyVertices);
			DestroyComponents();
			ForceMeshModify();
			ForceMaterialModify();
			base.OnDisable();
		}

		#if UNITY_2019_4_OR_NEWER
		// Called before ModifyMesh() and ModifyMaterial()
		void OnPreWillRenderCanvas()
		{
			//Debug.Log(Graphic.defaultGraphicMaterial.shader.name + " "+ Graphic.defaultGraphicMaterial.color);
			//Debug.Log(this.name + " " + GraphicComponent.materialForRendering.shader.name + " " + GraphicComponent.materialForRendering.mainTexture);
			//Debug.Log(Time.frameCount.ToString() + " PreRender!");
		}
		#endif

		// Called after ModifyMesh() and ModifyMaterial()
		void OnWillRenderCanvas()
		{
			//Debug.Log(Graphic.defaultGraphicMaterial.shader.name + " "+ Graphic.defaultGraphicMaterial.color);
			//Debug.Log(this.name + " " + GraphicComponent.materialForRendering.shader.name + " " + GraphicComponent.materialForRendering.mainTexture);
			//Debug.Log(Time.frameCount.ToString() + " Render!");
			
			//CanvasRenderer cr = this.GetComponent<CanvasRenderer>();

			//Debug.Log(this.name + " " + cr.GetMaterial().shader.name + " " + cr.materialCount + " " + cr.GetMaterial().mainTexture);
			
			//cr.SetMesh(_mesh);
			//.GetMaterial()
		}

		#if UNITY_EDITOR
		protected override void OnValidate()
		{
			ForceMeshModify();
			base.OnValidate();
		}
		#endif

		private void ForceMeshModify()
		{
			GraphicComponent.SetVerticesDirty();
		}
		
		private void ForceMaterialModify()
		{
//			Debug.Log("dirty material");
			GraphicComponent.SetMaterialDirty();
		}

		private enum DirtySource : byte
		{
			None = 0,
			Transform = 0x01,
			Vertices = 0x02,
			SelfForced = 0x04,
		}

		private DirtySource _dirtySource = DirtySource.None;

		private bool IsDirtyTransform { get { return (_dirtySource & DirtySource.Transform) != 0; } set { _dirtySource |= DirtySource.Transform; } }
		private bool IsDirtyVertices { get { return (_dirtySource & DirtySource.Vertices) != 0; } set { _dirtySource |= DirtySource.Vertices; } }
		private bool IsDirtySelfForced { get { return (_dirtySource & DirtySource.SelfForced) != 0; } set { _dirtySource |= DirtySource.SelfForced; } }

		void OnDirtyVertices()
		{
			if (!IsDirtyTransform && !IsDirtySelfForced)
			{
				IsDirtyVertices = true;
			}
		}

		private void ForceMeshBackToOriginal()
		{
			// This sets the mesh back to the original state (without motion blur)
			// NOTE: We have to do this since we're modifying the size of the mesh, which TMP doesn't expect...Otherwise we get out of bounds errors for the triangle/vertex arrays.
			for (int i = 0; i < _textMeshPro.textInfo.materialCount; i++)
			{
				if (_textMeshPro.textInfo.meshInfo[i].vertexCount > 0)
				{
					_textMeshPro.textInfo.meshInfo[i].ResizeMeshInfo(_textMeshPro.textInfo.meshInfo[i].vertexCount / 4, false);
				}
			}
		}

		void ModifyGeometry(TMP_TextInfo textInfo)
		{
			if (_textMeshPro == null || !_textMeshPro.IsActive()) return;

			ModifyMesh2(null, textInfo);
		}

		void Update()
		{
			//this.SetVerticesDirty();
		}

		void LateUpdate()
		{
			if (MotionBlurReal.GlobalDebugFreeze)
			{
				ForceMaterialModify();
				return;
			}

			// Draw the previous frames mesh to the RenderTexture
			if (HasGeneratedMesh())
			{
				RenderMeshToTexture();
			}

			// If blur was applied to the last frame, then force a new frame to render in case there has been no motion
			// in which case it needs to be rendered without any motion blur.
			if (_isBlurredLastFrame)
			{
				IsDirtySelfForced = true;
				ForceMeshModify();
				ForceMaterialModify();
			}

			ForceMeshModify();
			ForceMaterialModify();
				
			// Detect changes to the transform
			if (IsTrackingTransform() && _localToWorldPast != this.transform.localToWorldMatrix)
			{
//				Debug.Log("force");
				IsDirtyTransform = true;
				ForceMeshModify();
			}
			else
			{
				//Debug.Log("NO MOVEMENT");
			}

			if (_textMeshPro)
			{
				if (CanApply())
				{
					IsDirtyTransform = true;
					//IsDirtyVertices = true;

					// Detected changes to vertex count or effective sample count
					if (_textMeshPro.havePropertiesChanged)
					{
						ForceMeshBackToOriginal();
					}
					/*else if (_vertices != null)
					{
						int textVertexCount = 0;
						for (int i = 0; i < _textMeshPro.textInfo.materialCount; i++)
						{
							textVertexCount += _textMeshPro.textInfo.meshInfo[i].vertexCount;
						}
						int outputVertexCount = textVertexCount * _sampleCount;
						if (outputVertexCount != _vertices.VertexCount)
						{
							ForceMeshBackToOriginal();
						}
					}*/

					// Force the text mesh to be regenerated
					_textMeshPro.renderMode = TextRenderFlags.DontRender;
					_textMeshPro.ForceMeshUpdate(false, false);

					// NOTE: We call this from LateUpdate() instead of from the OnPreRenderText action as otherwise
					// adjusting the number of triangles causes an error to be thrown, I think because TMP
					// doesn't call SetTriangles() on its main mesh before calling SetVertices().
					ModifyGeometry(_textMeshPro.textInfo);
				}
			}
		}

		internal bool HasGeneratedMesh()
		{
			return (_textureWidth > 0);
		}

		internal bool HasRendered()
		{
			return (_rts != null && _rts.Length > 0 && RtDisplay != null && _frameIndex > 0);
		}

		internal bool CanApply()
		{
			if (!IsActive()) return false;
			if (_sampleCount <= 1) return false;
			if (!_blurMesh) return false;
			return true;
		}

		[UnityInternal.ExcludeFromDocs]
		public void ModifyMesh(VertexHelper vh)
		{
		//	ModifyMesh2(vh, _textMeshPro.textInfo);
		}

		private void ModifyMesh2(VertexHelper vh, TMP_TextInfo textInfo)
		{
			// In freeze mode simply return the previous quad mesh
			if (MotionBlurReal.GlobalDebugFreeze && HasRendered() && _isBlurredLastFrame)
			{
				//GenerateQuad(vh, Camera.main);
				return;
			}

			_isBlurredLastFrame = false;

#if true
			if (CanApply())
			{
				//Debug.Log(Time.frameCount.ToString() + " ModifyMesh");
				//Debug.Log(Time.frameCount.ToString() + " MODMESH");

				bool isForcedLastFrame = (IsDirtySelfForced && !IsDirtyVertices && !IsDirtyTransform);
				_dirtySource = DirtySource.None;

				if (!isForcedLastFrame)
				{
					if (PrepareBuffers(textInfo))
					{
						/*{
							Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
							Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
							foreach (UIVertex vertex in _currVertices)
							{
								min = Vector3.Min(min, vertex.position);
								max = Vector3.Max(max, vertex.position);
							}
							Debug.Log("minmax: " + min + " " + max);
						}*/

						// Generate motion blur mesh vertices
						CreateMotionBlurMesh();
						GenerateRenderMetrics(Camera.main);

						// Copy motion blur vertices to a Mesh
						{
							_blurMesh.Clear();
							#if UNITY_2019_3_OR_NEWER
							_blurMesh.SetVertices(_blurVertexPositions);
							_blurMesh.SetUVs(0, _blurVertexUV0s);
							_blurMesh.SetUVs(1, _blurVertexUV1s);
							_blurMesh.SetColors(_blurVertexColors);
							#else
							_blurMesh.SetVertices(new List<Vector3>(_blurVertexPositions));
							_blurMesh.SetUVs(0, new List<Vector2>(_blurVertexUV0s));
							_blurMesh.SetUVs(1, new List<Vector2>(_blurVertexUV1s));
							_blurMesh.SetColors(new List<Color>(_blurVertexColors));
							#endif
							_blurMesh.SetTriangles(_blurVertexIndices, 0, calculateBounds:true);
						}

						Camera cameraMain = Camera.main;

						// Modify vertices to render a quad with the resulting texture
						if (HasRendered())
						{
							{
								//Debug.Log("GenerateQuad");
								//GenerateQuad(vh, cameraMain);
							}
							_isBlurredLastFrame = true;
						}
						else
						{
							//Debug.Log("!HasRendered");
						}

						
					}
					else
					{
						// This is the first frame of this component, so we can't generate any motion blur on this frame, 
						// so just collect the current state, ready to render motion blur on the next frame.
						//Debug.Log("FIRST");
					}
					CacheState();
				}
				else
				{
					//Debug.Log("FORCE");
					PrepareBuffers(textInfo);
					CacheState();
				}
			}
			else
			{
				//Debug.Log("!PRIMED");
				_isPrimed = false;
				//Debug.Log(Time.frameCount.ToString() + " SKIP");
			}
			#endif
		}

		internal void GenerateRenderMetrics(Camera camera)
		{
			// get world vertex bounds
			Vector3 min = _blurMesh.bounds.min;
			Vector3 max = _blurMesh.bounds.max;
			/*Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			foreach (UIVertex vertex in vertices)
			{
				min = Vector3.Min(min, vertex.position);
				max = Vector3.Max(max, vertex.position);
			}*/

			Vector3 center = (max + min) / 2f;
			//_center = center;

			//_worldHeight = (max - min).y;

			//Debug.Log("World: " + (max-min));

			// convert world bounds to screen space
			min = camera.WorldToScreenPoint(min);
			max = camera.WorldToScreenPoint(max);

			//min = camera.WorldToViewportPoint(min);
			//max = camera.WorldToViewportPoint(max);

			//min = RectTransformUtility.WorldToScreenPoint(camera, min);
			//max = RectTransformUtility.WorldToScreenPoint(camera, max);

			//Debug.Log("Screen: " + (max-min) + " " + min + " "+ max);

			Vector3 clampedMin = Vector3.Max(Vector2.zero, min);
			Vector3 clampedMax = Vector3.Min(new Vector2(Screen.width, Screen.height), max);
			//Debug.Log("Clamped: " + (clampedMax-clampedMin) + " " + clampedMin + " "+ clampedMax);

			#if (CLIP_TO_SCREEN)
			_clampedRect = Rect.MinMaxRect(clampedMin.x, clampedMin.y, clampedMax.x, clampedMax.y);
			#else
			_clampedRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
			#endif

			_center = camera.ScreenToWorldPoint(_clampedRect.center);
			//Debug.Log("Center1 " + _center);

			/*{
				Vector3 c = _clampedRect.center;
				c -= new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
				c.Scale(new Vector3(2f/Screen.width, 2f/Screen.height, 1f));
				c = _screenToWorldPast.MultiplyPoint(c);
				_center = c;
				Debug.Log("Center2 " + _center);
			}*/


			Vector3 minWorld = camera.ScreenToWorldPoint(new Vector3(_clampedRect.min.x, _clampedRect.min.y, 0f));
			Vector3 maxWorld = camera.ScreenToWorldPoint(new Vector3(_clampedRect.max.x, _clampedRect.max.y, 0f));
			_worldHeight = (maxWorld - minWorld).y;
			
			// Rect.MinMaxRect(min.x, min.y, max.x, max.y);
			//Debug.Log(min + " " + max);

			Vector2 size = max - min;
			_screenWidth = size.x;
			_screenHeight = size.y;

			_textureWidth = Mathf.NextPowerOfTwo(Mathf.CeilToInt(size.x));
			_textureHeight = Mathf.NextPowerOfTwo(Mathf.CeilToInt(size.y));

			/*{
				// get local vertex bounds
				Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
				foreach (UIVertex vertex in _currVertices)
				{
					min = Vector3.Min(min, vertex.position);
					max = Vector3.Max(max, vertex.position);
				}
				
				// convert bounds to screen space
				min = cameraMain.WorldToScreenPoint(min);
				max = cameraMain.WorldToScreenPoint(max);
				Vector2 size = max - min;
				

				// Prevent huge texture by limiting the size based on maxSize * the original mesh size
				float maxSize = 2f;
				int maxWidth = Mathf.NextPowerOfTwo(Mathf.CeilToInt(size.x * maxSize));
				int maxHeight = Mathf.NextPowerOfTwo(Mathf.CeilToInt(size.y * maxSize));
				_textureWidth = Mathf.Min(_textureWidth, maxWidth);
				_textureHeight = Mathf.Min(_textureHeight, maxHeight);
			}*/	
		}

		internal void GenerateQuad(VertexHelper vh, Camera camera)
		{
			Vector3 v0 = (new Vector2(_clampedRect.xMin / 1f, _clampedRect.yMax / 1f));
			Vector3 v1 = (new Vector2(_clampedRect.xMax / 1f, _clampedRect.yMax / 1f));
			Vector3 v2 = (new Vector2(_clampedRect.xMax / 1f, _clampedRect.yMin / 1f));
			Vector3 v3 = (new Vector2(_clampedRect.xMin / 1f, _clampedRect.yMin / 1f));

			if (true)
			{
				v0 = camera.ScreenToWorldPoint(v0);
				v1 = camera.ScreenToWorldPoint(v1);
				v2 = camera.ScreenToWorldPoint(v2);
				v3 = camera.ScreenToWorldPoint(v3);
			}
			/*else
			{
				v0 -= new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
				v1 -= new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
				v2 -= new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
				v3 -= new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

				v0.Scale(new Vector3(2f/Screen.width, 2f/Screen.height, 1f));
				v1.Scale(new Vector3(2f/Screen.width, 2f/Screen.height, 1f));
				v2.Scale(new Vector3(2f/Screen.width, 2f/Screen.height, 1f));
				v3.Scale(new Vector3(2f/Screen.width, 2f/Screen.height, 1f));
				v0 = _screenToWorldPast.MultiplyPoint(v0);
				v1 = _screenToWorldPast.MultiplyPoint(v1);
				v2 = _screenToWorldPast.MultiplyPoint(v2);
				v3 = _screenToWorldPast.MultiplyPoint(v3);
				_screenToWorldPast = (camera.projectionMatrix * camera.worldToCameraMatrix).inverse;
			}*/

			// Convert to local space
			Matrix4x4 prevWorldToLocal = _localToWorldPast.inverse;
			v0 = prevWorldToLocal.MultiplyPoint(v0);
			v1 = prevWorldToLocal.MultiplyPoint(v1);
			v2 = prevWorldToLocal.MultiplyPoint(v2);
			v3 = prevWorldToLocal.MultiplyPoint(v3);

			v3.z = v2.z = v1.z = v0.z = 0f;//vertices[0].position.z;

			float tx = 1f;
			float ty = 1f;
			{
				tx = _clampedRect.width / (float)RtDisplay.width;
				ty = _clampedRect.height / (float)RtDisplay.height;

				// Add half texel offset because without it there seems to be some rounding error where texels from previous render bounds are visible
				//tx -= 0.5f / (float)RtDisplay.width;
				//ty -= 0.5f / (float)RtDisplay.height;
			}

			// Display the last rendered texture (t+1)
			vh.Clear();
			vh.AddVert(v0, WhiteColor32, new Vector4(0f, ty, 0f, 0f));
			vh.AddVert(v1, WhiteColor32, new Vector4(tx, ty, 0f, 0f));
			vh.AddVert(v2, WhiteColor32, new Vector4(tx, 0f, 0f, 0f));
			vh.AddVert(v3, WhiteColor32, Vector4.zero);
			vh.AddTriangle(0, 1, 2);
			vh.AddTriangle(0, 2, 3);
		}

		/*[UnityInternal.ExcludeFromDocs]
		public Material GetModifiedMaterial(Material baseMaterial)
		{
			Debug.Log(Time.frameCount.ToString() + " ModifyMaterial " + baseMaterial.name, baseMaterial);

			if (!CanApply() || !HasRendered() || !_isBlurredLastFrame)
			{
			//	return baseMaterial;
			}

			//return baseMaterial;

			return _materialResolve;
		}*/

		/// <summary>
		/// Reset the motion blur to begin again at the current state (transform/vertex positions).
		/// This is useful when reseting the transform to prevent motion blur drawing erroneously between
		/// the last position and the new position.
		/// </summary>
		public void ResetMotion()
		{
			_isPrimed = false;
			_frameIndex = 0;
			//_blurredLastFrame = false;
			//_dirtySource = DirtySource.None;
		}

		private bool PrepareBuffers(TMP_TextInfo textInfo)
		{
			int totalVertexCount = 0;
			{
				for (int i = 0; i < textInfo.materialCount; i++)
				{
					TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
					totalVertexCount += meshInfo.vertexCount;
				}
			}

			int totalIndexCount = (totalVertexCount / 4) * 6;

			bool isPrepared = _isPrimed;
			if (_graphicVerticesNow == null || totalVertexCount != _graphicVerticesNow.VertexCount)
			{
				// If the number of vertices has changed, we need to prime
				// NOTE: We compare with the number of indices, as this is really how many vertices will be returned, as GetUIVertexStream() returns triangle indices
				_graphicVerticesNow = new VertexData(totalVertexCount);
				_graphicVerticesPast = new VertexData(totalVertexCount);
				_graphicActiveVertexCount = totalVertexCount;
				isPrepared = false;
			}
			/*else if (vh.currentIndexCount < _graphicVerticesNow.Count)
			{
				// If the number of vertices has decreased, then don't reallocate the list, just use a portion of it
				_graphicActiveVertexCount = vh.currentIndexCount;
				isPrepared = false;
			}*/

			// Copy graphic vertices
			int vertexOffset = 0;
			for (int i = 0; i < textInfo.materialCount; i++)
			{
				TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
				int vertexCount = meshInfo.vertexCount;
				// NOTE: that meshInfo.vertices (etc) can be larger than meshInfo.vertexCount because TMP over allocates (power-of-2 sizes) to prevent frequent reallocation.
				if (vertexCount > 0)
				{
					System.Array.Copy(meshInfo.vertices, 0, _graphicVerticesNow.positions, vertexOffset, vertexCount);
					System.Array.Copy(meshInfo.uvs0, 0, _graphicVerticesNow.uvs0, vertexOffset, vertexCount);
					System.Array.Copy(meshInfo.uvs2, 0, _graphicVerticesNow.uvs1, vertexOffset, vertexCount);
					System.Array.Copy(meshInfo.colors32, 0, _graphicVerticesNow.colors, vertexOffset, vertexCount);
					vertexOffset += vertexCount;
				}
			}

			int motionBlurVertexCount = _graphicActiveVertexCount * _sampleCount;

			if (_blurVertexPositions == null || motionBlurVertexCount != _blurVertexPositions.Length)
			{
				_blurVertexPositions = new Vector3[motionBlurVertexCount];
				_blurVertexUV0s = new Vector2[motionBlurVertexCount];
				_blurVertexUV1s = new Vector2[motionBlurVertexCount];
				_blurVertexColors = new Color[motionBlurVertexCount];
				int motionBlurIndexCount = totalIndexCount * _sampleCount;
				_blurVertexIndices = new int[motionBlurIndexCount];
				for (int i = 0; i < motionBlurIndexCount; i+=6)
				{
					int j = (i * 4) / 6;
					_blurVertexIndices[i+0] = j + 0;
					_blurVertexIndices[i+1] = j + 1;
					_blurVertexIndices[i+2] = j + 2;
					_blurVertexIndices[i+3] = j + 2;
					_blurVertexIndices[i+4] = j + 0;
					_blurVertexIndices[i+5] = j + 3;
				}
				_blurVertexCount = motionBlurVertexCount;
				isPrepared = false;
			}

			// If the samplecount has been decreased, then we need to invalidate the
			// higher up triangle indices so the geometry doesn't render.
			/*if (motionBlurVertexCount < _blurVertexCount)
			{
				for (int i = motionBlurVertexCount; i < _blurVertexCount; i++)
				{
					_blurVertexIndices[i] = 0;
				}
			}
			// If the sampleCount has increased, regenerate the triangles
			else if (motionBlurVertexCount > _blurVertexCount)
			{
				for (int i = _blurVertexCount; i < motionBlurVertexCount; i++)
				{
					_blurVertexIndices[i] = i;
				}
			}
			_blurVertexCount = motionBlurVertexCount;*/

			return isPrepared;
		}

		private bool IsTrackingTransform()
		{
			return (_mode != VertexModifierSource.Vertex);
		}

		private bool IsTrackingVertices()
		{
			return (_mode != VertexModifierSource.Transform);
		}

		private void CacheState()
		{
			if (IsTrackingTransform())
			{
				_localToWorldPast = this.transform.localToWorldMatrix;
			}
			if (IsTrackingVertices())
			{
				_graphicVerticesNow.CopyTo(0, _graphicVerticesPast, 0, _graphicActiveVertexCount);
			}

			_graphicWorldBoundsPast = _graphicWorldBoundsNow;

			_isPrimed = true;
		}

		private void CreateMotionBlurMesh()
		{
			float stepSize = 1f / (_sampleCount - 1f);
			float t = 0f;

			Matrix4x4 localToWorld = this.transform.localToWorldMatrix;
			Matrix4x4 worldToLocal = this.transform.worldToLocalMatrix;
			
			//_vtxVertices.Clear();
			//_vtxUV0.Clear();
			//_vtxColors.Clear();

    		/*Canvas copyOfMainCanvas = GameObject.Find("Canvas").GetComponent <Canvas>();
    			float scaleFactor = copyOfMainCanvas.scaleFactor;
				Vector2 displaySize = copyOfMainCanvas.renderingDisplaySize;

			Debug.Log("mtx " + worldToLocal + " " + localToWorld + " " + scaleFactor + " " + displaySize + " " + copyOfMainCanvas.referencePixelsPerUnit);

			var canvasRect = copyOfMainCanvas.GetComponent<RectTransform>();
       		 var scale = canvasRect.sizeDelta;
			 Debug.Log("SCALE: " + scale);*/

			if (_mode == VertexModifierSource.Transform)
			{
				int idx = 0;
				for (int j = 0; j < _sampleCount; j++)
				{
					//float tt = ((t - 0.5f) * _strength) + 0.5f;
					//float tt = (t * _strength);
					float tt = ((t - 1.0f) * _strength) + 1.0f;
					float ttclamped = Mathf.Clamp01(tt);
					for (int i = 0; i < _graphicActiveVertexCount; i++)
					{
						Vector3 pos = _graphicVerticesNow.positions[i];

						// Xform the previous position to world space
						Vector3 v0 = _localToWorldPast.MultiplyPoint3x4(pos);
						// Xform the current position to world space
						Vector3 v1 = localToWorld.MultiplyPoint3x4(pos);

						_blurVertexPositions[idx] = Vector3.LerpUnclamped(v0, v1, tt);
						_blurVertexUV0s[idx] = _graphicVerticesNow.uvs0[i];
						_blurVertexUV1s[idx] = _graphicVerticesNow.uvs1[i];
						_blurVertexColors[idx] = _graphicVerticesNow.colors[i];
						idx++;
					}
					t += stepSize;
				}
			}
			else if (_mode == VertexModifierSource.Vertex)
			{
				int idx = 0;
				for (int j = 0; j < _sampleCount; j++)
				{
					float tt = ((t - 0.5f) * _strength) + 0.5f;
					float ttclamped = Mathf.Clamp01(tt);
					for (int i = 0; i < _graphicActiveVertexCount; i++)
					{
						Vector3 pos = _graphicVerticesNow.positions[i];

						pos = Vector3.LerpUnclamped(_graphicVerticesPast.positions[i], pos, tt);
						// Xform the current position to world space
						_blurVertexPositions[idx] = localToWorld.MultiplyPoint3x4(pos); 

						_blurVertexUV0s[idx] = Vector4.LerpUnclamped(_graphicVerticesPast.uvs0[i], _graphicVerticesNow.uvs0[i], ttclamped);
						_blurVertexUV1s[idx] = Vector4.LerpUnclamped(_graphicVerticesPast.uvs1[i], _graphicVerticesNow.uvs1[i], ttclamped);
						_blurVertexColors[idx] = Color.LerpUnclamped(_graphicVerticesPast.colors[i], _graphicVerticesNow.colors[i], ttclamped);
						idx++;
					}
					t += stepSize;
				}	
			}
			else if (_mode == VertexModifierSource.TranformAndVertex)
			{
				int idx = 0;
				for (int j = 0; j < _sampleCount; j++)
				{
					float tt = ((t - 0.5f) * _strength) + 0.5f;
					float ttclamped = Mathf.Clamp01(tt);
					for (int i = 0; i < _graphicActiveVertexCount; i++)
					{
						Vector3 pos = _graphicVerticesNow.positions[i];

						// Xform the previous position to world space
						Vector3 v0 = _localToWorldPast.MultiplyPoint3x4(_graphicVerticesPast.positions[i]);
						// Xform the current position to world space
						Vector3 v1 = localToWorld.MultiplyPoint3x4(pos); 

						_blurVertexPositions[idx]  = Vector3.LerpUnclamped(v0, v1, tt);
						_blurVertexUV0s[idx] = Vector4.LerpUnclamped(_graphicVerticesPast.uvs0[i], _graphicVerticesNow.uvs0[i], ttclamped);
						_blurVertexUV1s[idx] = Vector4.LerpUnclamped(_graphicVerticesPast.uvs1[i], _graphicVerticesNow.uvs1[i], ttclamped);
						_blurVertexColors[idx] = Color.LerpUnclamped(_graphicVerticesPast.colors[i], _graphicVerticesNow.colors[i], ttclamped);

						idx++;
					}
					t += stepSize;
				}
			}

			if (MotionBlurReal.GlobalDebugTint)
			{
				for (int i = 0; i < _blurVertexColors.Length; i++)
				{
					_blurVertexColors[i] = Color.magenta;
				}
			}
		}
	}
}
#endif