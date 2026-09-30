//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_TMPRO
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// This is a utility class used by MotionBlurRealTMP to draw a quad to display the motion blurred TMP
	/// We're doing this instead of modifying the geometry+material used by TMP as this is simpler.
	/// This component is a child of the GameObject with the TMP, and is usually hidden from the user
	/// </summary>
	[RequireComponent(typeof(Transform))]
	internal class MotionBlurRealTMP_Draw : Graphic, IMaterialModifier
	{
		public MotionBlurRealTMP _parentMotionBlur = null;

		void LateUpdate()
		{
			SetMaterialDirty();
			SetVerticesDirty();
		}


		[UnityInternal.ExcludeFromDocs]
		public Material GetModifiedMaterial(Material baseMaterial)
		{
			if (!_parentMotionBlur.CanApply() || !_parentMotionBlur.HasRendered() || !_parentMotionBlur._isBlurredLastFrame)
			{
				Debug.Log(Time.frameCount + " base");
				return baseMaterial;
			}

			//return baseMaterial;
			Debug.Log(Time.frameCount + " replace");
			return _parentMotionBlur._materialResolve;
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			if (_parentMotionBlur.HasRendered())
			{
				_parentMotionBlur.GenerateQuad(vh, Camera.main);
				_parentMotionBlur.GenerateRenderMetrics(Camera.main);
			}
		}
	}
}
#endif