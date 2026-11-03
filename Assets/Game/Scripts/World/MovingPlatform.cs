using UnityEngine;

namespace Rescate
{
    /// <summary>Plataforma móvil cinemática; transporta al jugador que se sube a ella.</summary>
    public class MovingPlatform : MonoBehaviour
    {
        public Vector2 offset = new Vector2(0f, 3f);
        public float speed = 0.35f;

        Rigidbody2D rb;
        Vector2 a;
        Vector2 b;
        float t;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            a = transform.position;
            b = a + offset;
        }

        void FixedUpdate()
        {
            if (rb == null) return;
            t += Time.fixedDeltaTime * speed;
            float k = Mathf.PingPong(t, 1f);
            rb.MovePosition(Vector2.Lerp(a, b, Mathf.SmoothStep(0f, 1f, k)));
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponentInParent<PlayerController2D>() != null)
            {
                collision.transform.SetParent(transform);
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.GetComponentInParent<PlayerController2D>() != null)
            {
                collision.transform.SetParent(null);
            }
        }
    }
}
