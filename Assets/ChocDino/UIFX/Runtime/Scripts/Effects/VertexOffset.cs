//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// Apply a translation to the vertex positions without motifying the Transform.
	/// Useful for adding a second layer of motion to other effects/filters.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(Graphic))]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Effects/UIFX - Vertex Offset")]
	public partial class VertexOffset : UIBehaviour, IEffectStrength, IMeshModifier
	{
		[SerializeField] Vector3 _offset = Vector3.zero;

		[SerializeField, Range(0f, 1f)] float _strength = 1f;

		[SerializeField] Easing _easing;

		public Vector3 Offset { get { return _offset; } set { if (value != _offset) { _offset = value; ForceVerticesUpdate(); } } }

		public Easing Easing { get { return _easing; } set { if (value != _easing) { _easing = value; } } }

		#region IEffectStrength
		public float Strength { get { return _strength; } set { value = Mathf.Clamp01(value); if (value != _strength) { _strength = value; ForceVerticesUpdate(); } } }
		public bool IsEnabled { get => this.isActiveAndEnabled; }
		public void ForceUpdate() { ForceVerticesUpdate(); }
		#endregion

		private Graphic _graphic;
		private Graphic CachedGraphicComponent { get { if (_graphic == null) _graphic = GetComponent<Graphic>(); return _graphic; } }

		#if UNITY_EDITOR
		protected override void Reset()
		{
			ForceVerticesUpdate();
			base.Reset();
		}
		protected override void OnValidate()
		{
			ForceVerticesUpdate();
			base.OnValidate();
		}
		#endif

		protected override void OnDisable()
		{
			ForceVerticesUpdate();
			base.OnDisable();
		}

		protected override void OnEnable()
		{
			ForceVerticesUpdate();
			base.OnEnable();
		}

		protected override void OnDidApplyAnimationProperties()
		{
			if (isActiveAndEnabled)
			{
				ForceVerticesUpdate();
			}
			base.OnDidApplyAnimationProperties();
		}

		private void ForceVerticesUpdate()
		{
			var graphic = CachedGraphicComponent;
			graphic.SetVerticesDirty();
			graphic.SetMaterialDirty();
		}

		[UnityInternal.ExcludeFromDocs]
		public void ModifyMesh(VertexHelper vh)
		{
			if (!this.isActiveAndEnabled) return;
			if (_strength <= 0f) return;

			UIVertex v = UIVertex.simpleVert;
			int vertexCount = vh.currentVertCount;

			float t = _easing.Evalulate(_strength);
			Vector3 offset = Vector3.LerpUnclamped(Vector3.zero, _offset, t);

			for (int i = 0; i < vertexCount; i++)
			{
				vh.PopulateUIVertex(ref v, i);
				v.position = v.position + offset;
				vh.SetUIVertex(v, i);
			}
		}

		[UnityInternal.ExcludeFromDocs]
		[System.Obsolete("use IMeshModifier.ModifyMesh (VertexHelper verts) instead", false)]
		public void ModifyMesh(Mesh mesh)
		{
			throw new System.NotImplementedException("use IMeshModifier.ModifyMesh (VertexHelper verts) instead");
		}
	}

	public enum AnimTrigger
	{
		Script,
		OnEnable,
		OnStrength,
	}

	public enum AnimTimingMode
	{
		Speed,
		Duration,
	}

#if false
	// Animation logic
	public partial class VertexOffset
	{
		[SerializeField] AnimTrigger _animTrigger = AnimTrigger.Script;

		[SerializeField] AnimTimingMode _animTimingMode = AnimTimingMode.Duration;

		[SerializeField, Min(0f)] float _animSpeed = 0f;
		[SerializeField, Min(0f)] float _animDuration = 0f;

		[Tooltip("The time delta to use when updating animation.")]
		[SerializeField] TimeDeltaMode _animDeltaTime = TimeDeltaMode.Normal;

#if UNITY_EDITOR
		internal bool IsPreviewAnim { get; set; }
#endif

		private bool _isAnimating;
		private float _animTime;
		private float _animOffset;

		public void ResetAnim()
		{
			ForceVerticesUpdate();
		}

		public void TriggerAnim()
		{
			_isAnimating = true;
			_animTime = 0f;
		}

		public void Update()
		{
			if (_isAnimating)
			{
				float strengthSpeed;
				_strength += strengthSpeed;

				float deltaTime = 0f;
				if (_animDeltaTime == TimeDeltaMode.Normal)
				{
					deltaTime = Time.deltaTime;
				}
				else
				{
					if (Time.captureDeltaTime == 0f)
					{
						deltaTime = Time.unscaledDeltaTime;
					}
					else
					{
						deltaTime = Time.captureDeltaTime;
					}
				}

				if (_animTimingMode == AnimTimingMode.Duration)
				{
					_animTime = Mathf.Min(_animTime + deltaTime, _animDuration);
				}
				else
				{
					// units / second
					float totalDistance = Vector3.Magnitude(_offset);
					_animOffset += Mathf.Min(_animSpeed * deltaTime, totalDistance);
				}
			}
		}
	}
#endif
}