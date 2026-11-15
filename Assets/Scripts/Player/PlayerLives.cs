using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Enemy contact and respawn (GDD 6). When a Bug touches Pac-Man:
    /// one life is lost, Pac-Man goes back to 'P', every Bug goes back to its spawn cell,
    /// and Pac-Man blinks and cannot be hit for a short time. Collected Nexus Cores stay collected.
    /// Power-ups (PlayerPowers): a blue Bug (Devour Mode) is eaten instead; the Energy Shield absorbs one hit
    /// (the Bug is pushed away and stunned).
    /// Contact is a simple distance check (no physics).
    /// </summary>
    public class PlayerLives : MonoBehaviour
    {
        public float hitDistance = 0.6f;          // world units, ~ half a cell plus a bit
        public float invulnerableTime = 3f;
        public float blinkRate = 10f;             // blinks per second

        public bool IsInvulnerable => invulnerableLeft > 0f;

        PlayerMovement mover;
        PlayerPowers powers;
        Renderer[] renderers;
        float invulnerableLeft;

        void Start()
        {
            mover = GetComponent<PlayerMovement>();
            powers = GetComponent<PlayerPowers>();
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
            }
            if (!gm.IsPlaying) return;

            for (int i = BugAI.All.Count - 1; i >= 0; i--)
            {
                var bug = BugAI.All[i];
                if (bug.IsEaten) continue;
                var d = bug.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude >= hitDistance * hitDistance) continue;

                if (bug.IsVulnerable)
                {
                    EatBug(gm, bug);
                    continue;
                }
                if (bug.IsFrozen || invulnerableLeft > 0f) continue;   // stunned Bugs are harmless
                if (powers != null && powers.ConsumeShield())
                {
                    bug.Knockback(mover.CurrentCell, 3, 1.5f);
                    invulnerableLeft = 1f;
                    return;
                }
                Hit(gm);
                return;
            }
        }

        void EatBug(GameManager gm, BugAI bug)
        {
            int points = powers != null ? powers.NextEatScore() : 200;
            gm.AddScore(points);
            if (GameHUD.Instance != null) GameHUD.Instance.Popup(bug.transform.position + Vector3.up * 0.6f, points.ToString(), Color.white, 1.1f);
            bug.Eat();
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
