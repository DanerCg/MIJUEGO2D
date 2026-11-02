using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rescate.EditorTools
{
    /// <summary>
    /// Parallax multicapa:
    ///  - capa lejana: el fondo pintado actual (bg_*) convertido en loseta horizontal sin
    ///    costura (original + espejo), 2048x1024;
    ///  - capa media: banda generada (mid_*) recortada a su alfa, con un faldón opaco
    ///    inferior para que en los fosos no se vea su borde, también en espejo.
    /// ApplyToCurrent sustituye el Parallax (factor 1) del Backdrop por ParallaxLayer y crea
    /// la capa media. Idempotente.
    /// </summary>
    public static class ParallaxBuilder
    {
        const string Art = "Assets/Game/Sprites";
        const string SourceArt = "Assets/Generated_Images";
        const float PPU = 100f;

        public const float FarScroll = 0.15f;
        public const float MidScroll = 0.35f;

        static readonly string[] Themes = { "forest", "mountain", "castle" };

        [MenuItem("Rescate/Arte/Generar capas de parallax", priority = 120)]
        public static void BuildTextures()
        {
            foreach (string t in Themes)
            {
                BuildFar(t);
                BuildMid(t);
            }
            AssetDatabase.Refresh();
            Debug.Log("[Parallax] Capas generadas.");
        }

        // --------------------------------------------------------------- texturas

        // franja horizontal útil del fondo pintado (se descartan los árboles/acantilados
        // que lo enmarcan, que al repetirse en espejo se verían duplicados)
        static Vector2 FarCrop(string theme)
        {
            if (theme == "forest") return new Vector2(0.24f, 0.80f);
            if (theme == "mountain") return new Vector2(0.20f, 0.80f);
            return new Vector2(0f, 1f);
        }

        static void BuildFar(string theme)
        {
            string src = Art + "/bg_" + theme + ".png";
            if (!File.Exists(src)) { Debug.LogWarning("[Parallax] Falta " + src); return; }

            Texture2D tex = Load(src);
            Vector2 crop = FarCrop(theme);
            Color32[] half = Resample(tex, 1024, 1024, 0, tex.height, crop.x, crop.y);
            Object.DestroyImmediate(tex);

            Write("para_far_" + theme, Mirror(half, 1024, 1024), 2048, 1024, new Vector2(0.5f, 0.5f));
        }

        static void BuildMid(string theme)
        {
            string src = SourceArt + "/mid_" + theme + ".png";
            if (!File.Exists(src)) { Debug.LogWarning("[Parallax] Falta " + src); return; }

            Texture2D tex = Load(src);
            int w = tex.width, h = tex.height;
            Color32[] px = tex.GetPixels32();

            // filas con contenido (alfa > 24)
            int minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x += 2)
                {
                    if (px[y * w + x].a > 24)
                    {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        break;
                    }
                }
            }
            if (maxY < 0) { Debug.LogWarning("[Parallax] mid_" + theme + " vacío"); Object.DestroyImmediate(tex); return; }

            int contentH = maxY - minY + 1;
            // escala uniforme (conserva la proporción): ancho total ≤ 2048, alto total ≤ 1024
            float sc = Mathf.Min(1024f / w, 758f / contentH);
            int halfW = Mathf.RoundToInt(w * sc);
            int outH = Mathf.RoundToInt(contentH * sc);
            int skirt = Mathf.RoundToInt(outH * 0.35f);
            int totalH = outH + skirt;

            Color32[] body = Resample(tex, halfW, outH, minY, contentH);
            Object.DestroyImmediate(tex);

            // la fila inferior se hace opaca y se prolonga hacia abajo oscureciéndose
            Color32[] half = new Color32[halfW * totalH];
            for (int y = 0; y < outH; y++)
                for (int x = 0; x < halfW; x++)
                    half[(y + skirt) * halfW + x] = body[y * halfW + x];

            for (int x = 0; x < halfW; x++)
            {
                // color medio de las 6 filas inferiores opacas de esa columna
                Color acc = Color.black; int n = 0;
                for (int y = 0; y < Mathf.Min(24, outH); y++)
                {
                    Color32 c = body[y * halfW + x];
                    if (c.a < 200) continue;
                    acc += (Color)c; n++;
                    if (n >= 6) break;
                }
                Color baseC = n > 0 ? acc / n : new Color(0.08f, 0.1f, 0.12f, 1f);
                baseC.a = 1f;
                for (int y = 0; y < skirt; y++)
                {
                    float k = (float)y / Mathf.Max(1, skirt - 1); // 0 abajo, 1 arriba
                    Color c = Color.Lerp(baseC * 0.55f, baseC, Mathf.SmoothStep(0f, 1f, k));
                    c.a = 1f;
                    half[y * halfW + x] = c;
                }
                // asegura que la base del dibujo no deje transparencias sobre el faldón
                for (int y = 0; y < 10 && y < outH; y++)
                {
                    int i = (y + skirt) * halfW + x;
                    Color c = half[i];
                    Color m = Color.Lerp(baseC, c, c.a);
                    m.a = 1f;
                    half[i] = m;
                }
            }

            Write("para_mid_" + theme, Mirror(half, halfW, totalH), halfW * 2, totalH, new Vector2(0.5f, 0f));
        }

        static Texture2D Load(string path)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(path));
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        /// <summary>Remuestrea la franja [y0, y0+srcH) de la textura a outW x outH.</summary>
        static Color32[] Resample(Texture2D t, int outW, int outH, int y0, int srcH, float u0 = 0f, float u1 = 1f)
        {
            Color32[] o = new Color32[outW * outH];
            for (int y = 0; y < outH; y++)
            {
                float v = (y0 + (y + 0.5f) * srcH / outH) / t.height;
                for (int x = 0; x < outW; x++)
                {
                    float u = Mathf.Lerp(u0, u1, (x + 0.5f) / outW);
                    o[y * outW + x] = t.GetPixelBilinear(u, v);
                }
            }
            return o;
        }

        static Color32[] Mirror(Color32[] half, int w, int h)
        {
            Color32[] o = new Color32[w * 2 * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 c = half[y * w + x];
                    o[y * w * 2 + x] = c;
                    o[y * w * 2 + (w * 2 - 1 - x)] = c;
                }
            }
            return o;
        }

        static void Write(string name, Color32[] px, int w, int h, Vector2 pivot)
        {
            string path = Art + "/" + name + ".png";
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapModeU = TextureWrapMode.Repeat;
            ti.wrapModeV = TextureWrapMode.Clamp;
            ti.maxTextureSize = 2048;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;

            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect; // obligatorio para Tiled
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }

        // --------------------------------------------------------------- escena

        [MenuItem("Rescate/Niveles/Aplicar parallax a escena actual", priority = 320)]
        public static void ApplyToCurrent()
        {
            Scene scene = SceneManager.GetActiveScene();
            string theme = ThemeOf(scene.name);

            Sprite far = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/para_far_" + theme + ".png");
            Sprite mid = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/para_mid_" + theme + ".png");
            if (far == null || mid == null)
            {
                Debug.LogError("[Parallax] Faltan para_far/para_mid de '" + theme + "'. Ejecuta 'Generar capas de parallax'.");
                return;
            }

            Camera cam = Object.FindFirstObjectByType<Camera>();
            float ortho = cam != null ? cam.orthographicSize : 6f;

            // ---------------- capa lejana (Backdrop)
            GameObject backdrop = GameObject.Find("Backdrop");
            if (backdrop == null)
            {
                backdrop = new GameObject("Backdrop");
                backdrop.AddComponent<SpriteRenderer>();
            }
            Parallax old = backdrop.GetComponent<Parallax>();
            if (old != null) Object.DestroyImmediate(old);

            SpriteRenderer fsr = backdrop.GetComponent<SpriteRenderer>();
            fsr.sprite = far;
            fsr.color = Color.white;
            fsr.sortingOrder = -100;
            fsr.drawMode = SpriteDrawMode.Tiled;
            fsr.tileMode = SpriteTileMode.Continuous;

            float farH = ortho * 2f + 3.5f;                  // cubre la vista con margen
            float kFar = farH / (far.rect.height / PPU);
            // cada mitad se remuestreó a 1024 px: se restaura la proporción original en X
            Vector2 fc = FarCrop(theme);
            float aspectFix = (fc.y - fc.x) * 1536f / 1024f;
            backdrop.transform.localScale = new Vector3(kFar * aspectFix, kFar, 1f);
            fsr.size = new Vector2(far.rect.width / PPU, far.rect.height / PPU);

            ParallaxLayer fl = backdrop.GetComponent<ParallaxLayer>();
            if (fl == null) fl = backdrop.AddComponent<ParallaxLayer>();
            fl.scroll = FarScroll;
            fl.scrollY = 0.03f;
            fl.lockToCameraY = true;
            fl.offsetY = 0.9f;
            fl.maxYShift = 0.35f;
            fl.phase = 3f;

            // ---------------- capa media
            float groundTop = LowestGroundTop();
            GameObject midGo = GameObject.Find("ParallaxMid");
            if (midGo == null) midGo = new GameObject("ParallaxMid");
            midGo.transform.SetParent(null, false);

            SpriteRenderer msr = midGo.GetComponent<SpriteRenderer>();
            if (msr == null) msr = midGo.AddComponent<SpriteRenderer>();
            msr.sprite = mid;
            msr.sortingOrder = -70;
            msr.drawMode = SpriteDrawMode.Tiled;
            msr.tileMode = SpriteTileMode.Continuous;
            msr.color = MidTint(theme);

            float visibleH = MidHeight(theme);              // altura del dibujo sobre el suelo
            float texH = mid.rect.height / PPU;
            float contentFrac = 1f / 1.35f;                  // el faldón es 35 % del dibujo
            float kMid = visibleH / (texH * contentFrac);
            midGo.transform.localScale = new Vector3(kMid, kMid, 1f);
            msr.size = new Vector2(mid.rect.width / PPU, texH);

            float skirtWorld = texH * kMid * (1f - contentFrac);
            midGo.transform.position = new Vector3(0f, groundTop - 0.9f - skirtWorld, 0f);

            ParallaxLayer ml = midGo.GetComponent<ParallaxLayer>();
            if (ml == null) ml = midGo.AddComponent<ParallaxLayer>();
            ml.scroll = MidScroll;
            ml.scrollY = 0f;
            ml.lockToCameraY = false;
            ml.phase = 11f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Parallax] " + scene.name + ": lejana scroll=" + FarScroll + " media scroll=" + MidScroll +
                      " sueloMin=" + groundTop.ToString("F2"));
        }

        static float MidHeight(string theme)
        {
            if (theme == "mountain") return 8.5f;
            if (theme == "castle") return 9f;
            return 6.2f;
        }

        static Color MidTint(string theme)
        {
            if (theme == "mountain") return new Color(0.9f, 0.93f, 1f, 1f);
            if (theme == "castle") return new Color(0.88f, 0.85f, 0.95f, 1f);
            return new Color(0.92f, 0.98f, 1f, 1f);
        }

        static float LowestGroundTop()
        {
            float best = float.MaxValue;
            foreach (SpriteRenderer sr in Object.FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr == null || sr.gameObject.name != "Ground") continue;
                float top = sr.bounds.max.y;
                if (top < best) best = top;
            }
            return best == float.MaxValue ? 0f : best;
        }

        static string ThemeOf(string sceneName)
        {
            if (sceneName.Contains("Monta")) return "mountain";
            if (sceneName.Contains("Castillo")) return "castle";
            return "forest";
        }
    }
}
