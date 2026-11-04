using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rescate
{
    /// <summary>
    /// Botón virtual multitáctil: mantiene pulsada una acción de InputHub mientras
    /// se presiona y se puede combinar con otros botones (multitouch real).
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum TouchAction { Left, Right, Jump, Attack, Run }

        public TouchAction action = TouchAction.Jump;
        public Image background;
        public Color normalColor = new Color(1f, 1f, 1f, 0.28f);
        public Color pressedColor = new Color(1f, 1f, 1f, 0.55f);

        bool held;

        void Start()
        {
            if (background == null) background = GetComponent<Image>();
            SetVisual(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            held = true;
            Apply(true);
            SetVisual(true);

            // Evita que el botón táctil quede seleccionado y lo active también
            // el botón Cross del mando (Submit del EventSystem).
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            held = false;
            Apply(false);
            SetVisual(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!held) return;
            held = false;
            Apply(false);
            SetVisual(false);
        }

        void Apply(bool value)
        {
            switch (action)
            {
                case TouchAction.Left: InputHub.TouchLeft = value; break;
                case TouchAction.Right: InputHub.TouchRight = value; break;
                case TouchAction.Jump: InputHub.TouchJump = value; break;
                case TouchAction.Attack: InputHub.TouchAttack = value; break;
                case TouchAction.Run: InputHub.TouchRun = value; break;
            }
        }

        void SetVisual(bool pressed)
        {
            if (background != null) background.color = pressed ? pressedColor : normalColor;
        }

        void OnDisable()
        {
            if (!held) return;
            held = false;
            Apply(false);
            SetVisual(false);
        }
    }
}
