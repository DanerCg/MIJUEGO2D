using UnityEngine;

namespace Rescate
{
    /// <summary>Punto de control: guarda la posición de reaparición y cura al jugador.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public SpriteRenderer flagRenderer;
        public Color inactiveColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        public Color activeColor = new Color(0.35f, 1f, 0.65f, 1f);

        bool activated;

        void Awake()
        {
            if (flagRenderer != null) flagRenderer.color = inactiveColor;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (activated) return;
            if (other.GetComponentInParent<PlayerController2D>() == null) return;

            activated = true;
            GameManager.CheckpointPosition = transform.position;
            GameManager.HasCheckpoint = true;

            if (flagRenderer != null) flagRenderer.color = activeColor;
            if (PlayerStats.Instance != null) PlayerStats.Instance.FullHeal();

            AudioManager.PlaySfx("checkpoint");
            Fx.Burst(transform.position + Vector3.up * 2.2f, activeColor, 16, 4f, 0.17f, 0.6f);
            Fx.Dust(transform.position + Vector3.up * 0.1f, 6, 0.9f);
            GameEvents.RaiseMessage("Punto de control activado");
            GameEvents.RaiseCheckpointReached();
        }
    }
}
