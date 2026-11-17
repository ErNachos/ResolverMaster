using UnityEngine;
using UnityEngine.UI;

namespace ResolverMaster.Core.UI
{
    /// <summary>Moves the drawn mesh of a uGUI graphic without touching its RectTransform (a pressed button goes down inside layouts too).</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public sealed class VertexOffset : BaseMeshEffect
    {
        Vector2 _offset;

        public Vector2 Offset
        {
            get => _offset;
            set
            {
                if (_offset == value) return;
                _offset = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || _offset == Vector2.zero) return;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position += (Vector3)_offset;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
