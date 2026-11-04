using System.Collections;
using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Jefe final Maldrak: tres fases de combate (mágia simple, más rápida con
    /// desplazamientos y ataques combinados). Al morir abre la prisión de Elena.
    /// </summary>
    public class BossMaldrak : EnemyBase
    {
        [Header("Jefe")]
        public EnemyProjectile projectilePrefab;
        public Transform firePoint;
        public GameObject prisonBarrier;
        public PrincessElena princess;
        public float arenaMinX;
        public float arenaMaxX = 10f;
        public float floatAmplitude = 0.4f;

        public int Phase { get; private set; }

        float attackTimer = 2.5f;
        float originY;
        float t;

        protected override void Awake()
        {
            maxHealth = 30;
            damageToPlayer = 1;
            xpReward = 250;
            coinsReward = 25;
            canBeStomped = false;
            Phase = 1;
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            originY = transform.position.y;
            AudioManager.PlayMusic("Music_boss");
            GameEvents.RaiseMessage("¡MALDRAK! Derrota al hechicero para liberar a Elena");
        }

        void Update()
        {
            if (IsDead) return;
            t += Time.deltaTime;
            UpdatePhase();

            Vector3 p = transform.position;
            p.y = originY + Mathf.Sin(t * 2.2f) * floatAmplitude;

            if (player != null)
            {
                float targetX = Mathf.Clamp(player.transform.position.x, arenaMinX, arenaMaxX);
                float spd = Phase == 1 ? 1.6f : (Phase == 2 ? 2.9f : 4.3f);
                p.x = Mathf.MoveTowards(p.x, targetX, spd * Time.deltaTime);
                if (sr != null) sr.flipX = player.transform.position.x < p.x;
            }

            p.x = Mathf.Clamp(p.x, arenaMinX, arenaMaxX);
            transform.position = p;

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) Attack();
        }

        void UpdatePhase()
        {
            float frac = (float)health / Mathf.Max(1, maxHealth);
            int next = frac > 0.66f ? 1 : (frac > 0.33f ? 2 : 3);
            if (next == Phase) return;

            Phase = next;
            AudioManager.PlaySfx("bossPhase");
            CameraFollow2D.Shake(0.3f, 0.45f);
            Fx.Burst(sr != null ? sr.bounds.center : transform.position, new Color(0.8f, 0.45f, 1f), 24, 6f, 0.2f, 0.6f);
            GameEvents.RaiseMessage("Maldrak entra en la FASE " + Phase);
            StartCoroutine(PhaseFlash());
        }

        IEnumerator PhaseFlash()
        {
            if (sr == null) yield break;
            Color c = sr.color;
            sr.color = Color.white;
            yield return new WaitForSeconds(0.12f);
            if (sr != null) sr.color = c;
        }

        void Attack()
        {
            attackTimer = Phase == 1 ? 2.2f : (Phase == 2 ? 1.5f : 1.0f);
            if (projectilePrefab == null || player == null) return;

            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position + Vector2.up * 0.6f;
            Vector2 baseDir = ((Vector2)player.transform.position + Vector2.up * 0.8f - origin);
            if (baseDir.sqrMagnitude < 0.01f) baseDir = Vector2.down;
            baseDir.Normalize();

            int count = Phase == 1 ? 1 : (Phase == 2 ? 3 : 5);
            float spread = Phase == 1 ? 0f : (Phase == 2 ? 18f : 30f);
            float speed = Phase == 3 ? 13f : 9f;

            for (int i = 0; i < count; i++)
            {
                float angle = spread * (i - (count - 1) * 0.5f);
                Vector2 dir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)baseDir);
                EnemyProjectile proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
                proj.Launch(dir, speed);
            }
            Fx.Spray(origin, new Color(0.85f, 0.4f, 1f), baseDir, 6, 3.5f, 40f);

            AudioManager.PlaySfx("bossCast", 0.85f);
        }

        protected override void Die()
        {
            if (IsDead) return;
            base.Die();

            if (prisonBarrier != null) prisonBarrier.SetActive(false);
            if (princess != null) princess.Free();
            CameraFollow2D.Shake(0.4f, 0.7f);
            Fx.Burst(sr != null ? sr.bounds.center : transform.position, new Color(1f, 0.8f, 0.4f), 40, 8f, 0.24f, 0.9f);

            AudioManager.PlaySfx("victory");
            AudioManager.StopMusic();
            GameEvents.RaiseBossDefeated();
            GameEvents.RaiseMessage("¡Maldrak ha caído! La princesa Elena te espera");
        }
    }
}
