using UnityEngine;

namespace Rescate
{
    /// <summary>Princesa Elena: liberada tras derrotar a Maldrak. Al tocarla, victoria.</summary>
    public class PrincessElena : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public GameObject prisonBars;

        public bool Freed { get; private set; }

        Vector3 basePos;
        float t;

        void Start()
        {
            basePos = transform.position;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (Freed)
            {
                transform.position = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(t * 2f)) * 0.35f;
                if (spriteRenderer != null)
                {
                    float g = 0.85f + Mathf.Sin(t * 4f) * 0.15f;
                    spriteRenderer.color = new Color(1f, g, 1f, 1f);
                }
            }
        }

        public void Free()
        {
            if (Freed) return;
            Freed = true;
            if (prisonBars != null) prisonBars.SetActive(false);
            AudioManager.PlaySfx("princess");
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!Freed || celebrating) return;
            PlayerController2D p = other.GetComponentInParent<PlayerController2D>();
            if (p == null) return;
            celebrating = true;
            p.Celebrate();
            Fx.Burst(transform.position + Vector3.up * 1.4f, new Color(1f, 0.6f, 0.9f), 20, 4.5f, 0.18f, 0.8f);
            StartCoroutine(VictoryAfterCelebration());
        }

        bool celebrating;

        System.Collections.IEnumerator VictoryAfterCelebration()
        {
            // breve celebración en el propio nivel antes de la escena de victoria
            yield return new WaitForSeconds(1.6f);
            if (GameManager.Instance != null) GameManager.Instance.LoadVictory();
        }
    }
}
