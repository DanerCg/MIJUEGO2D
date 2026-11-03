using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Capa de parallax horizontal infinita con UN solo SpriteRenderer en modo Tiled.
    /// El ancho del renderer se ajusta a un número entero de losetas que cubre siempre
    /// la vista + una loseta; al desplazarse se envuelve exactamente una loseta, de modo
    /// que no hay saltos, huecos ni objetos nuevos. Requiere una textura que repita en
    /// horizontal sin costura (ParallaxBuilder la genera en espejo).
    ///
    /// scroll  = velocidad aparente respecto al suelo (0 = pegada a la pantalla, 1 = suelo).
    /// scrollY = igual en vertical. lockToCameraY mantiene la capa centrada en la cámara.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(100)] // después de CameraFollow2D: sin temblores de un frame
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float scroll = 0.15f;
        [Range(0f, 1f)] public float scrollY = 0.03f;
        [Tooltip("Si está activo, Y = cámara + offsetY (con un pequeño parallax vertical). " +
                 "Si no, la capa queda anclada al mundo en vertical.")]
        public bool lockToCameraY = true;
        public float offsetY = 1f;
        [Tooltip("Desplazamiento vertical máximo por parallax cuando lockToCameraY está activo.")]
        public float maxYShift = 0.4f;
        public float phase;

        Camera cam;
        SpriteRenderer sr;
        float startCamY;
        float worldTileW;
        float lastAspect = -1f;
        float lastOrtho = -1f;
        float worldY;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            worldY = transform.position.y;
        }

        void Start()
        {
            cam = Camera.main;
            if (cam != null) startCamY = cam.transform.position.y;
            Resize();
            Apply();
        }

        void Resize()
        {
            if (cam == null || sr.sprite == null) return;
            lastAspect = cam.aspect;
            lastOrtho = cam.orthographicSize;

            float localTileW = sr.sprite.bounds.size.x;
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            if (localTileW <= 0f || scaleX <= 0f) return;
            worldTileW = localTileW * scaleX;

            float viewW = cam.orthographicSize * 2f * cam.aspect;
            int tiles = Mathf.CeilToInt(viewW / worldTileW) + 1;
            sr.size = new Vector2(localTileW * tiles, sr.sprite.bounds.size.y);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            if (!Mathf.Approximately(cam.aspect, lastAspect) ||
                !Mathf.Approximately(cam.orthographicSize, lastOrtho)) Resize();
            Apply();
        }

        void Apply()
        {
            if (cam == null || worldTileW <= 0f) return;
            Vector3 c = cam.transform.position;
            float viewW = cam.orthographicSize * 2f * cam.aspect;
            float width = sr.size.x * Mathf.Abs(transform.lossyScale.x);

            // borde izquierdo: siempre a la izquierda de la vista, como mucho una loseta
            float left = c.x - viewW * 0.5f - Mathf.Repeat(c.x * scroll + phase, worldTileW);
            float x = left + width * 0.5f;

            float y;
            if (lockToCameraY)
            {
                float shift = Mathf.Clamp((c.y - startCamY) * scrollY, -maxYShift, maxYShift);
                y = c.y + offsetY - shift;
            }
            else
            {
                y = worldY;
            }

            transform.position = new Vector3(x, y, transform.position.z);
        }
    }
}
