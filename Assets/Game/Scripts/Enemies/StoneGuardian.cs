using UnityEngine;

namespace Rescate
{
    /// <summary>Guardián de piedra: lento, muy resistente, no se puede pisar.</summary>
    public class StoneGuardian : EnemyBase
    {
        public float speed = 1.3f;
        public float edgeCheck = 0.9f;
        public float wallCheck = 0.6f;
        public LayerMask groundLayers;
        public float chargeSpeed = 3.5f;
        public float detectRange = 7f;

        int dir = 1;
        float animTime;
        float stepTimer;
        Vector3 baseScale;

        protected override void Awake()
        {
            base.Awake();
            canBeStomped = false;
            baseScale = transform.localScale;
        }

        protected override void OnDamaged(Vector2 from)
        {
            // Los guardianes no retroceden: son pesados.
            StartCoroutine(ShakeRoutine());
        }

        System.Collections.IEnumerator ShakeRoutine()
        {
            Vector3 basePos = transform.position;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                transform.position = basePos + (Vector3)(Random.insideUnitCircle * 0.06f);
                yield return null;
            }
            transform.position = basePos;
        }

        void FixedUpdate()
        {
            if (IsDead) return;

            Vector2 pos = transform.position;
            float dist = player != null ? Mathf.Abs(player.transform.position.x - pos.x) : 999f;
            float moveSpeed = speed;

            if (dist < detectRange && player != null && Mathf.Abs(player.transform.position.y - pos.y) < 3f)
            {
                int toward = player.transform.position.x > pos.x ? 1 : -1;
                if (toward != dir) dir = toward;
                moveSpeed = chargeSpeed;
            }
            else
            {
                bool wall = Physics2D.Raycast(pos + new Vector2(0f, 0.6f), Vector2.right * dir, wallCheck, groundLayers);
                bool floorAhead = Physics2D.Raycast(pos + new Vector2(dir * edgeCheck, 0.4f), Vector2.down, 1.4f, groundLayers);
                if (wall || !floorAhead) dir = -dir;
            }

            if (rb != null) rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        }

        void Update()
        {
            if (IsDead) return;
            animTime += Time.deltaTime;
            stepTimer += Time.deltaTime;

            float step = Mathf.Abs(Mathf.Sin(stepTimer * 5f)) * 0.05f;
            transform.localScale = new Vector3(baseScale.x * (1f + step * 0.5f), baseScale.y * (1f - step), baseScale.z);
            if (sr != null) sr.flipX = dir < 0;
        }
    }
}
