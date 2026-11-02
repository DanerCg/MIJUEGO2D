using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rescate.EditorTools
{
    /// <summary>
    /// Pule la escena de nivel ABIERTA sin tocar su lógica ni sus colisiones:
    ///  - el suelo se alarga hacia abajo con una capa de relleno sin hierba, para que
    ///    no se vea el fondo por debajo ni se repita la franja de césped,
    ///  - las plataformas usan su propia loseta (antes reutilizaban la del suelo, que
    ///    al ser más alta se aplastaba y perdía la hierba),
    ///  - se corrige la posición visual de las puertas de salida, cuyos hijos quedaron
    ///    desplazados al crearse con posición de mundo y padre ya desplazado,
    ///  - los controles táctiles solo aparecen en móvil.
    /// Es idempotente: se puede ejecutar varias veces.
    /// </summary>
    public static class LevelPolishBuilder
    {
        const string Art = "Assets/Game/Sprites";
        const float GroundTileHeight = 2f;   // altura natural de la loseta de suelo
        const float FillDepth = 12f;         // altura de la capa de relleno

        [MenuItem("Rescate/Niveles/Pulir escena actual", priority = 300)]
        public static void PolishCurrent()
        {
            Scene scene = SceneManager.GetActiveScene();
            string theme = ThemeOf(scene.name);

            Sprite ground = Load("tile_" + theme);
            Sprite plat = Load("plat_" + theme);
            Sprite fill = Load("fill_" + theme);

            if (ground == null || plat == null || fill == null)
            {
                Debug.LogError("[Niveles] Faltan losetas del tema '" + theme +
                               "' (tile/plat/fill). Ejecuta antes 'Regenerar losetas de terreno'.");
                return;
            }

            int grounds = 0, platforms = 0, doors = 0, mobile = 0;

            SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null) continue;
                string n = sr.gameObject.name;

                if (n == "Ground")
                {
                    RestoreGround(sr, ground, fill);
                    grounds++;
                }
                else if (n == "Platform" || n == "MovingPlatform")
                {
                    sr.sprite = plat;
                    platforms++;
                }
            }

            // puertas: los hijos se crearon con posición de mundo y quedaron desplazados
            foreach (LevelExit exit in Object.FindObjectsByType<LevelExit>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                RepairDoorVisual(exit);
                doors++;
            }

            Transform touch = FindByName("TouchControls");
            if (touch != null && touch.GetComponent<MobileOnly>() == null)
            {
                touch.gameObject.AddComponent<MobileOnly>();
                mobile++;
            }

            bool camera = RepairCameraBounds();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Niveles] " + scene.name + " pulido: suelo=" + grounds +
                      " plataformas=" + platforms + " puertas=" + doors + " táctil=" + mobile +
                      " cámara=" + (camera ? "ajustada" : "ok"));
        }

        /// <summary>
        /// Ajusta los límites horizontales de la cámara a la extensión real del suelo.
        /// Si son más anchos que el nivel, la cámara asoma en los extremos y deja ver
        /// el color de fondo (el 'vacío') a los lados del terreno.
        /// </summary>
        static bool RepairCameraBounds()
        {
            CameraFollow2D follow = Object.FindFirstObjectByType<CameraFollow2D>(
                FindObjectsInactive.Include);
            if (follow == null) return false;

            float minX = float.MaxValue, maxX = float.MinValue;
            bool any = false;

            foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr == null || sr.gameObject.name != "Ground") continue;
                Bounds b = sr.bounds;
                if (b.min.x < minX) minX = b.min.x;
                if (b.max.x > maxX) maxX = b.max.x;
                any = true;
            }

            if (!any) return false;

            Vector2 boundsMin = follow.boundsMin;
            Vector2 boundsMax = follow.boundsMax;
            boundsMin.x = minX;
            boundsMax.x = maxX;

            if (boundsMin == follow.boundsMin && boundsMax == follow.boundsMax) return false;

            follow.boundsMin = boundsMin;
            follow.boundsMax = boundsMax;
            return true;
        }

        static string ThemeOf(string sceneName)
        {
            if (sceneName.Contains("Monta")) return "mountain";
            if (sceneName.Contains("Castillo")) return "castle";
            return "forest";
        }

        static Sprite Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/" + name + ".png");
        }

        /// <summary>
        /// Deja el bloque de suelo en su altura original (una sola loseta, para no
        /// repetir la hierba) y añade debajo una capa de relleno sin césped.
        /// </summary>
        static void RestoreGround(SpriteRenderer sr, Sprite ground, Sprite fill)
        {
            sr.sprite = ground;

            float width = Mathf.Max(0.5f, sr.size.x);
            float top = sr.transform.position.y + sr.size.y * 0.5f;

            sr.size = new Vector2(width, GroundTileHeight);
            sr.transform.position = new Vector3(
                sr.transform.position.x,
                top - GroundTileHeight * 0.5f,
                sr.transform.position.z);

            BoxCollider2D box = sr.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(width, GroundTileHeight);
                box.offset = Vector2.zero;
            }

            // capa de relleno (una sola vez)
            Transform existing = sr.transform.Find("UndergroundFill");
            GameObject fillGo;
            if (existing != null)
            {
                fillGo = existing.gameObject;
            }
            else
            {
                fillGo = new GameObject("UndergroundFill");
                fillGo.transform.SetParent(sr.transform, false);
            }

            fillGo.transform.localPosition = new Vector3(0f, -GroundTileHeight * 0.5f - FillDepth * 0.5f, 0f);
            fillGo.transform.localRotation = Quaternion.identity;
            fillGo.transform.localScale = Vector3.one;

            SpriteRenderer fsr = fillGo.GetComponent<SpriteRenderer>();
            if (fsr == null) fsr = fillGo.AddComponent<SpriteRenderer>();
            fsr.sprite = fill;
            fsr.color = Color.white;
            fsr.sortingOrder = sr.sortingOrder - 1;
            fsr.drawMode = SpriteDrawMode.Tiled;
            fsr.tileMode = SpriteTileMode.Continuous;
            fsr.size = new Vector2(width, FillDepth);

            // sin collider: solo cubre visualmente por debajo del suelo
            Collider2D stray = fillGo.GetComponent<Collider2D>();
            if (stray != null) Object.DestroyImmediate(stray);
        }

        /// <summary>Recoloca los hijos visuales de la puerta en el centro del objeto.</summary>
        static void RepairDoorVisual(LevelExit exit)
        {
            Transform t = exit.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                Vector3 lp = child.localPosition;
                if (Mathf.Abs(lp.x) > 0.01f)
                    child.localPosition = new Vector3(0f, lp.y, lp.z);
            }
        }

        static Transform FindByName(string name)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == name) return t;
            }
            return null;
        }
    }
}
