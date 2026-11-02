using System.Collections.Generic;
using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Reproductor de música y SFX. Los clips son WAV originales sintetizados
    /// por el proyecto y viven en Assets/Game/Resources/Audio.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource musicSource;
        AudioSource sfxSource;
        string currentMusic = "";
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        void Awake()
        {
            Instance = this;
            GameData.Load();

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = GameData.MusicVolume;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void PlaySfx(string clipName, float volume = 1f)
        {
            if (Instance == null) return;
            Instance.PlaySfxInternal(clipName, volume);
        }

        void PlaySfxInternal(string clipName, float volume)
        {
            AudioClip clip = Get(clipName);
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume * GameData.SfxVolume));
        }

        public static void PlayMusic(string clipName)
        {
            if (Instance == null) return;
            Instance.PlayMusicInternal(clipName);
        }

        void PlayMusicInternal(string clipName)
        {
            if (currentMusic == clipName && musicSource.isPlaying) return;
            AudioClip clip = Get(clipName);
            if (clip == null) return;
            currentMusic = clipName;
            musicSource.clip = clip;
            musicSource.volume = GameData.MusicVolume;
            musicSource.Play();
        }

        public static void StopMusic()
        {
            if (Instance == null) return;
            Instance.currentMusic = "";
            Instance.musicSource.Stop();
        }

        public static void ApplyVolumes()
        {
            if (Instance == null) return;
            Instance.musicSource.volume = GameData.MusicVolume;
        }

        AudioClip Get(string clipName)
        {
            AudioClip clip;
            if (cache.TryGetValue(clipName, out clip)) return clip;
            clip = Resources.Load<AudioClip>("Audio/" + clipName);
            cache[clipName] = clip;
            return clip;
        }
    }
}
