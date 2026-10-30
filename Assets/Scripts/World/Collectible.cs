using UnityEngine;

namespace Nexus
{
    /// <summary>Energy Orb or Nexus Core. Collected by LevelBuilder when the player enters its cell.</summary>
    public class Collectible : MonoBehaviour
    {
        public enum Kind { Orb, NexusCore }
        public Kind kind;

        Transform visual;
        float phase;

        void Start()
        {
            visual = transform.Find("Visual");
            phase = Random.value * 10f;
        }

        void Update()
        {
            if (visual == null) return;
            float t = Time.time + phase;
            var p = visual.localPosition;
            p.y = (kind == Kind.Orb ? 0.45f : 0.6f) + Mathf.Sin(t * 2.5f) * (kind == Kind.Orb ? 0.04f : 0.08f);
            visual.localPosition = p;
            if (kind == Kind.NexusCore) visual.Rotate(0f, 90f * Time.deltaTime, 0f);
        }

        public void Collect()
        {
            if (GameManager.Instance != null)
            {
                if (kind == Kind.Orb) GameManager.Instance.CollectOrb();
                else GameManager.Instance.CollectCore();
            }
            Destroy(gameObject);
        }
    }
}
