//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	[System.Serializable]
	public abstract class WipeBase
	{
		[SerializeField, HideInInspector] Shader _shader;
		internal Shader Shader { get { if (_shader == null) _shader = Shader.Find(GetShaderPath()); return _shader; } set => _shader = value; }

		internal event System.Action OnValuesChanged;

		protected abstract string GetShaderPath();

		public virtual void CopyFrom(WipeBase source)
		{
			Debug.Assert(this.Shader == source.Shader);
		}

		protected void SignalDirty()
		{
			OnValuesChanged?.Invoke();
		}

		internal virtual bool IsExpandable { get => false; }

		internal virtual void GetFilterAdjustSize(FilterBase filter, ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
		}

		internal virtual void Apply(Material material, float resolutionFactor)
		{
		}
	}
}