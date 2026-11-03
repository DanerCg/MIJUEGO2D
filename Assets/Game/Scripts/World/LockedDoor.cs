using UnityEngine;

namespace Rescate
{
    /// <summary>Puerta cerrada del castillo: se abre al reunir las llaves mágicas.</summary>
    public class LockedDoor : MonoBehaviour
    {
        public int keysRequired = 3;
        public GameObject barrier;
        public SpriteRenderer glow;

        bool opened;
        float hintCooldown;

        void Update()
        {
            if (hintCooldown > 0f) hintCooldown -= Time.deltaTime;
        }

        void OnTriggerEnter2D(Collider2D other) { TryOpen(other); }
        void OnTriggerStay2D(Collider2D other) { TryOpen(other); }

        void TryOpen(Collider2D other)
        {
            if (opened) return;
            if (other.GetComponentInParent<PlayerController2D>() == null) return;

            PlayerStats stats = PlayerStats.Instance;
            int keys = stats != null ? stats.keys : 0;

            if (keys >= keysRequired)
            {
                opened = true;
                if (barrier != null) barrier.SetActive(false);
                if (glow != null) glow.color = new Color(0.4f, 1f, 0.6f, 1f);
                AudioManager.PlaySfx("door");
                GameEvents.RaiseMessage("¡Las tres llaves abren la puerta del trono!");
            }
            else if (hintCooldown <= 0f)
            {
                hintCooldown = 1.6f;
                AudioManager.PlaySfx("lockedDoor", 0.7f);
                GameEvents.RaiseMessage("Sellada: necesitas " + keysRequired + " llaves mágicas. Tienes " + keys);
            }
        }
    }
}
