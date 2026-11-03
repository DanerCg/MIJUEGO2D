using UnityEngine;

namespace Rescate
{
    /// <summary>Puerta de salida del nivel: exige un número de llaves mágicas.</summary>
    public class LevelExit : MonoBehaviour
    {
        public int keysRequired = 1;
        public SpriteRenderer glow;

        bool transitioning;
        float hintCooldown;

        void Update()
        {
            if (hintCooldown > 0f) hintCooldown -= Time.deltaTime;
        }

        void OnTriggerEnter2D(Collider2D other) { TryExit(other); }
        void OnTriggerStay2D(Collider2D other) { TryExit(other); }

        void TryExit(Collider2D other)
        {
            if (transitioning) return;
            if (other.GetComponentInParent<PlayerController2D>() == null) return;

            PlayerStats stats = PlayerStats.Instance;
            int keys = stats != null ? stats.keys : 0;

            if (keys >= keysRequired)
            {
                transitioning = true;
                if (glow != null) glow.color = Color.white;
                AudioManager.PlaySfx("door");
                GameEvents.RaiseMessage("¡Puerta abierta!");
                if (GameManager.Instance != null) GameManager.Instance.LoadNextLevel();
            }
            else if (hintCooldown <= 0f)
            {
                hintCooldown = 1.5f;
                AudioManager.PlaySfx("lockedDoor", 0.7f);
                GameEvents.RaiseMessage("Necesitas " + keysRequired + " llave(s) mágica(s). Tienes " + keys);
            }
        }
    }
}
