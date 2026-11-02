using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rescate.EditorTools
{
    /// <summary>
    /// Sintetiza música y efectos ORIGINALES (sin contenido de terceros) y los escribe
    /// como WAV mono en Assets/Game/Resources/Audio, donde AudioManager los carga.
    /// Todo es determinista: se puede regenerar en cualquier momento.
    /// </summary>
    public static class AudioBuilder
    {
        const int Rate = 22050;
        const string Folder = "Assets/Game/Resources/Audio";

        enum Wave { Sine, Square, Triangle, Saw, Noise }

        [MenuItem("Rescate/Audio/Generar audio original", priority = 200)]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Folder);

            // ------------------------------------------------ MÚSICA
            WriteMusic("Music_menu", BuildMenuMusic(), 24f);
            WriteMusic("Music_level", BuildLevelMusic(), 26f);
            WriteMusic("Music_boss", BuildBossMusic(), 22f);
            WriteMusic("Music_victory", BuildVictoryMusic(), 14f);

            // ------------------------------------------------ EFECTOS
            WriteSfx("jump", SfxJump());
            WriteSfx("attack", SfxAttack());
            WriteSfx("magic", SfxMagic());
            WriteSfx("damage", SfxDamage());
            WriteSfx("defeat", SfxDefeat());
            WriteSfx("levelup", SfxLevelUp());
            WriteSfx("coin", SfxCoin());
            WriteSfx("crystal", SfxCrystal());
            WriteSfx("heart", SfxHeart());
            WriteSfx("key", SfxKey());
            WriteSfx("xp", SfxXp());
            WriteSfx("door", SfxDoor());
            WriteSfx("lockedDoor", SfxLockedDoor());
            WriteSfx("checkpoint", SfxCheckpoint());
            WriteSfx("princess", SfxPrincess());
            WriteSfx("hit", SfxHit());
            WriteSfx("enemyDeath", SfxEnemyDeath());
            WriteSfx("stomp", SfxStomp());
            WriteSfx("bossPhase", SfxBossPhase());
            WriteSfx("bossCast", SfxBossCast());
            WriteSfx("victory", SfxVictory());

            AssetDatabase.Refresh();
            Debug.Log("[Audio] Audio original generado en " + Folder);
        }

        // ============================================================ MÚSICA

        static float[] BuildLevelMusic()
        {
            const float bpm = 132f;
            float[] buf = Blank(bpm, 32f);

            int[] melA = { 69, 71, 74, 76, 74, 71, 69, 66, 67, 69, 71, 74, 71, 69, 67, 64 };
            int[] melB = { 69, 71, 74, 76, 78, 76, 74, 71, 74, 73, 71, 69, 71, 69, 66, 62 };
            float[] d1 = Ones(16, 1f);

            Melody(buf, bpm, 0f, melA, d1, Wave.Square, 0.16f, 0.03f);
            Melody(buf, bpm, 16f, melB, d1, Wave.Square, 0.16f, 0.03f);
            // armonía suave una octava arriba, más floja
            Melody(buf, bpm, 0f, melA, d1, Wave.Triangle, 0.07f, 0.06f, 12);
            Melody(buf, bpm, 16f, melB, d1, Wave.Triangle, 0.07f, 0.06f, 12);

            int[] bass = { 50, 50, 55, 55, 57, 57, 50, 50, 57, 57, 55, 55, 50, 50, 43, 43,
                           50, 50, 55, 55, 57, 57, 50, 50, 55, 55, 57, 57, 50, 50, 50, 50 };
            Melody(buf, bpm, 0f, bass, Ones(32, 1f), Wave.Triangle, 0.22f, 0.02f);

            Drums(buf, bpm, 0, 32, 0.16f, 0.05f, 0.10f);
            return Finish(buf, 0.86f);
        }

        static float[] BuildMenuMusic()
        {
            const float bpm = 92f;
            float[] buf = Blank(bpm, 32f);

            int[] arpA = { 72, 76, 79, 84, 79, 76, 72, 0, 74, 77, 81, 86, 81, 77, 74, 0 };
            int[] arpB = { 71, 74, 79, 83, 79, 74, 71, 0, 72, 76, 79, 84, 79, 76, 72, 0 };
            float[] d = Ones(16, 1f);

            Melody(buf, bpm, 0f, arpA, d, Wave.Triangle, 0.20f, 0.10f);
            Melody(buf, bpm, 16f, arpB, d, Wave.Triangle, 0.20f, 0.10f);
            Melody(buf, bpm, 0f, arpA, d, Wave.Sine, 0.10f, 0.14f, 12);
            Melody(buf, bpm, 16f, arpB, d, Wave.Sine, 0.10f, 0.14f, 12);

            // bajo sostenido por compás
            int[] bassRoots = { 48, 53, 55, 48, 53, 55, 48, 48 };
            for (int i = 0; i < bassRoots.Length; i++)
            {
                AddTone(buf, i * 4f * (60f / bpm), 4f * (60f / bpm) * 0.92f,
                    Midi(bassRoots[i]), Midi(bassRoots[i]), Wave.Sine, 0.24f, 0.05f, 0.6f);
            }

            Drums(buf, bpm, 0, 32, 0.05f, 0.02f, 0.03f);
            return Finish(buf, 0.82f);
        }

        static float[] BuildBossMusic()
        {
            const float bpm = 152f;
            float[] buf = Blank(bpm, 32f);

            int[] melA = { 62, 65, 69, 65, 62, 65, 69, 72, 62, 65, 69, 65, 74, 72, 69, 65 };
            int[] melB = { 62, 65, 69, 65, 62, 65, 69, 72, 74, 77, 81, 77, 74, 72, 69, 65 };
            float[] d = Ones(16, 0.5f);

            Melody(buf, bpm, 0f, melA, d, Wave.Saw, 0.17f, 0.02f);
            Melody(buf, bpm, 8f, melB, d, Wave.Saw, 0.19f, 0.02f);
            Melody(buf, bpm, 0f, melA, d, Wave.Square, 0.06f, 0.02f, 12);
            Melody(buf, bpm, 8f, melB, d, Wave.Square, 0.06f, 0.02f, 12);

            int[] bass = new int[32];
            for (int i = 0; i < 32; i++) bass[i] = (i % 4 < 2) ? 38 : ((i % 8 < 6) ? 45 : 41);
            Melody(buf, bpm, 0f, bass, Ones(32, 0.5f), Wave.Square, 0.20f, 0.01f);

            Drums(buf, bpm, 0, 32, 0.22f, 0.06f, 0.13f);
            // golpes tribales cada 2 compases
            for (int b = 0; b < 32; b += 8) AddTone(buf, b * (60f / bpm), 0.22f, 90f, 55f, Wave.Sine, 0.35f, 0.005f, 4f);
            return Finish(buf, 0.84f);
        }

        static float[] BuildVictoryMusic()
        {
            const float bpm = 116f;
            float[] buf = Blank(bpm, 16f);

            int[] mel = { 72, 76, 79, 84, 0, 79, 76, 79, 84, 86, 84, 81, 79, 76, 72, 0 };
            float[] d = { .5f, .5f, .5f, .5f, .5f, .5f, .5f, .5f,
                          .5f, .5f, .5f, .5f, .5f, .5f, 1f, 1f };
            Melody(buf, bpm, 0f, mel, d, Wave.Square, 0.18f, 0.02f);
            Melody(buf, bpm, 0f, mel, d, Wave.Triangle, 0.10f, 0.04f, 12);

            int[] bass = { 48, 48, 53, 53, 55, 55, 48, 48 };
            for (int i = 0; i < bass.Length; i++)
                AddTone(buf, i * 2f * (60f / bpm), 1.8f * (60f / bpm), Midi(bass[i]), Midi(bass[i]), Wave.Triangle, 0.24f, 0.02f, 0.8f);

            Drums(buf, bpm, 0, 16, 0.16f, 0.05f, 0.09f);
            return Finish(buf, 0.88f);
        }

        // ============================================================ EFECTOS

        static float[] SfxJump()
        {
            float[] b = New(0.20f);
            AddTone(b, 0f, 0.17f, 360f, 780f, Wave.Square, 0.34f, 0.005f, 2.2f);
            AddTone(b, 0f, 0.12f, 720f, 900f, Wave.Sine, 0.12f, 0.005f, 3f);
            return Finish(b, 0.9f);
        }

        static float[] SfxAttack()
        {
            float[] b = New(0.18f);
            AddTone(b, 0f, 0.11f, 1400f, 220f, Wave.Noise, 0.30f, 0.002f, 5f);
            AddTone(b, 0f, 0.10f, 240f, 90f, Wave.Saw, 0.22f, 0.002f, 4f);
            return Finish(b, 0.9f);
        }

        static float[] SfxMagic()
        {
            float[] b = New(0.42f);
            AddTone(b, 0.00f, 0.22f, 700f, 900f, Wave.Sine, 0.22f, 0.01f, 2.0f);
            AddTone(b, 0.05f, 0.22f, 950f, 1180f, Wave.Sine, 0.20f, 0.01f, 2.0f);
            AddTone(b, 0.10f, 0.26f, 1250f, 1560f, Wave.Sine, 0.18f, 0.01f, 2.0f);
            AddTone(b, 0.00f, 0.34f, 400f, 1700f, Wave.Noise, 0.06f, 0.05f, 1.6f);
            return Finish(b, 0.88f);
        }

        static float[] SfxDamage()
        {
            float[] b = New(0.42f);
            AddTone(b, 0f, 0.28f, 320f, 80f, Wave.Saw, 0.38f, 0.003f, 2.6f);
            AddTone(b, 0f, 0.09f, 900f, 300f, Wave.Noise, 0.22f, 0.002f, 5f);
            return Finish(b, 0.9f);
        }

        static float[] SfxDefeat()
        {
            float[] b = New(1.10f);
            int[] notes = { 392, 349, 311, 262, 208, 156 };
            for (int i = 0; i < notes.Length; i++)
                AddTone(b, i * 0.16f, 0.26f, Midi(52 + i * 0), notes[i], Wave.Triangle, 0.26f, 0.01f, 1.6f);
            AddTone(b, 0f, 1.0f, 200f, 60f, Wave.Sine, 0.16f, 0.2f, 1.2f);
            return Finish(b, 0.9f);
        }

        static float[] SfxLevelUp()
        {
            float[] b = New(0.62f);
            int[] notes = { 523, 659, 784, 1046, 1318 };
            for (int i = 0; i < notes.Length; i++)
                AddTone(b, i * 0.08f, 0.24f, notes[i], notes[i], Wave.Square, 0.20f, 0.005f, 2.4f);
            AddTone(b, 0.30f, 0.30f, 1046f, 1568f, Wave.Sine, 0.14f, 0.01f, 2f);
            return Finish(b, 0.9f);
        }

        static float[] SfxCoin()
        {
            float[] b = New(0.30f);
            AddTone(b, 0.00f, 0.07f, 1046f, 1046f, Wave.Square, 0.24f, 0.002f, 3f);
            AddTone(b, 0.07f, 0.19f, 1568f, 1568f, Wave.Square, 0.22f, 0.002f, 2.2f);
            AddTone(b, 0.07f, 0.19f, 2093f, 2093f, Wave.Sine, 0.08f, 0.005f, 2.5f);
            return Finish(b, 0.9f);
        }

        static float[] SfxCrystal()
        {
            float[] b = New(0.55f);
            int[] notes = { 880, 1108, 1320, 1760 };
            for (int i = 0; i < notes.Length; i++)
                AddTone(b, i * 0.04f, 0.42f, notes[i], notes[i], Wave.Sine, 0.17f, 0.008f, 1.7f);
            AddTone(b, 0f, 0.45f, 1400f, 2600f, Wave.Noise, 0.05f, 0.08f, 1.6f);
            return Finish(b, 0.88f);
        }

        static float[] SfxHeart()
        {
            float[] b = New(0.36f);
            AddTone(b, 0.00f, 0.16f, 660f, 880f, Wave.Sine, 0.24f, 0.01f, 2f);
            AddTone(b, 0.10f, 0.22f, 990f, 990f, Wave.Sine, 0.18f, 0.01f, 2f);
            return Finish(b, 0.88f);
        }

        static float[] SfxKey()
        {
            float[] b = New(0.50f);
            int[] notes = { 1318, 1760, 2093 };
            for (int i = 0; i < notes.Length; i++)
                AddTone(b, i * 0.06f, 0.30f, notes[i], notes[i], Wave.Square, 0.15f, 0.004f, 2.4f);
            AddTone(b, 0f, 0.10f, 2400f, 3200f, Wave.Noise, 0.10f, 0.002f, 6f);
            return Finish(b, 0.9f);
        }

        static float[] SfxXp()
        {
            float[] b = New(0.16f);
            AddTone(b, 0f, 0.10f, 1200f, 1700f, Wave.Square, 0.16f, 0.003f, 3.5f);
            return Finish(b, 0.85f);
        }

        static float[] SfxDoor()
        {
            float[] b = New(0.60f);
            AddTone(b, 0f, 0.40f, 160f, 70f, Wave.Saw, 0.26f, 0.02f, 1.8f);
            AddTone(b, 0f, 0.35f, 400f, 120f, Wave.Noise, 0.16f, 0.01f, 2.2f);
            AddTone(b, 0.28f, 0.22f, 720f, 980f, Wave.Sine, 0.14f, 0.01f, 2.6f);
            return Finish(b, 0.9f);
        }

        static float[] SfxLockedDoor()
        {
            float[] b = New(0.30f);
            AddTone(b, 0f, 0.16f, 170f, 110f, Wave.Square, 0.26f, 0.003f, 3f);
            AddTone(b, 0.05f, 0.10f, 900f, 500f, Wave.Noise, 0.14f, 0.002f, 5f);
            return Finish(b, 0.9f);
        }

        static float[] SfxCheckpoint()
        {
            float[] b = New(0.70f);
            int[] notes = { 784, 988, 1175, 1568 };
            for (int i = 0; i < notes.Length; i++)
                AddTone(b, i * 0.10f, 0.32f, notes[i], notes[i], Wave.Sine, 0.19f, 0.008f, 1.9f);
            return Finish(b, 0.88f);
        }

        static float[] SfxPrincess()
        {
            float[] b = New(0.95f);
            int[] notes = { 1046, 1318, 1568, 2093, 1568 };
            for (int i = 0; i < notes.Length; i++)
                AddTone(b, i * 0.14f, 0.42f, notes[i], notes[i], Wave.Sine, 0.16f, 0.02f, 1.5f);
            return Finish(b, 0.86f);
        }

        static float[] SfxHit()
        {
            float[] b = New(0.14f);
            AddTone(b, 0f, 0.08f, 700f, 200f, Wave.Noise, 0.28f, 0.001f, 6f);
            AddTone(b, 0f, 0.07f, 320f, 130f, Wave.Square, 0.20f, 0.001f, 5f);
            return Finish(b, 0.9f);
        }

        static float[] SfxEnemyDeath()
        {
            float[] b = New(0.42f);
            AddTone(b, 0f, 0.32f, 520f, 80f, Wave.Saw, 0.26f, 0.003f, 2.6f);
            AddTone(b, 0f, 0.26f, 1200f, 200f, Wave.Noise, 0.20f, 0.002f, 3f);
            return Finish(b, 0.9f);
        }

        static float[] SfxStomp()
        {
            float[] b = New(0.22f);
            AddTone(b, 0f, 0.14f, 200f, 70f, Wave.Sine, 0.44f, 0.001f, 4.5f);
            AddTone(b, 0f, 0.06f, 1200f, 400f, Wave.Noise, 0.16f, 0.001f, 7f);
            return Finish(b, 0.92f);
        }

        static float[] SfxBossPhase()
        {
            float[] b = New(0.90f);
            AddTone(b, 0f, 0.75f, 55f, 120f, Wave.Saw, 0.34f, 0.05f, 1.4f);
            AddTone(b, 0.2f, 0.55f, 300f, 900f, Wave.Noise, 0.12f, 0.10f, 1.6f);
            AddTone(b, 0.45f, 0.35f, 700f, 1400f, Wave.Square, 0.10f, 0.01f, 2.4f);
            return Finish(b, 0.9f);
        }

        static float[] SfxBossCast()
        {
            float[] b = New(0.45f);
            AddTone(b, 0f, 0.34f, 600f, 1800f, Wave.Saw, 0.22f, 0.02f, 1.8f);
            AddTone(b, 0.06f, 0.32f, 200f, 900f, Wave.Noise, 0.14f, 0.03f, 2f);
            return Finish(b, 0.9f);
        }

        static float[] SfxVictory()
        {
            float[] b = New(1.40f);
            int[] notes = { 523, 659, 784, 1046, 1318 };
            float[] durs = { 0.12f, 0.12f, 0.12f, 0.20f, 0.70f };
            float t = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                AddTone(b, t, durs[i] + 0.12f, notes[i], notes[i], Wave.Square, 0.20f, 0.004f, 2.0f);
                AddTone(b, t, durs[i] + 0.12f, notes[i] * 2f, notes[i] * 2f, Wave.Sine, 0.08f, 0.006f, 2.2f);
                t += durs[i];
            }
            AddTone(b, 0.56f, 0.80f, 1046f, 1046f, Wave.Triangle, 0.14f, 0.02f, 1.2f);
            return Finish(b, 0.9f);
        }

        // ============================================================ SÍNTESIS

        static float Midi(int n) { return 440f * Mathf.Pow(2f, (n - 69) / 12f); }

        static float[] Blank(float bpm, float beats)
        {
            int samples = Mathf.CeilToInt(beats * (60f / bpm) * Rate);
            return new float[samples];
        }

        static float[] New(float seconds) { return new float[Mathf.CeilToInt(seconds * Rate)]; }

        static float[] Ones(int count, float value)
        {
            float[] a = new float[count];
            for (int i = 0; i < count; i++) a[i] = value;
            return a;
        }

        static void Melody(float[] buf, float bpm, float startBeat, int[] midis, float[] beats,
            Wave wave, float amp, float attack, int transpose = 0)
        {
            float beatDur = 60f / bpm;
            float t = startBeat * beatDur;
            for (int i = 0; i < midis.Length; i++)
            {
                float dur = beats[Mathf.Min(i, beats.Length - 1)] * beatDur;
                if (midis[i] > 0)
                {
                    float f = Midi(midis[i] + transpose);
                    AddTone(buf, t, dur * 0.94f, f, f, wave, amp, attack, 1.6f);
                }
                t += dur;
            }
        }

        static void Drums(float[] buf, float bpm, int startBeat, int beats, float kick, float hat, float snare)
        {
            float beatDur = 60f / bpm;
            for (int b = startBeat; b < startBeat + beats; b++)
            {
                float t = b * beatDur;
                if (b % 4 == 0 || b % 4 == 2) AddTone(buf, t, 0.13f, 130f, 58f, Wave.Sine, kick, 0.001f, 5f);
                if (b % 8 == 4) AddTone(buf, t, 0.10f, 1600f, 900f, Wave.Noise, snare, 0.001f, 7f);
                AddTone(buf, t, 0.035f, 6000f, 4000f, Wave.Noise, hat, 0.001f, 9f);
                AddTone(buf, t + beatDur * 0.5f, 0.03f, 6000f, 4000f, Wave.Noise, hat * 0.7f, 0.001f, 9f);
            }
        }

        static void AddTone(float[] buf, float start, float dur, float f0, float f1,
            Wave wave, float amp, float attack = 0.02f, float decay = 2f)
        {
            if (buf == null || buf.Length == 0) return;
            int startIdx = Mathf.RoundToInt(start * Rate);
            int len = Mathf.RoundToInt(dur * Rate);
            if (len <= 0) return;

            double phase = 0.0;
            uint noise = (uint)((f0 * 7919f) + 0x9E3779B9u);
            float invRate = 1f / Rate;

            for (int i = 0; i < len; i++)
            {
                int idx = startIdx + i;
                if (idx >= buf.Length) break;

                float tn = (float)i / len;
                float freq = Mathf.Lerp(f0, f1, tn);
                phase += 2.0 * Mathf.PI * freq * invRate;
                if (phase > 1e7) phase -= 1e7;

                float s;
                switch (wave)
                {
                    case Wave.Sine: s = (float)System.Math.Sin(phase); break;
                    case Wave.Square: s = System.Math.Sin(phase) >= 0.0 ? 0.7f : -0.7f; break;
                    case Wave.Triangle:
                        s = (float)(2.0 / Mathf.PI * System.Math.Asin(System.Math.Sin(phase)));
                        break;
                    case Wave.Saw:
                        {
                            double norm = phase / (2.0 * System.Math.PI);
                            s = (float)(2.0 * (norm - System.Math.Floor(norm + 0.5)));
                            break;
                        }
                    default:
                        noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                        s = ((noise & 0xFFFF) / 32768f) - 1f;
                        break;
                }

                // envolvente: ataque lineal + caída exponencial + release al final
                float a = attack <= 0.0001f ? 1f : Mathf.Clamp01(tn / (attack * 40f + 0.001f));
                float rel = Mathf.Clamp01((1f - tn) / 0.10f);
                float env = a * rel * (float)System.Math.Exp(-decay * tn);

                buf[idx] += s * amp * env;
            }
        }

        static float[] Finish(float[] buf, float peak)
        {
            // saturación suave + normalización para evitar clipping al mezclar
            float max = 0.0001f;
            for (int i = 0; i < buf.Length; i++)
            {
                float x = buf[i];
                x = x / (1f + Mathf.Abs(x) * 0.6f);
                buf[i] = x;
                float ax = Mathf.Abs(x);
                if (ax > max) max = ax;
            }
            float gain = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= gain;

            // fade corto en los extremos para evitar clics al reproducir en bucle
            int fade = Mathf.Min(Rate / 200, buf.Length / 8);
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                buf[i] *= k;
                buf[buf.Length - 1 - i] *= k;
            }
            return buf;
        }

        // ============================================================ ESCRITURA

        static void WriteMusic(string name, float[] data, float expectedSeconds)
        {
            WriteWav(name, data, true);
            if (Mathf.Abs(data.Length / (float)Rate - expectedSeconds) > 8f)
                Debug.LogWarning("[Audio] " + name + " dura " + (data.Length / (float)Rate).ToString("F1") + "s (esperado ~" + expectedSeconds + "s)");
        }

        static void WriteSfx(string name, float[] data) { WriteWav(name, data, false); }

        static void WriteWav(string name, float[] data, bool music)
        {
            string path = Folder + "/" + name + ".wav";
            int sampleCount = data.Length;

            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                int dataBytes = sampleCount * 2;
                w.Write(Ascii("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(Ascii("WAVE"));
                w.Write(Ascii("fmt "));
                w.Write(16);
                w.Write((short)1);          // PCM
                w.Write((short)1);          // mono
                w.Write(Rate);
                w.Write(Rate * 2);          // byte rate
                w.Write((short)2);          // block align
                w.Write((short)16);         // bits
                w.Write(Ascii("data"));
                w.Write(dataBytes);

                for (int i = 0; i < sampleCount; i++)
                {
                    float v = Mathf.Clamp(data[i], -1f, 1f);
                    w.Write((short)Mathf.RoundToInt(v * 32767f));
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            AudioImporter ai = AssetImporter.GetAtPath(path) as AudioImporter;
            if (ai != null)
            {
                AudioImporterSampleSettings s = ai.defaultSampleSettings;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                s.quality = music ? 0.7f : 1f;
                ai.defaultSampleSettings = s;
                ai.forceToMono = true;
                ai.SaveAndReimport();
            }
        }

        static byte[] Ascii(string s) { return System.Text.Encoding.ASCII.GetBytes(s); }
    }
}
