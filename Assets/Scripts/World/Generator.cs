using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus
{
    /// <summary>
    /// Exit generator. Red while locked, cyan after all Nexus Cores are collected.
    /// Player stands on it and presses E to complete the level.
    /// </summary>
    public class Generator : MonoBehaviour
    {
        public Vector2Int cell;
        public Light glow;

        bool active;

        void Start()
        {
            if (glow == null)
            {
                glow = new GameObject("Glow").AddComponent<Light>();
                glow.transform.SetParent(transform, false);
                glow.transform.localPosition = new Vector3(0, 1.2f, 0);
                glow.type = LightType.Point;
                glow.range = 4f;
                glow.intensity = 3f;
            }
            glow.color = Visuals.CorruptedRed;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (!active && gm.AllCoresCollected) Activate();

            transform.Rotate(0f, (active ? 120f : 20f) * Time.deltaTime, 0f);

            if (active && PlayerIsHere && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                gm.CompleteLevel();
        }

        public bool PlayerIsHere =>
            PlayerMovement.Instance != null && PlayerMovement.Instance.CurrentCell == cell;

        public bool IsActive => active;

        void Activate()
        {
            active = true;
            glow.color = Visuals.Cyan;
            foreach (var part in new[] { "Core", "Beam" })
            {
                var t = transform.Find(part);
                if (t != null) Visuals.SetMaterial(t.gameObject, Visuals.GetMaterial(Visuals.Cyan, Visuals.Cyan, 3f));
            }
        }
    }
}
