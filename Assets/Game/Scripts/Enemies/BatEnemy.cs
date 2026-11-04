using UnityEngine;

namespace Rescate
{
    /// <summary>Murciélago sombrío: patrulla en el aire y persigue al jugador si se acerca.</summary>
    public class BatEnemy : EnemyBase
    {
        public float patrolSpeed = 2.6f;
        public float chaseSpeed = 4.6f;
        public float patrolRadiusX = 3f;
        public float patrolRadiusY = 1.2f;
        public float detectRange = 6.5f;

        Vector2 origin;
        float t;
        Vector3 baseScale;

        protected override void Awake()
        {
            base.Awake();
            canBeStomped = false;
            baseScale = transform.localScale;
        }

        protected override void Start()
        {
            base.Start();
            origin = transform.position;
        }

        void Update()
        {
            if (IsDead) return;
            t += Time.deltaTime;

            Vector2 pos = transform.position;
            float dist = player != null ? Vector2.Distance(player.transform.position, pos) : 999f;

            if (dist < detectRange && player != null)
            {
                Vector2 chaseTarget = (Vector2)player.transform.position + new Vector2(0f, 1f);
                transform.position = Vector2.MoveTowards(pos, chaseTarget, chaseSpeed * Time.deltaTime);
                if (sr != null) sr.flipX = chaseTarget.x < pos.x;
            }
            else
            {
                Vector2 target = origin + new Vector2(Mathf.Cos(t * 0.9f) * patrolRadiusX, Mathf.Sin(t * 1.8f) * patrolRadiusY);
                transform.position = Vector2.MoveTowards(pos, target, patrolSpeed * Time.deltaTime);
                if (sr != null) sr.flipX = target.x < pos.x;
            }

            float flap = 1f + Mathf.Sin(t * 16f) * 0.13f;
            transform.localScale = new Vector3(baseScale.x * flap, baseScale.y * (1f - (flap - 1f) * 0.5f), baseScale.z);
        }
    }
}
