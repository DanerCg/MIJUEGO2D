using System.Collections;
using UnityEngine;

namespace Rescate
{
    public enum CollectibleType { Coin, Crystal, Heart, Key, Xp }

    /// <summary>Recolectable con animación de flotación, pulso y efecto de recogida.</summary>
    public class Collectible : MonoBehaviour
    {
        public CollectibleType type = CollectibleType.Coin;
        public int amount = 1;
        public float bobAmplitude = 0.14f;
        public float bobSpeed = 2.6f;

        bool taken;
        Vector3 startPos;
        SpriteRenderer sr;
        float t;
        Vector3 visualScale = Vector3.one;
        float sparkleTimer;

        void Awake()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) visualScale = sr.transform.localScale;
            startPos = transform.position;
            t = Random.value * 6f;
            sparkleTimer = Random.Range(0.3f, 2f);
        }

        void Update()
        {
            if (taken) return;
            t += Time.deltaTime;
            transform.position = startPos + Vector3.up * (Mathf.Sin(t * bobSpeed) * bobAmplitude);
            float pulse = 1f + Mathf.Sin(t * 3f) * 0.06f;
            transform.localScale = new Vector3(pulse, pulse, 1f);

            if (sr == null) return;
            Transform v = sr.transform;
            if (v == transform) return;
            switch (type)
            {
                case CollectibleType.Coin:
                    // giro falso de moneda (escala X por coseno, sin voltear del todo)
                    float c = Mathf.Cos(t * 3.4f);
                    float sx = Mathf.Lerp(0.22f, 1f, Mathf.Abs(c));
                    v.localScale = new Vector3(visualScale.x * sx, visualScale.y, visualScale.z);
                    sr.flipX = c < 0f;
                    break;
                case CollectibleType.Crystal:
                case CollectibleType.Key:
                    v.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 2.1f) * 9f);
                    break;
            }

            // destello ocasional
            sparkleTimer -= Time.deltaTime;
            if (sparkleTimer <= 0f)
            {
                sparkleTimer = Random.Range(1.4f, 2.6f);
                if (sr.isVisible)
                    Fx.Burst(transform.position + (Vector3)(Random.insideUnitCircle * 0.25f), GlowColor(), 1, 0.6f, 0.13f, 0.5f);
            }
        }

        Color GlowColor()
        {
            switch (type)
            {
                case CollectibleType.Coin: return new Color(1f, 0.88f, 0.35f, 1f);
                case CollectibleType.Crystal: return new Color(0.55f, 0.85f, 1f, 1f);
                case CollectibleType.Heart: return new Color(1f, 0.45f, 0.55f, 1f);
                case CollectibleType.Key: return new Color(1f, 0.95f, 0.55f, 1f);
                default: return new Color(0.6f, 1f, 0.6f, 1f);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken) return;
            if (other.GetComponentInParent<PlayerController2D>() == null) return;
            taken = true;
            Apply();
            int n = type == CollectibleType.Key ? 16 : (type == CollectibleType.Coin ? 7 : 11);
            Fx.Burst(transform.position, GlowColor(), n, type == CollectibleType.Key ? 5f : 3.5f, 0.15f, 0.45f);
            if (type == CollectibleType.Key) CameraFollow2D.Shake(0.1f, 0.15f);
            StartCoroutine(PopRoutine());
        }

        void Apply()
        {
            PlayerStats s = PlayerStats.Instance;
            switch (type)
            {
                case CollectibleType.Coin:
                    if (s != null) s.AddCoins(amount);
                    AudioManager.PlaySfx("coin", 0.8f);
                    break;

                case CollectibleType.Crystal:
                    if (s != null) s.AddCrystals(amount * 10);
                    AudioManager.PlaySfx("crystal", 0.8f);
                    break;

                case CollectibleType.Heart:
                    if (s != null) s.AddHearts(amount);
                    AudioManager.PlaySfx("heart", 0.9f);
                    break;

                case CollectibleType.Key:
                    if (s != null)
                    {
                        s.AddKey();
                        GameEvents.RaiseMessage("¡Llave mágica conseguida! (" + s.keys + "/3)");
                    }
                    AudioManager.PlaySfx("key", 1f);
                    break;

                case CollectibleType.Xp:
                    if (s != null) s.AddXp(amount * 15);
                    AudioManager.PlaySfx("xp", 0.8f);
                    break;
            }
        }

        IEnumerator PopRoutine()
        {
            float t2 = 0f;
            const float dur = 0.25f;
            Vector3 s0 = transform.localScale;
            while (t2 < dur)
            {
                t2 += Time.deltaTime;
                float k = t2 / dur;
                transform.localScale = s0 * (1f + k * 0.8f);
                transform.position += Vector3.up * (1.6f * Time.deltaTime);
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f - k;
                    sr.color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}

