using UnityEngine;

namespace Rescate
{
    /// <summary>Pantalla de Game Over: reintentar desde el último punto de control o volver al menú.</summary>
    public class GameOverUI : MonoBehaviour
    {
        public GameObject panel;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        void Start()
        {
            GameEvents.PlayerDied += Show;
        }

        void OnDestroy()
        {
            GameEvents.PlayerDied -= Show;
        }

        void Show()
        {
            StartCoroutine(ShowDelayed());
        }

        System.Collections.IEnumerator ShowDelayed()
        {
            yield return new WaitForSeconds(1.1f);
            if (panel != null) panel.SetActive(true);
            UIFocus.SelectFirst(panel);
        }

        public void Retry()
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        public void GoToMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
        }
    }
}
