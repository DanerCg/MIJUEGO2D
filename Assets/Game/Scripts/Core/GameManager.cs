using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rescate
{
    /// <summary>Eventos globales de juego (UI desacoplada).</summary>
    public static class GameEvents
    {
        public static event System.Action PlayerDied;
        public static event System.Action<string> Message;
        public static event System.Action BossDefeated;
        public static event System.Action CheckpointReached;

        public static void RaisePlayerDied() { if (PlayerDied != null) PlayerDied(); }
        public static void RaiseMessage(string text) { if (Message != null) Message(text); }
        public static void RaiseBossDefeated() { if (BossDefeated != null) BossDefeated(); }
        public static void RaiseCheckpointReached() { if (CheckpointReached != null) CheckpointReached(); }

        public static void ClearAll()
        {
            PlayerDied = null;
            Message = null;
            BossDefeated = null;
            CheckpointReached = null;
        }
    }

    /// <summary>
    /// Singleton persistente: flujo de escenas, progreso y estado de partida.
    /// Se autocrea en cualquier escena (no requiere objetos en la jerarquía).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string SceneMenu = "MainMenu";
        public const string SceneLevel1 = "Level1_Bosque";
        public const string SceneLevel2 = "Level2_Montanas";
        public const string SceneLevel3 = "Level3_Castillo";
        public const string SceneVictory = "Victoria";

        public static Vector2 CheckpointPosition;
        public static bool HasCheckpoint;
        public static int CurrentLevel = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("~GameManager");
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameData.Load();
            if (GetComponent<AudioManager>() == null) gameObject.AddComponent<AudioManager>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            GameEvents.ClearAll();
        }

        public static string SceneFor(int level)
        {
            if (level == 2) return SceneLevel2;
            if (level >= 3) return SceneLevel3;
            return SceneLevel1;
        }

        public void StartNewGame()
        {
            GameData.ResetProgress();
            HasCheckpoint = false;
            LoadLevel(1);
        }

        public void ContinueGame()
        {
            HasCheckpoint = false;
            LoadLevel(Mathf.Clamp(GameData.UnlockedLevel, 1, 3));
        }

        public void LoadLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 1, 3);
            HasCheckpoint = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneFor(CurrentLevel));
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneFor(CurrentLevel));
        }

        public void LoadNextLevel()
        {
            if (CurrentLevel >= 3)
            {
                LoadVictory();
                return;
            }

            int next = CurrentLevel + 1;
            if (GameData.UnlockedLevel < next)
            {
                GameData.UnlockedLevel = next;
                GameData.Save();
            }

            LoadLevel(next);
        }

        public void LoadVictory()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneVictory);
        }

        public void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneMenu);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
