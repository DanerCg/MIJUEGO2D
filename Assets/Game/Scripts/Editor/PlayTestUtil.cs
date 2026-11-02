using UnityEngine;

namespace Rescate.EditorTools
{
    /// <summary>Utilidades de verificación en Play Mode (solo editor).</summary>
    public static class PlayTestUtil
    {
        /// <summary>Coloca al jugador sobre el suelo en X, congela el tiempo y encuadra la cámara.</summary>
        public static string Frame(float x, bool freeze)
        {
            PlayerController2D p = Object.FindFirstObjectByType<PlayerController2D>();
            if (p == null) return "sin jugador";
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, 30f), Vector2.down, 80f, 1 << 8);
            float y = hit ? hit.point.y + 0.02f : p.transform.position.y;
            p.transform.position = new Vector3(x, y, 0f);
            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.position = p.transform.position; rb.linearVelocity = Vector2.zero; }
            CameraFollow2D cf = Object.FindFirstObjectByType<CameraFollow2D>();
            if (cf != null) cf.SnapToTarget();
            Time.timeScale = freeze ? 0f : 1f;
            Camera c = Camera.main;
            return "player=" + p.transform.position.ToString("F2") + " cam=" + (c != null ? c.transform.position.ToString("F2") : "-");
        }

        public static void Unfreeze() { Time.timeScale = 1f; }
    }
}
