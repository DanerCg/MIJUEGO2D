using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rescate.EditorTools
{
    /// <summary>
    /// Recorta los props generados a su caja alfa real y los reparte por la escena
    /// abierta para llenar el escenario (vegetación, rocas, flores, hongos) y dar
    /// profundidad con árboles de fondo. Todo es decorativo: sin colliders.
    /// Es idempotente: borra el grupo "Decor" antes de volver a crearlo.
    /// </summary>
    public static class DecorationBuilder
    {
        const string Art = "Assets/Game/Sprites";
        const string SourceArt = "Assets/Generated_Images";
        const byte AlphaCutoff = 40;
        // El render de la roca trae un halo gris suave; se recorta más agresivo.
        const byte RockCutoff = 110;

        static readonly string[] Props =
        {
            "prop_tree", "prop_bush", "prop_rock",
            "prop_flower", "prop_mushroom", "prop_grass"
        };

        [MenuItem("Rescate/Arte/Importar props recortados", priority = 110)]
        public static void ImportAll()
        {
            foreach (string p in Props)
            {
                Sprite s = ImportProp(p);
                if (s == null) Debug.LogWarning("[Props] No se pudo importar " + p);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Props] Props recortados importados.");
        }

        [MenuItem("Rescate/Niveles/Decorar escena actual", priority = 310)]
        public static void DecorateCurrent()
        {
            Scene scene = SceneManager.GetActiveScene();

            // props
            System.Collections.Generic.Dictionary<string, Sprite> sprites =
                new System.Collections.Generic.Dictionary<string, Sprite>();
            foreach (string p in Props)
            {
                Sprite s = ImportProp(p);
                if (s != null) sprites[p] = s;
            }
            if (sprites.Count == 0)
            {
                Debug.LogError("[Props] No hay props importados; no se decora.");
                return;
            }

            // limpia la decoración anterior
            GameObject previous = GameObject.Find("Decor");
            if (previous != null) Object.DestroyImmediate(previous);

            GameObject decorRoot = new GameObject("Decor");
            GameObject level = GameObject.Find("Level");
            if (level != null) decorRoot.transform.SetParent(level.transform, false);

            int placed = 0;
            int trees = 0;

            SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (SpriteRenderer ground in renderers)
            {
                if (ground == null || ground.gameObject.name != "Ground") continue;
                if (ground.transform.parent != null && ground.transform.parent.name == "Decor") continue;

                float halfW = ground.size.x * 0.5f;
                float cx = ground.transform.position.x;
                float topY = ground.transform.position.y + ground.size.y * 0.5f;
                float x0 = cx - halfW + 1.0f;
                float x1 = cx + halfW - 1.0f;

                // --- línea de árboles lejana: baja, hundida tras el suelo y
                //     desaturada, para que lea como profundidad y deje ver el fondo.
                for (float x = x0 + 1f; x < x1 - 1f; x += 9f)
                {
                    Sprite tree = Get(sprites, "prop_tree");
                    if (tree == null) break;
                    float h = 4.2f + Deterministic(x, 1) * 1.5f;
                    AddVisual(decorRoot.transform, "BgTree", tree,
                        new Vector3(x, topY - 1.9f, 0f), h, -50, 0.62f,
                        new Color(0.86f, 0.93f, 1f, 0.62f));
                    trees++;
                }

                // --- vegetación sobre el suelo
                float step = 3.4f;
                float cursor = x0 + Deterministic(x0, 7) * step;
                while (cursor < x1)
                {
                    float roll = Deterministic(cursor, 13);
                    string pick;
                    float h;
                    if (roll < 0.26f)      { pick = "prop_bush"; h = 1.35f + Deterministic(cursor, 21) * 0.55f; }
                    else if (roll < 0.42f) { pick = "prop_grass"; h = 0.55f + Deterministic(cursor, 23) * 0.30f; }
                    else if (roll < 0.56f) { pick = "prop_flower"; h = 0.55f + Deterministic(cursor, 25) * 0.25f; }
                    else if (roll < 0.70f) { pick = "prop_rock"; h = 0.60f + Deterministic(cursor, 27) * 0.45f; }
                    else if (roll < 0.82f) { pick = "prop_mushroom"; h = 0.55f + Deterministic(cursor, 29) * 0.30f; }
                    else                   { pick = "prop_grass"; h = 0.45f + Deterministic(cursor, 31) * 0.25f; }

                    Sprite s = Get(sprites, pick);
                    if (s != null)
                    {
                        int order = 3 + Mathf.RoundToInt(Deterministic(cursor, 37) * 5f);
                        float y = topY - 0.02f;
                        AddVisual(decorRoot.transform, pick, s, new Vector3(cursor, y, 0f), h, order, 1f);
                        placed++;
                    }

                    cursor += step * (0.75f + Deterministic(cursor, 41) * 0.7f);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Props] " + scene.name + " decorado: props=" + placed + " árboles de fondo=" + trees);
        }

        static Sprite Get(System.Collections.Generic.Dictionary<string, Sprite> map, string key)
        {
            Sprite s;
            return map.TryGetValue(key, out s) ? s : null;
        }

        static float Deterministic(float x, int salt)
        {
            float v = Mathf.Sin(x * 12.9898f + salt * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        static void AddVisual(Transform parent, string name, Sprite sprite, Vector3 position,
            float desiredHeight, int sortingOrder, float alpha, Color? tint = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            if (tint.HasValue) sr.color = tint.Value;
            else if (alpha < 1f) sr.color = new Color(1f, 1f, 1f, alpha);

            float natural = sprite.rect.height / sprite.pixelsPerUnit;
            if (natural > 0.001f)
            {
                float k = desiredHeight / natural;
                go.transform.localScale = new Vector3(k, k, 1f);
            }
        }

        // ------------------------------------------------------------ importación

        /// <summary>
        /// Recorta el PNG a la caja de alfa (ignorando halos tenues) y lo importa como
        /// sprite con pivote inferior-centro, para poder apoyarlo en el suelo.
        /// </summary>
        static Sprite ImportProp(string name)
        {
            string src = SourceArt + "/" + name + ".png";
            string dst = Art + "/" + name + ".png";

            if (File.Exists(src))
            {
                byte[] bytes = File.ReadAllBytes(src);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);

                int w = tex.width, h = tex.height;
                Color32[] px = tex.GetPixels32();

                int minX = w, minY = h, maxX = -1, maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int cutoff = name == "prop_rock" ? RockCutoff : AlphaCutoff;
                        if (px[y * w + x].a < cutoff) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < 0 || maxY < 0)
                {
                    Debug.LogWarning("[Props] " + name + " está vacío tras el recorte.");
                    Object.DestroyImmediate(tex);
                    return Reimport(dst);
                }

                int cw = maxX - minX + 1;
                int ch = maxY - minY + 1;

                Texture2D cropped = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
                Color32[] outPx = new Color32[cw * ch];
                for (int y = 0; y < ch; y++)
                    for (int x = 0; x < cw; x++)
                        outPx[y * cw + x] = px[(y + minY) * w + (x + minX)];

                cropped.SetPixels32(outPx);
                cropped.Apply();

                File.WriteAllBytes(dst, cropped.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(cropped);

                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }

            return Reimport(dst);
        }

        static Sprite Reimport(string path)
        {
            if (!File.Exists(path)) return null;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return AssetDatabase.LoadAssetAtPath<Sprite>(path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
