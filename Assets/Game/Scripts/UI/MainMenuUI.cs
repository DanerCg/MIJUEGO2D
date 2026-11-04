using UnityEngine;
using UnityEngine.UI;

namespace Rescate
{
    /// <summary>Menú principal: Jugar, Continuar, Opciones y Salir.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject mainPanel;
        public GameObject optionsPanel;
        public Slider musicSlider;
        public Slider sfxSlider;
        public Button continueButton;
        public Text continueLabel;

        void Start()
        {
            GameData.Load();
            Time.timeScale = 1f;
            InputHub.ResetTouch();

            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            if (audioCheck()) AudioManager.PlayMusic("Music_menu");

            if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameData.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameData.SfxVolume);

            UpdateContinueState();
            UIFocus.SelectFirst(mainPanel);
        }

        void Update()
        {
            // Circle (gamepad) o Escape (teclado) cierran las opciones.
            if (optionsPanel != null && optionsPanel.activeSelf && InputHub.CancelPressed)
            {
                CloseOptions();
            }
        }

        bool audioCheck()
        {
            return AudioManager.Instance != null;
        }

        void UpdateContinueState()
        {
            bool hasProgress = GameData.UnlockedLevel > 1;
            if (continueButton != null) continueButton.interactable = hasProgress || GameData.Coins > 0;
            if (continueLabel != null)
            {
                continueLabel.text = hasProgress ? "CONTINUAR (NIVEL " + GameData.UnlockedLevel + ")" : "CONTINUAR";
            }
        }

        public void NewGame()
        {
            if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
        }

        public void Continue()
        {
            if (GameManager.Instance != null) GameManager.Instance.ContinueGame();
        }

        public void OpenOptions()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            UIFocus.SelectFirst(optionsPanel);
        }

        public void CloseOptions()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            GameData.Save();
            UIFocus.SelectFirst(mainPanel);
        }

        public void SetMusicVolume(float value)
        {
            GameData.MusicVolume = Mathf.Clamp01(value);
            AudioManager.ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            GameData.SfxVolume = Mathf.Clamp01(value);
        }

        public void TestSfx()
        {
            AudioManager.PlaySfx("coin", 1f);
        }

        public void Quit()
        {
            GameData.Save();
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }
}
