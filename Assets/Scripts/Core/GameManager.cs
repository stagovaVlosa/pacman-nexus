using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Nexus
{
    /// <summary>
    /// Keeps the state of the current level: score, energy, Nexus Cores, lives, completion / game over.
    /// Other scripts read from here; they do not keep their own copy of the score.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Level")]
        public string levelName = "Neon District";

        [Header("Scoring")]
        public int orbScore = 10;
        public int coreScore = 500;
        public float orbEnergy = 1f;      // +1 energy per orb (max 100)
        public float maxEnergy = 100f;

        [Header("Lives (GDD 6)")]
        public int startLives = 3;

        public int Score { get; private set; }
        public float Energy { get; private set; }
        public int CoresCollected { get; private set; }
        public int CoresRequired { get; private set; } = 10;
        public bool AllCoresCollected => CoresCollected >= CoresRequired;
        public bool LevelComplete { get; private set; }
        public int Lives { get; private set; }
        public bool GameOver { get; private set; }
        /// <summary>False after the level is complete, the game is over or the game is paused: movement and enemies stop.</summary>
        public bool IsPlaying => !LevelComplete && !GameOver && !IntroSequence.Active && !PauseMenu.Paused;

        public event Action OnAllCoresCollected;
        public event Action OnLevelComplete;
        public event Action OnLifeLost;
        public event Action OnGameOver;

        float gameOverTime, completeTime;

        /// <summary>True when this level has a next region to load (LevelBuilder.nextScenePath).</summary>
        public bool HasNextLevel => LevelBuilder.Instance != null && !string.IsNullOrEmpty(LevelBuilder.Instance.nextScenePath);

        void Awake()
        {
            Instance = this;
            Lives = startLives;
        }

        void Update()
        {
            // Short delay so a key held during the hit does not restart immediately
            var kb = Keyboard.current;
            if (GameOver && kb != null && kb.spaceKey.wasPressedThisFrame && Time.unscaledTime - gameOverTime > 0.5f)
                Restart();
            // Level complete: ENTER loads the next region
            if (LevelComplete && HasNextLevel && kb != null && Time.unscaledTime - completeTime > 1.5f
                && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                LoadScene(LevelBuilder.Instance.nextScenePath);
        }

        /// <summary>Removes one life (collected Nexus Cores stay collected). At 0 lives the game is over.</summary>
        public void LoseLife()
        {
            if (!IsPlaying) return;
            Lives = Mathf.Max(0, Lives - 1);
            OnLifeLost?.Invoke();
            if (Lives == 0)
            {
                GameOver = true;
                gameOverTime = Time.unscaledTime;
                OnGameOver?.Invoke();
            }
        }

        /// <summary>Reloads the current scene (new game).</summary>
        public void Restart()
        {
            IntroSequence.SkipNextLevelCard = true;   // retrying a level does not show its title card again
            LoadScene(SceneManager.GetActiveScene().path);
        }

        static void LoadScene(string path)
        {
#if UNITY_EDITOR
            // Works in Play mode even if the scene is not in the Build Settings
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(path);
#endif
        }

        void Start()
        {
            if (LevelBuilder.Instance != null)
            {
                levelName = LevelBuilder.Instance.levelName;
                if (LevelBuilder.Instance.CoreCount > 0) CoresRequired = LevelBuilder.Instance.CoreCount;
            }
        }

        public void CollectOrb()
        {
            Score += orbScore;
            Energy = Mathf.Min(maxEnergy, Energy + orbEnergy);
        }

        public void CollectCore()
        {
            Score += coreScore;
            CoresCollected++;
            if (CoresCollected == CoresRequired) OnAllCoresCollected?.Invoke();
        }

        /// <summary>Extra points (eating a Bug in Devour Mode, picking up a power-up).</summary>
        public void AddScore(int points) => Score += points;

        public bool UseEnergy(float amount)
        {
            if (Energy < amount) return false;
            Energy -= amount;
            return true;
        }

        public void CompleteLevel()
        {
            if (LevelComplete || !AllCoresCollected) return;
            LevelComplete = true;
            completeTime = Time.unscaledTime;
            OnLevelComplete?.Invoke();
        }
    }
}
