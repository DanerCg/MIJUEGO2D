using UnityEngine;
using UnityEngine.UI;

namespace Rescate
{
    /// <summary>HUD: corazones, monedas, llaves, nivel, barra de experiencia y objetivo actual.</summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Corazones")]
        public Image[] hearts;
        public Color heartFull = Color.white;
        public Color heartEmpty = new Color(1f, 1f, 1f, 0.18f);

        [Header("Textos")]
        public Text coinsText;
        public Text keysText;
        public Text levelText;
        public Text objectiveText;
        public Text messageText;

        [Header("Experiencia")]
        public Image xpFill;

        PlayerStats stats;
        float messageTimer;
        string objective = "";

        void Start()
        {
            stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null) stats.Changed += Refresh;

            GameEvents.Message += OnMessage;
            Refresh();
        }

        void OnDestroy()
        {
            if (stats != null) stats.Changed -= Refresh;
            GameEvents.Message -= OnMessage;
        }

        void Update()
        {
            if (messageText == null) return;
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                Color c = messageText.color;
                c.a = Mathf.Clamp01(messageTimer / 0.4f);
                messageText.color = c;
                if (messageTimer <= 0f) messageText.text = "";
            }
        }

        public void SetObjective(string text)
        {
            objective = text;
            if (objectiveText != null) objectiveText.text = "OBJETIVO: " + text;
        }

        void OnMessage(string text)
        {
            if (messageText == null) return;
            messageText.text = text;
            Color c = messageText.color;
            c.a = 1f;
            messageText.color = c;
            messageTimer = 2.6f;
        }

        public void Refresh()
        {
            if (stats == null) stats = FindFirstObjectByType<PlayerStats>();

            if (stats != null)
            {
                if (hearts != null)
                {
                    for (int i = 0; i < hearts.Length; i++)
                    {
                        if (hearts[i] == null) continue;
                        hearts[i].gameObject.SetActive(i < stats.maxHearts);
                        hearts[i].color = i < stats.hearts ? heartFull : heartEmpty;
                    }
                }

                if (coinsText != null) coinsText.text = "x " + stats.coins;
                if (keysText != null) keysText.text = "x " + stats.keys + "/3";
                if (levelText != null) levelText.text = "NV " + stats.level;
                if (xpFill != null) xpFill.fillAmount = stats.XpNormalized;
            }

            if (objectiveText != null && !string.IsNullOrEmpty(objective))
            {
                objectiveText.text = "OBJETIVO: " + objective;
            }
        }
    }
}
