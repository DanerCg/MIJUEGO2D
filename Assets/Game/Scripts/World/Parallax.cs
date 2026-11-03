using UnityEngine;

namespace Rescate
{
    /// <summary>Desplaza una capa de fondo a distinta velocidad que la cámara (profundidad).</summary>
    public class Parallax : MonoBehaviour
    {
        public float factor = 0.35f;
        public bool followY;

        Transform cam;
        Vector3 startPos;
        float startCamX, startCamY;

        void Start()
        {
            cam = Camera.main != null ? Camera.main.transform : null;
            startPos = transform.position;
            if (cam != null)
            {
                startCamX = cam.position.x;
                startCamY = cam.position.y;
            }
        }

        void LateUpdate()
        {
            if (cam == null) return;
            float x = startPos.x + (cam.position.x - startCamX) * factor;
            float y = followY ? startPos.y + (cam.position.y - startCamY) * factor : startPos.y;
            transform.position = new Vector3(x, y, startPos.z);
        }
    }
}
