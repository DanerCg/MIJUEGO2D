using UnityEngine;

namespace Rescate
{
    /// <summary>Obstáculo que daña al jugador (pinchos, lava, trampas).</summary>
    public class Hazard : MonoBehaviour
    {
        public int damage = 1;

        void OnTriggerEnter2D(Collider2D other) { Hit(other); }
        void OnTriggerStay2D(Collider2D other) { Hit(other); }

        void Hit(Collider2D other)
        {
            PlayerController2D p = other.GetComponentInParent<PlayerController2D>();
            if (p == null || p.IsDead) return;
            p.TakeDamage(damage, transform.position);
        }
    }
}
