using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Cámara 2D lateral: seguimiento suave con adelanto, límites de nivel,
    /// adaptación a cualquier relación de aspecto y temblor de pantalla (Shake).
    /// El temblor se suma DESPUÉS del suavizado y se vuelve a limitar, de modo que
    /// nunca muestra zonas fuera del escenario.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.16f;
        public float verticalOffset = 1.4f;
        public float lookAhead = 1.6f;
        public float lookAheadSmoothing = 4f;
        public bool useBounds = true;
        public Vector2 boundsMin = new Vector2(-5f, -5f);
        public Vector2 boundsMax = new Vector2(50f, 20f);

        static CameraFollow2D active;

        Camera cam;
        Vector3 velocity;
        float currentLookAhead;
        Vector3 basePos;
        bool hasBase;

        float shakeAmp;
        float shakeTime;
        float shakeDuration;

        /// <summary>Temblor breve. amplitude en unidades de mundo (0.1 – 0.35 recomendado).</summary>
        public static void Shake(float amplitude, float duration)
        {
            if (active == null) return;
            if (amplitude >= active.shakeAmp * (active.shakeDuration > 0f ? active.shakeTime / active.shakeDuration : 0f))
            {
                active.shakeAmp = amplitude;
                active.shakeDuration = Mathf.Max(0.01f, duration);
                active.shakeTime = active.shakeDuration;
            }
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            active = this;
            if (target == null)
            {
                PlayerController2D p = FindFirstObjectByType<PlayerController2D>();
                if (p != null) target = p.transform;
            }
        }

        void OnEnable() { active = this; }
        void OnDisable() { if (active == this) active = null; }

        void LateUpdate()
        {
            if (target == null)
            {
                PlayerController2D p = FindFirstObjectByType<PlayerController2D>();
                if (p == null) return;
                target = p.transform;
            }
            if (!hasBase) { basePos = transform.position; hasBase = true; }

            float desiredLook = 0f;
            Rigidbody2D trb = target.GetComponent<Rigidbody2D>();
            if (trb != null)
            {
                desiredLook = Mathf.Clamp(trb.linearVelocity.x * 0.22f, -lookAhead, lookAhead);
            }

            currentLookAhead = Mathf.Lerp(currentLookAhead, desiredLook, 1f - Mathf.Exp(-lookAheadSmoothing * Time.deltaTime));

            Vector3 desired = new Vector3(target.position.x + currentLookAhead,
                                          target.position.y + verticalOffset,
                                          transform.position.z);
            desired = Clamp(desired);

            basePos = Vector3.SmoothDamp(basePos, desired, ref velocity, smoothTime);

            Vector3 final = basePos;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDuration);
                float a = shakeAmp * k * k;
                float t = Time.unscaledTime * 38f;
                final.x += (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * a;
                final.y += (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * a;
                if (shakeTime <= 0f) shakeAmp = 0f;
            }

            transform.position = Clamp(final);
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!useBounds || cam == null) return p;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = boundsMin.x + halfW;
            float maxX = boundsMax.x - halfW;
            float minY = boundsMin.y + halfH;
            float maxY = boundsMax.y - halfH;

            p.x = minX > maxX ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? (boundsMin.y + boundsMax.y) * 0.5f : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 p = Clamp(new Vector3(target.position.x, target.position.y + verticalOffset, transform.position.z));
            transform.position = p;
            basePos = p;
            hasBase = true;
            velocity = Vector3.zero;
        }
    }
}
