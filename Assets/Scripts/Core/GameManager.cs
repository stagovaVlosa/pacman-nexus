using System;
using UnityEngine;

namespace Nexus
{
    /// <summary>
    /// Keeps the state of the current level: score, energy, Nexus Cores, completion.
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
        public float orbEnergy = 4f;
        public float maxEnergy = 100f;

        public int Score { get; private set; }
        public float Energy { get; private set; }
        public int CoresCollected { get; private set; }
        public int CoresRequired { get; private set; } = 10;
        public bool AllCoresCollected => CoresCollected >= CoresRequired;
        public bool LevelComplete { get; private set; }

        public event Action OnAllCoresCollected;
        public event Action OnLevelComplete;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (LevelBuilder.Instance != null && LevelBuilder.Instance.CoreCount > 0)
                CoresRequired = LevelBuilder.Instance.CoreCount;
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
            OnLevelComplete?.Invoke();
        }
    }
}
