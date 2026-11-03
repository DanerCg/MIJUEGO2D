using UnityEngine;

namespace Rescate
{
    /// <summary>Proyectil mágico enemigo (Maldrak).</summary>
    public class EnemyProjectile : MonoBehaviour
    {
        public float speed = 9f;
        public int damage = 1;
        public float lifeTime = 3.5f;
        public LayerMask groundLayers;

        Vector2 direction = Vector2.left;
        float life;

        public void Launch(Vector2 dir, float overrideSpeed = -1f)
        {
            direction = dir.sqrMagnitude < 0.01f ? Vector2.left : dir.normalized;
            if (overrideSpeed > 0f) speed = overrideSpeed;
            life = lifeTime;
        }

        void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            transform.Rotate(0f, 0f, 360f * Time.deltaTime);

            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);

            if (Physics2D.OverlapCircle(transform.position, 0.28f, groundLayers) != null)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController2D p = other.GetComponentInParent<PlayerController2D>();
            if (p != null && !p.IsDead)
            {
                p.TakeDamage(damage, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
