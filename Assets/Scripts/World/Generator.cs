using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Exit generator on the 'G' cell. Red and inactive until Pac-Man reaches it with all Nexus Cores.
    /// - Reached with all cores: turns cyan, spins fast and the level is complete.
    /// - Reached with fewer cores: only a short on-screen message, nothing else happens.
    /// </summary>
    public class Generator : MonoBehaviour
    {
        public Vector2Int cell;
        public Light glow;
        public float messageTime = 2f;
        [Header("Light (off until activated)")]
        public float activeIntensity = 6f;
        public float activeRange = 7f;
        public float lightUpTime = 0.8f;

        bool active;
        bool playerWasHere;
        float lightUp;

        void Start()
        {
            if (glow == null)
            {
                glow = new GameObject("Glow").AddComponent<Light>();
                glow.transform.SetParent(transform, false);
                glow.transform.localPosition = new Vector3(0, 1.2f, 0);
                glow.type = LightType.Point;
                glow.shadows = LightShadows.None;
            }
            // Dark while inactive: only the red emissive parts show it is there
            glow.color = Visuals.Cyan;
            glow.range = activeRange;
            glow.intensity = 0f;
            glow.enabled = false;
        }

        void Update()
        {
            transform.Rotate(0f, (active ? 120f : 20f) * Time.deltaTime, 0f);

            // Light switching on: flickers a few times, then settles at full brightness
            if (active && lightUp < 1f)
            {
                lightUp = Mathf.Min(1f, lightUp + Time.deltaTime / lightUpTime);
                bool flickerOff = lightUp < 0.6f && Mathf.Repeat(lightUp * 10f, 1f) < 0.4f;
                glow.intensity = flickerOff ? 0f : activeIntensity * lightUp;
            }

            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;

            // React once each time Pac-Man arrives on the cell (not every frame while standing on it)
            bool here = PlayerIsHere;
            if (here && !playerWasHere)
            {
                if (gm.AllCoresCollected)
                {
                    Activate();
                    gm.CompleteLevel();
                }
                else if (GameHUD.Instance != null)
                {
                    GameHUD.Instance.ShowMessage($"Collect all {gm.CoresRequired} Nexus Cores", messageTime);
                }
            }
            playerWasHere = here;
        }

        public bool PlayerIsHere =>
            PlayerMovement.Instance != null && PlayerMovement.Instance.CurrentCell == cell;

        public bool IsActive => active;

        void Activate()
        {
            active = true;
            lightUp = 0f;
            glow.enabled = true;
            foreach (var part in new[] { "Core", "Beam" })
            {
                var t = transform.Find(part);
                if (t != null) Visuals.SetMaterial(t.gameObject, Visuals.GetMaterial(Visuals.Cyan, Visuals.Cyan, 3f));
            }
        }
    }
}
