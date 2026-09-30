//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public interface ICompositeParent
	{
		void MarkDirty(ICompositeChild child, bool force);
		bool IsFilterEnabled();
	}

	public interface ICompositeChild
	{
		void SetCompositeParent(ICompositeParent parent);
		bool IsRenderable();
		void UpdateMeshMaterial();
		Material GetMaterial();
		Mesh GetMesh();
		Transform GetTransform();
	}
}