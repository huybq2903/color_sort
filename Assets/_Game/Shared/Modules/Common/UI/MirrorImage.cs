using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.Common
{
    [RequireComponent(typeof(Graphic))]
    public class MirrorImage : BaseMeshEffect
    {
        public enum MirrorAxis { Horizontal, Vertical }

        [SerializeField] private MirrorAxis axis = MirrorAxis.Horizontal;
        [SerializeField] private Vector2 pivot = new(0.5f, 0.5f);

        public MirrorAxis Axis
        {
            get => axis;
            set { axis = value; graphic.SetVerticesDirty(); }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled) return;

            var originalCount = vh.currentVertCount;
            if (originalCount == 0) return;

            var verts = new List<UIVertex>();
            for (int i = 0; i < originalCount; i++)
            {
                var v = new UIVertex();
                vh.PopulateUIVertex(ref v, i);
                verts.Add(v);
            }

            var rect = graphic.rectTransform.rect;
            float pivotLocalX = rect.xMin + pivot.x * rect.width;
            float pivotLocalY = rect.yMin + pivot.y * rect.height;
            float offsetX = rect.center.x - pivotLocalX;
            float offsetY = rect.center.y - pivotLocalY;

            // Shift original verts so midpoint between original & mirror lands at rect center
            for (int i = 0; i < originalCount; i++)
            {
                var v = verts[i];
                v.position.x += offsetX;
                v.position.y += offsetY;
                vh.SetUIVertex(v, i);
                verts[i] = v;
            }

            foreach (var original in verts)
            {
                var mirrored = original;
                if (axis == MirrorAxis.Horizontal)
                    mirrored.position.x = 2 * (pivotLocalX + offsetX) - original.position.x;
                else
                    mirrored.position.y = 2 * (pivotLocalY + offsetY) - original.position.y;
                vh.AddVert(mirrored);
            }

            int triangleCount = (originalCount / 4) * 2;
            for (int quad = 0; quad < triangleCount / 2; quad++)
            {
                int baseOriginal = quad * 4;
                int baseMirrored = originalCount + quad * 4;

                if (axis == MirrorAxis.Horizontal)
                {
                    vh.AddTriangle(baseMirrored + 0, baseMirrored + 3, baseMirrored + 2);
                    vh.AddTriangle(baseMirrored + 2, baseMirrored + 1, baseMirrored + 0);
                }
                else
                {
                    vh.AddTriangle(baseMirrored + 0, baseMirrored + 3, baseMirrored + 2);
                    vh.AddTriangle(baseMirrored + 2, baseMirrored + 1, baseMirrored + 0);
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            graphic?.SetVerticesDirty();
        }
#endif
    }
}
