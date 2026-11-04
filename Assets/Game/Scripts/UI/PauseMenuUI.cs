using UnityEngine;

namespace Rescate
{
    /// <summary>Pausa con Escape o botón en pantalla. Congela el tiempo del juego.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject pausePanel;
        public bool canPause = true;

        bool paused;

        void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        void Update()
        {
            if (!canPause) return;

            if (paused)
            {
                // Continuar: Options/Start (gamepad), Escape (teclado) o Circle (cancelar).
                if (InputHub.PausePressed || InputHub.CancelPressed) Resume();
            }
            else if (InputHub.PausePressed)
            {
                Pause();
            }
        }

        public void Toggle()
        {
            if (paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (paused) return;
            paused = true;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            // Foco inicial para poder navegar con D-Pad / stick izquierdo.
            UIFocus.SelectFirst(pausePanel);
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            UIFocus.Clear();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        public void GoToMenu()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
        }

        void OnDisable()
        {
            if (paused)
            {
                paused = false;
                Time.timeScale = 1f;
            }
        }
    }
}
