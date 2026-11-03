using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Enemy contact and respawn (GDD 6). When a Bug touches Pac-Man:
    /// one life is lost, Pac-Man goes back to 'P', every Bug goes back to its spawn cell,
    /// and Pac-Man blinks and cannot be hit for a short time. Collected Nexus Cores stay collected.
    /// Contact is a simple distance check (no physics).
    /// </summary>
    public class PlayerLives : MonoBehaviour
    {
        public float hitDistance = 0.6f;          // world units, ~ half a cell plus a bit
        public float invulnerableTime = 2f;
        public float blinkRate = 10f;             // blinks per second

        public bool IsInvulnerable => invulnerableLeft > 0f;

        PlayerMovement mover;
        Renderer[] renderers;
        float invulnerableLeft;

        void Start()
        {
            mover = GetComponent<PlayerMovement>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || mover == null) return;

            if (invulnerableLeft > 0f)
            {
                invulnerableLeft -= Time.deltaTime;
                bool visible = invulnerableLeft <= 0f || Mathf.Repeat(invulnerableLeft * blinkRate, 1f) > 0.5f;
                SetVisible(visible);
                return;
            }
            if (!gm.IsPlaying) return;

            foreach (var bug in BugAI.All)
            {
                if (bug.IsFrozen) continue; // stunned by an Energy Blast: harmless
                var d = bug.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude < hitDistance * hitDistance)
                {
                    Hit(gm);
                    return;
                }
            }
        }

        void Hit(GameManager gm)
        {
            gm.LoseLife();
            if (gm.GameOver) return; // stays where it was caught, the HUD shows GAME OVER

            mover.Respawn();
            foreach (var bug in BugAI.All) bug.ResetToSpawn();
            invulnerableLeft = invulnerableTime;
        }

        void SetVisible(bool visible)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;
        }
    }
}
