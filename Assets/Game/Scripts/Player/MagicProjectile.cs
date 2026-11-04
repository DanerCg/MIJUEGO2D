using System.Collections.Generic;
using UnityEngine;

namespace Rescate
{
    /// <summary>Proyectil mágico del jugador (se desbloquea en el nivel 2).</summary>
    public class MagicProjectile : MonoBehaviour
    {
        public float speed = 16f;
        public float lifeTime = 1.6f;
        public int damage = 2;
        public LayerMask hitLayers;

        Vector2 direction = Vector2.right;
        float life;
        bool launched;

        public void Launch(Vector2 dir)
        {
            direction = dir.sqrMagnitude < 0.01f ? Vector2.right : dir.normalized;
            launched = true;
            life = lifeTime;
            transform.localScale = new Vector3(dir.x < 0f ? -Mathf.Abs(transform.localScale.x) : Mathf.Abs(transform.localScale.x),
                                               transform.localScale.y, transform.localScale.z);
        }

        void Update()
        {
            if (!launched) return;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            transform.Rotate(0f, 0f, 540f * Time.deltaTime);

            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(damage, transform.position);
                AudioManager.PlaySfx("hit", 0.6f);
                Destroy(gameObject);
                return;
            }

            if (((1 << other.gameObject.layer) & hitLayers) != 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
