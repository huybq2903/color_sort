//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public interface IFilterParent
	{
		void MarkDirty(IFilterChild gather);
		bool IsFilterEnabled();
	}

	public interface IFilterChild
	{
		void SetFilterParent(IFilterParent render);
		bool IsRenderable();
		Material GetMaterial();
		Mesh GetMesh();
		Transform GetTransform();
	}
}