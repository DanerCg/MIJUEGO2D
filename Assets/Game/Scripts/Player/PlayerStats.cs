using System;
using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Vida, experiencia, nivel, monedas, cristales y llaves del jugador.
    /// Nivel 2 desbloquea el proyectil mágico; nivel 3 amplía la vida máxima.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }
        public const int MaxLevel = 3;

        [Header("Vida")]
        public int maxHearts = 3;
        public int hearts = 3;

        [Header("Progreso")]
        public int level = 1;
        public int xp;

        public int coins;
        public int crystals;
        public int keys;

        public event Action Changed;

        public int XpRequired
        {
            get
            {
                if (level <= 1) return 100;
                if (level == 2) return 250;
                return 0;
            }
        }

        public float XpNormalized
        {
            get
            {
                int req = XpRequired;
                if (req <= 0) return 1f;
                return Mathf.Clamp01((float)xp / req);
            }
        }

        public bool CanCastMagic { get { return level >= 2; } }

        void Awake()
        {
            Instance = this;
            GameData.Load();
            coins = GameData.Coins;
            crystals = GameData.Crystals;
            keys = GameData.Keys;
            level = Mathf.Clamp(GameData.PlayerLevel, 1, MaxLevel);
            xp = Mathf.Max(0, GameData.Xp);
            maxHearts = 3 + (level >= 3 ? 2 : 0);
            hearts = maxHearts;
        }

        void Start()
        {
            Raise();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Raise()
        {
            if (Changed != null) Changed();
        }

        public void Persist()
        {
            GameData.Coins = coins;
            GameData.Crystals = crystals;
            GameData.Keys = keys;
            GameData.PlayerLevel = level;
            GameData.Xp = xp;
            GameData.Save();
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            if (level >= MaxLevel)
            {
                xp = XpRequired;
                Raise();
                return;
            }

            xp += amount;
            while (level < MaxLevel && xp >= XpRequired)
            {
                xp -= XpRequired;
                LevelUp();
            }

            if (level >= MaxLevel && xp > 250) xp = 250;
            Persist();
            Raise();
        }

        void LevelUp()
        {
            level++;
            if (level >= 3)
            {
                maxHearts += 2;
                hearts = maxHearts;
            }

            AudioManager.PlaySfx("levelup");
            Fx.Burst(transform.position + Vector3.up * 1f, new Color(0.55f, 1f, 0.95f), 22, 5.5f, 0.18f, 0.7f);
            CameraFollow2D.Shake(0.14f, 0.2f);
            GameEvents.RaiseMessage("¡NIVEL " + level + "! " + (level == 2 ? "Proyectil mágico desbloqueado (F)" : "Vida máxima aumentada"));
        }

        public void AddCoins(int amount)
        {
            coins += amount;
            Persist();
            Raise();
        }

        public void AddCrystals(int amount)
        {
            crystals += amount;
            AddXp(amount);
        }

        public void AddKey()
        {
            keys = Mathf.Min(keys + 1, 3);
            Persist();
            Raise();
        }

        public void AddHearts(int amount)
        {
            hearts = Mathf.Min(hearts + amount, maxHearts);
            Raise();
        }

        /// <summary>Devuelve false si el jugador se ha quedado sin corazones.</summary>
        public bool RemoveHearts(int amount)
        {
            hearts = Mathf.Max(0, hearts - Mathf.Max(1, amount));
            Raise();
            return hearts > 0;
        }

        public void FullHeal()
        {
            hearts = maxHearts;
            Raise();
        }
    }
}
