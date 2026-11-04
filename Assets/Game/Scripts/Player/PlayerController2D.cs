using System.Collections;
using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Controlador del jugador: aceleración/frenado, correr, salto variable,
    /// coyote time, jump buffering, ataque cuerpo a cuerpo, proyectil mágico,
    /// daño con invulnerabilidad y retroceso. También resuelve el pisotón.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Movimiento")]
        public float walkSpeed = 6.5f;
        public float runSpeed = 10.5f;
        public float acceleration = 80f;
        public float deceleration = 95f;
        public float airControl = 0.6f;

        [Header("Salto")]
        public float jumpSpeed = 16.5f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float gravityRise = 2.6f;
        public float gravityFall = 5.6f;
        public float jumpCutMultiplier = 0.45f;
        public float maxFallSpeed = 28f;

        [Header("Combate")]
        public int attackDamage = 1;
        public float attackRadius = 1.0f;
        public float attackCooldown = 0.3f;
        public LayerMask enemyLayers = ~0;
        public Transform attackOrigin;
        public MagicProjectile projectilePrefab;
        public float castCooldown = 0.45f;

        [Header("Suelo")]
        public Transform groundCheck;
        public float groundCheckRadius = 0.16f;
        public LayerMask groundLayers;

        [Header("Referencias")]
        public PlayerVisuals visuals;

        public bool IsGrounded { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCelebrating { get; private set; }
        public bool IsInvulnerable { get { return invulnTimer > 0f; } }
        public int Facing { get; private set; }

        Rigidbody2D rb;
        float coyoteTimer;
        float bufferTimer;
        float attackTimer;
        float castTimer;
        float invulnTimer;
        bool jumpCutUsed;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Facing = 1;
            if (visuals == null) visuals = GetComponentInChildren<PlayerVisuals>();
            if (PlayerStats.Instance == null) gameObject.AddComponent<PlayerStats>();
        }

        void Start()
        {
            if (GameManager.HasCheckpoint)
            {
                transform.position = (Vector3)GameManager.CheckpointPosition + Vector3.up * 0.1f;
            }

            AudioManager.PlayMusic("Music_level");
        }

        void Update()
        {
            if (IsDead || IsCelebrating)
            {
                if (IsCelebrating)
                {
                    GroundDetection();
                    Vector2 cv = rb.linearVelocity;
                    cv.x = Mathf.MoveTowards(cv.x, 0f, deceleration * Time.deltaTime);
                    rb.linearVelocity = cv;
                    if (visuals != null) visuals.SetMotion(0f, IsGrounded, cv.y);
                }
                InputHub.Tick();
                return;
            }

            Timers();
            GroundDetection();
            HorizontalMovement();
            JumpLogic();
            AttackLogic();
            VisualUpdate();
            InputHub.Tick();
        }

        void Timers()
        {
            float dt = Time.deltaTime;
            if (attackTimer > 0f) attackTimer -= dt;
            if (castTimer > 0f) castTimer -= dt;
            if (invulnTimer > 0f)
            {
                invulnTimer -= dt;
                float a = Mathf.PingPong(Time.time * 14f, 1f) > 0.5f ? 1f : 0.35f;
                if (visuals != null) visuals.SetAlpha(a);
                if (invulnTimer <= 0f && visuals != null) visuals.SetAlpha(1f);
            }
            if (bufferTimer > 0f) bufferTimer -= dt;
        }

        void GroundDetection()
        {
            Vector2 point = groundCheck != null
                ? (Vector2)groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.05f;

            bool overlap = Physics2D.OverlapCircle(point, groundCheckRadius, groundLayers) != null;
            IsGrounded = overlap && rb.linearVelocity.y <= 0.6f;
            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                jumpCutUsed = false;
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }
        }

        void HorizontalMovement()
        {
            float input = InputHub.Horizontal;
            bool running = InputHub.RunHeld;
            float targetSpeed = input * (running ? runSpeed : walkSpeed);
            float rate = Mathf.Abs(input) > 0.01f ? acceleration : deceleration;
            if (!IsGrounded) rate *= airControl;

            Vector2 v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, targetSpeed, rate * Time.deltaTime);

            if (v.y < -maxFallSpeed) v.y = -maxFallSpeed;
            rb.linearVelocity = v;

            if (Mathf.Abs(input) > 0.1f)
            {
                Facing = input > 0f ? 1 : -1;
            }

            rb.gravityScale = v.y < -0.1f ? gravityFall : gravityRise;
        }

        void JumpLogic()
        {
            if (InputHub.JumpPressed) bufferTimer = jumpBufferTime;

            if (bufferTimer > 0f && coyoteTimer > 0f)
            {
                bufferTimer = 0f;
                coyoteTimer = 0f;
                rb.gravityScale = gravityRise;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
                IsGrounded = false;
                AudioManager.PlaySfx("jump", 0.8f);
            }

            if (InputHub.JumpReleased && !jumpCutUsed && rb.linearVelocity.y > 0.5f)
            {
                jumpCutUsed = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        void AttackLogic()
        {
            if (InputHub.AttackPressed && attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                if (visuals != null) visuals.PlayAttack();
                AudioManager.PlaySfx("attack", 0.75f);

                Vector2 center = attackOrigin != null
                    ? (Vector2)attackOrigin.position + new Vector2(Facing * 0.45f, 0f)
                    : (Vector2)transform.position + new Vector2(Facing * 0.9f, 0.9f);

                Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRadius, enemyLayers);
                bool landed = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    EnemyBase enemy = hits[i].GetComponentInParent<EnemyBase>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.TakeDamage(attackDamage, transform.position);
                        landed = true;
                    }
                }

                // estela del golpe; con impacto, chispas + micro-pausa + temblor
                Fx.Spray(center, new Color(1f, 1f, 1f, 0.8f), new Vector2(Facing, 0.15f), 4, 5f, 18f);
                if (landed)
                {
                    Fx.Burst(center, new Color(1f, 0.85f, 0.4f), 8, 5f, 0.15f, 0.3f);
                    CameraFollow2D.Shake(0.12f, 0.12f);
                    Fx.HitStop(0.045f);
                }
            }

            if (InputHub.MagicPressed && castTimer <= 0f)
            {
                if (PlayerStats.Instance != null && PlayerStats.Instance.CanCastMagic && projectilePrefab != null)
                {
                    castTimer = castCooldown;
                    Vector3 spawn = attackOrigin != null ? attackOrigin.position : transform.position + Vector3.up;
                    MagicProjectile proj = Instantiate(projectilePrefab, spawn, Quaternion.identity);
                    proj.Launch(new Vector2(Facing, 0f));
                    if (visuals != null) visuals.PlayCast();
                    Fx.Spray(spawn, new Color(0.55f, 0.85f, 1f), new Vector2(Facing, 0f), 7, 4.5f, 35f);
                    Fx.Burst(spawn, new Color(0.8f, 0.6f, 1f), 5, 2f, 0.2f, 0.3f);
                    AudioManager.PlaySfx("magic", 0.8f);
                }
                else if (PlayerStats.Instance != null && !PlayerStats.Instance.CanCastMagic)
                {
                    GameEvents.RaiseMessage("El proyectil mágico se desbloquea en el nivel 2");
                }
            }
        }

        void VisualUpdate()
        {
            if (visuals == null) return;
            float speed01 = Mathf.InverseLerp(0f, runSpeed, Mathf.Abs(rb.linearVelocity.x));
            visuals.SetFacing(Facing);
            visuals.SetMotion(speed01, IsGrounded, rb.linearVelocity.y);
        }

        public void TakeDamage(int amount, Vector2 from)
        {
            if (IsDead || IsInvulnerable) return;

            PlayerStats stats = PlayerStats.Instance;
            if (stats != null && !stats.RemoveHearts(amount))
            {
                Die();
                return;
            }

            invulnTimer = 1.4f;
            float dir = transform.position.x - from.x;
            dir = Mathf.Abs(dir) < 0.05f ? -Facing : Mathf.Sign(dir);
            rb.linearVelocity = new Vector2(dir * 9f, 11f);
            AudioManager.PlaySfx("damage");
            CameraFollow2D.Shake(0.22f, 0.25f);
            Fx.Burst(transform.position + Vector3.up * 0.9f, new Color(1f, 0.45f, 0.4f), 9, 4f, 0.17f, 0.35f);
            if (visuals != null)
            {
                visuals.PlayHurt();
                StartCoroutine(HurtFlash());
            }
        }

        IEnumerator HurtFlash()
        {
            if (visuals == null) yield break;
            visuals.SetTint(new Color(1f, 0.35f, 0.35f, 1f));
            yield return new WaitForSeconds(0.18f);
            if (visuals != null) visuals.ResetTint();
        }

        /// <summary>Pose de celebración: deja de responder a la entrada y frena.</summary>
        public void Celebrate()
        {
            if (IsDead || IsCelebrating) return;
            IsCelebrating = true;
            invulnTimer = 0f;
            if (visuals != null)
            {
                visuals.SetAlpha(1f);
                visuals.PlayVictory();
            }
        }

        public void Bounce()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed * 0.62f);
            jumpCutUsed = true;
        }

        public void Heal(int amount)
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.AddHearts(amount);
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            if (visuals != null) visuals.PlayDeath();
            CameraFollow2D.Shake(0.3f, 0.35f);
            AudioManager.PlaySfx("defeat");
            GameEvents.RaisePlayerDied();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector2 point = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
            Gizmos.DrawWireSphere(point, groundCheckRadius);

            Gizmos.color = Color.red;
            Vector2 center = attackOrigin != null
                ? (Vector2)attackOrigin.position + new Vector2(Facing * 0.45f, 0f)
                : (Vector2)transform.position + new Vector2(Facing * 0.9f, 0.9f);
            Gizmos.DrawWireSphere(center, attackRadius);
        }
    }
}
