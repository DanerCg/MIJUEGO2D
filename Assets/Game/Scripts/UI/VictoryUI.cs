using UnityEngine;
using UnityEngine.UI;

namespace Rescate
{
    /// <summary>Pantalla de victoria y créditos tras rescatar a Elena.</summary>
    public class VictoryUI : MonoBehaviour
    {
        public GameObject victoryPanel;
        public GameObject creditsPanel;
        public Text summaryText;

        void Start()
        {
            GameData.Load();
            Time.timeScale = 1f;

            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (AudioManager.Instance != null) AudioManager.PlayMusic("Music_victory");
            UIFocus.SelectFirst(victoryPanel);

            if (summaryText != null)
            {
                summaryText.text = "¡Elena está a salvo!\n\nMonedas: " + GameData.Coins +
                                   "\nCristales: " + GameData.Crystals +
                                   "\nNivel de Nico: " + GameData.PlayerLevel;
            }

            GameData.UnlockedLevel = 3;
            GameData.Save();
        }

        void Update()
        {
            if (creditsPanel != null && creditsPanel.activeSelf && (InputHub.ConfirmPressed || InputHub.CancelPressed))
            {
                ShowVictory();
            }
        }

        public void ShowCredits()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
            UIFocus.Clear();
        }

        public void ShowVictory()
        {
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(true);
            UIFocus.SelectFirst(victoryPanel);
        }

        public void GoToMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
        }
    }
}
