using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Presentación del protagonista sin Animator:
    ///  - poses dibujadas (idle, ciclo de carrera, salto, caída, golpe, daño, victoria)
    ///    elegidas por estado, con el ciclo de carrera sincronizado a la velocidad real;
    ///  - capa procedural SUTIL encima (respiración, rebote de pasos, inclinación,
    ///    estirado al despegar, aplastado al aterrizar, anticipación/golpe/recuperación);
    ///  - sombra que se pega al suelo y polvo en pasos, salto y aterrizaje.
    /// Si falta alguna pose se usa la de idle, así que nunca queda sin sprite.
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public Transform visualRoot;

        [Header("Poses")]
        public Sprite idleSprite;
        public Sprite[] runSprites;
        public Sprite jumpSprite;
        public Sprite fallSprite;
        public Sprite attackSprite;
        public Sprite hurtSprite;
        public Sprite victorySprite;

        [Header("Sombra")]
        public SpriteRenderer shadow;
        public LayerMask groundLayers = 1 << 8;
        public float shadowWidth = 0.95f;

        [Header("Ritmo")]
        [Tooltip("Distancia recorrida (u) por cada ciclo completo de carrera.")]
        public float strideLength = 2.3f;

        // ---- estado
        Vector3 baseScale = Vector3.one;
        Vector3 baseLocalPos;
        Color baseColor = Color.white;
        int facing = 1;

        float time;
        float speed01;
        bool grounded = true;
        bool wasGrounded = true;
        float vy;
        float minAirVy;

        float runPhase;
        int lastRunFrame = -1;

        float landTimer;      // aplastado al aterrizar
        float takeoffTimer;   // estirado al despegar
        float attackTimer = -1f;
        bool attackIsCast;
        float hurtTimer;
        bool dead;
        float deadTime;
        bool victory;
        float victoryTime;

        // suavizado
        Vector3 curScale;
        float curLean;
        Vector2 curOffset;

        const float AttackAnticipation = 0.05f;
        const float AttackStrike = 0.14f;
        const float AttackRecover = 0.12f;
        const float HurtDuration = 0.38f;

        static Sprite shadowSprite;

        void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
                if (idleSprite == null) idleSprite = spriteRenderer.sprite;
            }
            baseScale = visualRoot.localScale;
            baseLocalPos = visualRoot.localPosition;
            curScale = baseScale;
            EnsureShadow();
        }

        // ------------------------------------------------------------ entrada desde el controlador

        public void SetMotion(float speed, bool isGrounded, float verticalSpeed)
        {
            speed01 = speed;
            vy = verticalSpeed;
            grounded = isGrounded;
        }

        public void SetFacing(int dir)
        {
            if (dir == 0) return;
            facing = dir > 0 ? 1 : -1;
            if (spriteRenderer != null) spriteRenderer.flipX = facing < 0;
        }

        public void PlayAttack() { attackTimer = 0f; attackIsCast = false; }

        public void PlayCast()
        {
            attackTimer = 0f;
            attackIsCast = true;
        }

        public void PlayHurt()
        {
            hurtTimer = HurtDuration;
            attackTimer = -1f;
        }

        public void PlayDeath()
        {
            dead = true;
            deadTime = 0f;
        }

        public void PlayVictory()
        {
            victory = true;
            victoryTime = 0f;
            Fx.Burst(transform.position + Vector3.up * 1.6f, new Color(1f, 0.9f, 0.35f), 18, 5f, 0.18f, 0.7f);
        }

        public void SetAlpha(float a)
        {
            if (spriteRenderer == null) return;
            Color c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;
        }

        public void SetTint(Color tint)
        {
            if (spriteRenderer == null) return;
            Color c = tint;
            c.a = spriteRenderer.color.a;
            spriteRenderer.color = c;
        }

        public void ResetTint()
        {
            if (spriteRenderer == null) return;
            Color c = baseColor;
            c.a = spriteRenderer.color.a;
            spriteRenderer.color = c;
        }

        // ------------------------------------------------------------ animación

        void Update()
        {
            if (visualRoot == null) return;
            float dt = Time.deltaTime;
            time += dt;

            DetectAirTransitions();

            Sprite pose = idleSprite;
            Vector3 s = baseScale;
            float lean = 0f;            // grados; positivo = inclinado hacia delante
            Vector2 off = Vector2.zero; // desplazamiento local (x hacia delante)

            if (dead)
            {
                deadTime += dt;
                pose = hurtSprite != null ? hurtSprite : idleSprite;
                float k = Mathf.Clamp01(deadTime / 0.45f);
                float hop = Mathf.Sin(Mathf.Clamp01(deadTime / 0.5f) * Mathf.PI) * 0.6f;
                lean = -Mathf.SmoothStep(0f, 82f, k);   // cae de espaldas
                off = new Vector2(-0.35f * k, hop + 0.25f * k);
            }
            else if (victory)
            {
                victoryTime += dt;
                pose = victorySprite != null ? victorySprite : idleSprite;
                float hop = Mathf.Abs(Mathf.Sin(victoryTime * 6.5f));
                off = new Vector2(0f, hop * 0.28f);
                float sq = 1f - hop;
                s = Scale(1f + sq * 0.05f, 1f - sq * 0.05f);
            }
            else if (hurtTimer > 0f)
            {
                hurtTimer -= dt;
                pose = hurtSprite != null ? hurtSprite : idleSprite;
                float k = hurtTimer / HurtDuration;
                lean = -10f * k;
                s = Scale(1f + 0.06f * k, 1f - 0.06f * k);
            }
            else if (attackTimer >= 0f)
            {
                attackTimer += dt;
                float a = attackTimer;
                Sprite strike = attackSprite != null ? attackSprite : idleSprite;
                if (a < AttackAnticipation)
                {
                    float k = a / AttackAnticipation;
                    pose = grounded ? idleSprite : (vy > 0f ? jumpSprite : fallSprite);
                    if (pose == null) pose = idleSprite;
                    lean = -6f * k;
                    off = new Vector2(-0.06f * k, 0f);
                    s = Scale(1f + 0.05f * k, 1f - 0.05f * k);
                }
                else if (a < AttackAnticipation + AttackStrike)
                {
                    float k = 1f - (a - AttackAnticipation) / AttackStrike;
                    pose = strike;
                    lean = 7f * k;
                    off = new Vector2((attackIsCast ? 0.06f : 0.14f) * k, 0f);
                    s = Scale(1f + 0.06f * k, 1f - 0.03f * k);
                }
                else if (a < AttackAnticipation + AttackStrike + AttackRecover)
                {
                    pose = strike;
                    float k = (a - AttackAnticipation - AttackStrike) / AttackRecover;
                    lean = Mathf.Lerp(2f, 0f, k);
                }
                else
                {
                    attackTimer = -1f;
                }

                if (attackTimer < 0f) pose = LocomotionPose(ref s, ref lean, ref off, dt);
            }
            else
            {
                pose = LocomotionPose(ref s, ref lean, ref off, dt);
            }

            // aterrizaje / despegue (se suman a cualquier pose)
            if (landTimer > 0f)
            {
                landTimer -= dt;
                float k = Mathf.Clamp01(landTimer / 0.14f);
                s = new Vector3(s.x * (1f + 0.10f * k), s.y * (1f - 0.12f * k), s.z);
            }
            if (takeoffTimer > 0f)
            {
                takeoffTimer -= dt;
                float k = Mathf.Clamp01(takeoffTimer / 0.16f);
                s = new Vector3(s.x * (1f - 0.06f * k), s.y * (1f + 0.08f * k), s.z);
            }

            if (spriteRenderer != null && pose != null && spriteRenderer.sprite != pose)
                spriteRenderer.sprite = pose;

            // suavizado (la escala responde rápido; la inclinación algo más lenta)
            float ks = 1f - Mathf.Exp(-28f * dt);
            float kl = 1f - Mathf.Exp(-16f * dt);
            curScale = Vector3.Lerp(curScale, s, ks);
            curLean = Mathf.Lerp(curLean, lean, dead ? 1f : kl);
            curOffset = Vector2.Lerp(curOffset, off, dead || victory ? 1f : ks);

            visualRoot.localScale = curScale;
            visualRoot.localEulerAngles = new Vector3(0f, 0f, -curLean * facing);
            visualRoot.localPosition = baseLocalPos + new Vector3(curOffset.x * facing, curOffset.y, 0f);

            UpdateShadow();
        }

        Sprite LocomotionPose(ref Vector3 s, ref float lean, ref Vector2 off, float dt)
        {
            if (!grounded)
            {
                if (vy > 1.5f)
                {
                    lean = 3f;
                    return jumpSprite != null ? jumpSprite : RunFrame(0);
                }
                // caída: brazos arriba; cerca del ápice mezcla la pose de salto
                lean = -2f;
                if (vy > -1.5f && jumpSprite != null) return jumpSprite;
                return fallSprite != null ? fallSprite : (jumpSprite != null ? jumpSprite : idleSprite);
            }

            if (speed01 > 0.06f && runSprites != null && runSprites.Length > 0)
            {
                // ciclo ligado a la distancia real recorrida: los pies no patinan
                float worldSpeed = speed01 * 10.5f;
                runPhase += worldSpeed * dt / Mathf.Max(0.5f, strideLength);
                int frames = runSprites.Length;
                float f = Mathf.Repeat(runPhase, 1f) * frames;
                int frame = Mathf.FloorToInt(f) % frames;

                // rebote: arriba en las fases de paso, abajo en los apoyos
                float stepPhase = Mathf.Repeat(runPhase * 2f, 1f);
                float bounce = Mathf.Sin(stepPhase * Mathf.PI);
                off = new Vector2(0f, bounce * 0.07f * Mathf.Lerp(0.6f, 1f, speed01));
                lean = Mathf.Lerp(3f, 8f, speed01);
                s = Scale(1f - 0.025f * bounce, 1f + 0.03f * bounce);

                if (frame != lastRunFrame)
                {
                    lastRunFrame = frame;
                    // polvo en cada apoyo cuando se corre de verdad
                    if (speed01 > 0.7f && (frame % 2 == 0))
                        Fx.Dust(transform.position + new Vector3(-0.15f * facing, 0.05f, 0f), 2, 0.75f, -facing);
                }
                return RunFrame(frame);
            }

            // idle: respiración muy suave
            lastRunFrame = -1;
            runPhase = 0f;
            float b = Mathf.Sin(time * 2.4f);
            s = Scale(1f + b * 0.008f, 1f + b * 0.016f);
            lean = Mathf.Sin(time * 1.1f) * 0.6f;
            return idleSprite;
        }

        Sprite RunFrame(int i)
        {
            if (runSprites == null || runSprites.Length == 0) return idleSprite;
            Sprite sp = runSprites[Mathf.Clamp(i, 0, runSprites.Length - 1)];
            return sp != null ? sp : idleSprite;
        }

        Vector3 Scale(float x, float y)
        {
            return new Vector3(baseScale.x * x, baseScale.y * y, baseScale.z);
        }

        void DetectAirTransitions()
        {
            if (dead) { wasGrounded = grounded; return; }

            if (!grounded) minAirVy = Mathf.Min(minAirVy, vy);

            if (grounded && !wasGrounded)
            {
                float impact = Mathf.InverseLerp(2f, 22f, -minAirVy);
                if (impact > 0.05f)
                {
                    landTimer = 0.14f;
                    Fx.Dust(transform.position + Vector3.up * 0.05f, Mathf.RoundToInt(Mathf.Lerp(3f, 8f, impact)),
                        Mathf.Lerp(0.7f, 1.2f, impact));
                }
                minAirVy = 0f;
            }
            else if (!grounded && wasGrounded && vy > 4f)
            {
                takeoffTimer = 0.16f;
                Fx.Dust(transform.position + Vector3.up * 0.05f, 3, 0.7f);
                minAirVy = 0f;
            }
            wasGrounded = grounded;
        }

        // ------------------------------------------------------------ sombra

        void EnsureShadow()
        {
            if (shadow != null) return;
            if (shadowSprite == null) shadowSprite = BuildShadowSprite();
            GameObject go = new GameObject("Shadow");
            go.transform.SetParent(transform, false);
            shadow = go.AddComponent<SpriteRenderer>();
            shadow.sprite = shadowSprite;
            shadow.sortingOrder = 2; // sobre el suelo, bajo la decoración y el jugador
            shadow.color = new Color(0f, 0f, 0f, 0.32f);
        }

        void UpdateShadow()
        {
            if (shadow == null) return;
            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.2f;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 6f, groundLayers);
            if (!hit || dead)
            {
                shadow.enabled = false;
                return;
            }
            shadow.enabled = true;
            float h = Mathf.Max(0f, transform.position.y - hit.point.y);
            float k = Mathf.Clamp01(1f - h / 5f);
            float w = shadowWidth * Mathf.Lerp(0.45f, 1f, k);
            shadow.transform.position = new Vector3(transform.position.x, hit.point.y + 0.02f, transform.position.z);
            shadow.transform.localScale = new Vector3(w, w * 0.28f, 1f);
            Color c = shadow.color;
            c.a = 0.32f * Mathf.Lerp(0.35f, 1f, k);
            shadow.color = c;
        }

        static Sprite BuildShadowSprite()
        {
            const int W = 64, H = 64;
            Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f) / W * 2f - 1f;
                    float dy = (y + 0.5f) / H * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.SmoothStep(0f, 1f, a * 1.6f);
                    px[y * W + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W);
        }
    }
}
