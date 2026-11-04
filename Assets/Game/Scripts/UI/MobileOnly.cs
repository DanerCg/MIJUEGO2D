using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Activa el objeto únicamente en plataformas táctiles (Android / iOS).
    /// Se usa para no mostrar los controles en pantalla en Windows.
    /// </summary>
    public class MobileOnly : MonoBehaviour
    {
        void Awake()
        {
            if (!Application.isMobilePlatform) gameObject.SetActive(false);
        }
    }
}
