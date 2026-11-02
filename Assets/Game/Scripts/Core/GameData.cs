using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Persistencia local del progreso (PlayerPrefs). Datos no sensibles.
    /// </summary>
    public static class GameData
    {
        const string K_UNLOCKED = "rc_unlocked";
        const string K_COINS    = "rc_coins";
        const string K_CRYSTALS = "rc_crystals";
        const string K_KEYS     = "rc_keys";
        const string K_LEVEL    = "rc_playerlevel";
        const string K_XP       = "rc_xp";
        const string K_MUSIC    = "rc_music";
        const string K_SFX      = "rc_sfx";

        public static int UnlockedLevel = 1;
        public static int Coins;
        public static int Crystals;
        public static int Keys;
        public static int PlayerLevel = 1;
        public static int Xp;
        public static float MusicVolume = 0.65f;
        public static float SfxVolume = 0.9f;
        public static bool Loaded;

        public static void Load()
        {
            if (Loaded) return;
            Loaded = true;
            UnlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt(K_UNLOCKED, 1), 1, 3);
            Coins      = PlayerPrefs.GetInt(K_COINS, 0);
            Crystals   = PlayerPrefs.GetInt(K_CRYSTALS, 0);
            Keys       = Mathf.Clamp(PlayerPrefs.GetInt(K_KEYS, 0), 0, 3);
            PlayerLevel = Mathf.Clamp(PlayerPrefs.GetInt(K_LEVEL, 1), 1, 3);
            Xp         = PlayerPrefs.GetInt(K_XP, 0);
            MusicVolume = PlayerPrefs.GetFloat(K_MUSIC, 0.65f);
            SfxVolume   = PlayerPrefs.GetFloat(K_SFX, 0.9f);
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(K_UNLOCKED, UnlockedLevel);
            PlayerPrefs.SetInt(K_COINS, Coins);
            PlayerPrefs.SetInt(K_CRYSTALS, Crystals);
            PlayerPrefs.SetInt(K_KEYS, Keys);
            PlayerPrefs.SetInt(K_LEVEL, PlayerLevel);
            PlayerPrefs.SetInt(K_XP, Xp);
            PlayerPrefs.SetFloat(K_MUSIC, MusicVolume);
            PlayerPrefs.SetFloat(K_SFX, SfxVolume);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            UnlockedLevel = 1;
            Coins = 0;
            Crystals = 0;
            Keys = 0;
            PlayerLevel = 1;
            Xp = 0;
            Save();
        }
    }
}
