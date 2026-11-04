using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rescate
{
    /// <summary>
    /// Utilidad de foco para UGUI: al abrir un panel se selecciona su primer botón,
    /// de forma que el D-Pad y el stick izquierdo del mando puedan recorrer el menú
    /// y Cross/Enter activen la opción resaltada.
    /// </summary>
    public static class UIFocus
    {
        /// <summary>Primer botón visible e interactable dentro del panel.</summary>
        public static GameObject FirstButton(GameObject panelRoot)
        {
            if (panelRoot == null || !panelRoot.activeInHierarchy) return null;

            Button[] buttons = panelRoot.GetComponentsInChildren<Button>(false);
            foreach (Button b in buttons)
                if (b != null && b.IsInteractable() && b.gameObject.activeInHierarchy) return b.gameObject;

            foreach (Button b in buttons)
                if (b != null && b.gameObject.activeInHierarchy) return b.gameObject;

            return null;
        }

        public static void SelectFirst(GameObject panelRoot)
        {
            GameObject go = FirstButton(panelRoot);
            if (go != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
        }

        public static void Select(GameObject target)
        {
            if (target != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
        }

        public static void Clear()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
