using UnityEngine;

namespace Nexus
{
    /// <summary>Slowly scrolls the base map of this renderer's material (drifting clouds).</summary>
    public class UvScroll : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0.004f, 0.0015f);
        Material mat;
        Vector2 offset;

        void Start()
        {
            var r = GetComponent<Renderer>();
            if (r != null) mat = r.material;   // own instance, so other users of the material don't scroll
        }

        void Update()
        {
            if (mat == null) return;
            offset += speed * Time.deltaTime;
            offset.x %= 1f;
            offset.y %= 1f;
            mat.SetTextureOffset("_BaseMap", offset);
        }
    }
}
