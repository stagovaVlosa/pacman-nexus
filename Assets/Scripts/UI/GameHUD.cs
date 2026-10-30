using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Temporary prototype HUD drawn with OnGUI (no Canvas setup needed).
    /// Will be replaced by a proper cyberpunk UI in week 5.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        GUIStyle label, big, small;

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (label == null) CreateStyles();

            GUI.Label(new Rect(20, 15, 600, 30), gm.levelName.ToUpper(), label);
            GUI.Label(new Rect(20, 45, 600, 30), $"Nexus Cores  {gm.CoresCollected}/{gm.CoresRequired}", label);
            GUI.Label(new Rect(20, 75, 600, 30), $"Score  {gm.Score}", label);

            // Energy meter (bottom center)
            float w = 300f, h = 14f;
            var bar = new Rect((Screen.width - w) / 2f, Screen.height - 40f, w, h);
            GUI.color = new Color(1, 1, 1, 0.25f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Visuals.Cyan;
            GUI.DrawTexture(new Rect(bar.x, bar.y, w * gm.Energy / gm.maxEnergy, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(bar.x, bar.y - 22f, w, 20f), "ENERGY", small);

            if (gm.LevelComplete)
            {
                GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 60), "REGION RESTORED", big);
                GUI.Label(new Rect(0, Screen.height / 2f + 20, Screen.width, 30), $"Final score: {gm.Score}", small);
            }
            else if (gm.AllCoresCollected)
            {
                GUI.Label(new Rect(0, 20, Screen.width, 30),
                    "Generator online! Return to the generator and press E", small);
            }
        }

        void CreateStyles()
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            label.normal.textColor = Visuals.Cyan;
            small = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            small.normal.textColor = Visuals.NeonYellow;
            big = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Visuals.Cyan;
        }
    }
}
