using UnityEngine;

namespace Rescate
{
    /// <summary>Slimo: criatura que patrulla plataformas y gira ante muros o precipicios.</summary>
    public class SlimeEnemy : EnemyBase
    {
        public float speed = 2.2f;
        public float edgeCheck = 0.75f;
        public float wallCheck = 0.45f;
        public LayerMask groundLayers;

        int dir = 1;
        float animTime;
        Vector3 baseScale;

        protected override void Awake()
        {
            base.Awake();
            baseScale = transform.localScale;
        }

        void FixedUpdate()
        {
            if (IsDead) return;

            Vector2 pos = transform.position;
            Vector2 wallOrigin = pos + new Vector2(0f, 0.35f);
            bool wall = Physics2D.Raycast(wallOrigin, Vector2.right * dir, wallCheck, groundLayers);
            Vector2 floorOrigin = pos + new Vector2(dir * edgeCheck, 0.25f);
            bool floorAhead = Physics2D.Raycast(floorOrigin, Vector2.down, 1.1f, groundLayers);

            if (wall || !floorAhead) dir = -dir;

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            }
            else
            {
                transform.position += new Vector3(dir * speed * Time.fixedDeltaTime, 0f, 0f);
            }
        }

        void Update()
        {
            if (IsDead) return;
            animTime += Time.deltaTime;
            float squash = 1f + Mathf.Sin(animTime * 8f) * 0.08f;
            transform.localScale = new Vector3(baseScale.x * (2f - squash), baseScale.y * squash, baseScale.z);
            if (sr != null) sr.flipX = dir < 0;
        }
    }
}
