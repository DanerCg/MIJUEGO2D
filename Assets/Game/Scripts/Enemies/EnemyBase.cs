using System.Collections;
using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Base de todos los enemigos: vida, daño por contacto, pisotón, recompensas y muerte.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Combate")]
        public int maxHealth = 2;
        public int damageToPlayer = 1;
        public float contactCooldown = 0.4f;

        [Header("Recompensas")]
        public int xpReward = 25;
        public int coinsReward = 1;

        [Header("Pisotón")]
        public bool canBeStomped = true;

        public bool IsDead { get; protected set; }

        protected int health;
        protected SpriteRenderer sr;
        protected Rigidbody2D rb;
        protected Collider2D col;
        protected PlayerController2D player;
        protected Color baseColor = Color.white;

        protected virtual void Awake()
        {
            health = maxHealth;
            sr = GetComponentInChildren<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            if (sr != null) baseColor = sr.color;
            if (sr != null) visualBaseScale = sr.transform.localScale;
        }

        Vector3 visualBaseScale = Vector3.one;

        protected virtual void Start()
        {
            player = FindFirstObjectByType<PlayerController2D>();
        }

        public virtual void TakeDamage(int amount, Vector2 from)
        {
            if (IsDead) return;
            health -= Mathf.Max(1, amount);
            StartCoroutine(HitFlash());
            AudioManager.PlaySfx("hit", 0.65f);

            if (health <= 0) Die();
            else OnDamaged(from);
        }

        protected virtual void OnDamaged(Vector2 from)
        {
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                float dir = Mathf.Sign(transform.position.x - from.x);
                rb.linearVelocity = new Vector2(dir * 5f, rb.linearVelocity.y + 2.5f);
            }
        }

        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
            if (col != null) col.enabled = false;
            AudioManager.PlaySfx("enemyDeath", 0.7f);
            Vector3 c = sr != null ? sr.bounds.center : transform.position;
            Fx.Burst(c, new Color(1f, 0.95f, 0.75f), 12, 5f, 0.18f, 0.45f);
            Fx.Dust(transform.position + Vector3.up * 0.1f, 6, 1f);
            CameraFollow2D.Shake(0.12f, 0.15f);

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddCoins(coinsReward);
                PlayerStats.Instance.AddXp(xpReward);
            }

            StartCoroutine(DeathRoutine());
        }

        IEnumerator HitFlash()
        {
            if (sr == null) yield break;
            Transform v = sr.transform;
            Vector3 vs = visualBaseScale;
            // destello blanco breve -> rojo -> normal, con un "punch" de escala en el visual
            sr.color = new Color(1.6f, 1.6f, 1.6f, 1f);
            if (v != transform) v.localScale = new Vector3(vs.x * 1.18f, vs.y * 0.86f, vs.z);
            yield return new WaitForSeconds(0.045f);
            if (sr == null) yield break;
            sr.color = new Color(1f, 0.4f, 0.4f, 1f);
            if (v != transform) v.localScale = new Vector3(vs.x * 0.94f, vs.y * 1.08f, vs.z);
            yield return new WaitForSeconds(0.06f);
            if (sr == null) yield break;
            sr.color = baseColor;
            if (v != transform) v.localScale = vs;
        }

        IEnumerator DeathRoutine()
        {
            float t = 0f;
            const float dur = 0.28f;
            Vector3 s0 = transform.localScale;
            Color c = baseColor;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                transform.localScale = new Vector3(s0.x * (1f + 0.35f * k), s0.y * (1f - 0.95f * k), s0.z);
                if (sr != null)
                {
                    c.a = 1f - k;
                    sr.color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnCollisionEnter2D(Collision2D collision) { HandleTouch(collision.collider); }
        void OnCollisionStay2D(Collision2D collision) { HandleTouch(collision.collider); }
        void OnTriggerEnter2D(Collider2D other) { HandleTouch(other); }
        void OnTriggerStay2D(Collider2D other) { HandleTouch(other); }

        void HandleTouch(Collider2D other)
        {
            if (IsDead || other == null) return;

            PlayerController2D p = other.GetComponentInParent<PlayerController2D>();
            if (p == null || p.IsDead) return;

            if (canBeStomped && IsStomp(p))
            {
                TakeDamage(9999, p.transform.position);
                p.Bounce();
                AudioManager.PlaySfx("stomp", 0.85f);
                return;
            }

            p.TakeDamage(damageToPlayer, transform.position);
        }

        bool IsStomp(PlayerController2D p)
        {
            Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
            if (prb == null) return false;
            if (prb.linearVelocity.y > -1.5f) return false;
            return p.transform.position.y > transform.position.y + 0.35f;
        }
    }
}
