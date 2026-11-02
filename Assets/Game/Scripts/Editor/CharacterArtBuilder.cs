using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rescate.EditorTools
{
    /// <summary>
    /// Importa las poses dibujadas del protagonista y las asigna al prefab Player:
    ///  - limpia el alfa residual (halo) de las imágenes generadas,
    ///  - coloca el pivote en los pies (fila opaca más baja) y centrado en el torso
    ///    (centroide de la mitad superior), para que al cambiar de pose el personaje
    ///    no "salte" ni se desplace y el flipX gire sobre el cuerpo;
    ///  - crea el material de partículas compartido (Resources/FX/FxParticle.mat).
    /// </summary>
    public static class CharacterArtBuilder
    {
        const string Art = "Assets/Game/Sprites";
        const string SourceArt = "Assets/Generated_Images";
        const string PlayerPrefab = "Assets/Game/Prefabs/Player.prefab";
        const string FxFolder = "Assets/Game/Resources/FX";

        // destino -> candidatos de origen (el primero que exista)
        static readonly Dictionary<string, string[]> Poses = new Dictionary<string, string[]>
        {
            { "nico_run_a",  new[] { "edited_nico_run2_1" } },
            { "nico_run_b",  new[] { "edited_nico_run1_1" } },
            { "nico_jump",   new[] { "edited_nico_jump_1" } },
            { "nico_fall",   new[] { "edited_nico_fall_1" } },
            { "nico_attack", new[] { "edited_nico_attack_1" } },
            { "nico_hurt",   new[] { "edited_nico_hurt_1" } },
            { "nico_victory",new[] { "edited_nico_victory_1" } },
        };

        [MenuItem("Rescate/Arte/Importar poses del protagonista", priority = 130)]
        public static void BuildAll()
        {
            BuildFxMaterial();

            Dictionary<string, Sprite> s = new Dictionary<string, Sprite>();
            foreach (KeyValuePair<string, string[]> kv in Poses)
            {
                foreach (string src in kv.Value)
                {
                    string path = SourceArt + "/" + src + ".png";
                    if (!File.Exists(path)) continue;
                    Sprite sp = ImportPose(path, Art + "/" + kv.Key + ".png");
                    if (sp != null) { s[kv.Key] = sp; break; }
                }
            }

            // la pose idle es el sprite original: solo se recalcula su pivote
            Sprite idle = SetPivotInPlace(Art + "/nico.png");

            AssignToPrefab(idle, s);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] Poses importadas: " + string.Join(", ", s.Keys));
        }

        // ------------------------------------------------------------ poses

        static Sprite ImportPose(string src, string dst)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(src));
            Color32[] px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                // halo casi invisible -> transparente; evita bordes grises al escalar
                if (px[i].a < 16) px[i] = new Color32(0, 0, 0, 0);
            }
            t.SetPixels32(px);
            t.Apply();
            File.WriteAllBytes(dst, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return SetPivotInPlace(dst);
        }

        static Sprite SetPivotInPlace(string path)
        {
            if (!File.Exists(path)) return null;
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(path));
            int w = t.width, h = t.height;
            Color32[] px = t.GetPixels32();
            Object.DestroyImmediate(t);

            int minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x += 2)
                    if (px[y * w + x].a > 128) { if (y < minY) minY = y; if (y > maxY) maxY = y; break; }
            if (maxY < 0) return AssetDatabase.LoadAssetAtPath<Sprite>(path);

            // centroide horizontal de cabeza+torso (mitad superior del personaje)
            int split = minY + (maxY - minY) / 2;
            double sum = 0; long n = 0;
            for (int y = split; y <= maxY; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 128) { sum += x; n++; }
            float cx = n > 0 ? (float)(sum / n) : w * 0.5f;

            Vector2 pivot = new Vector2(cx / w, (minY + 1f) / h);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return null;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 512;   // se muestra a ~2 u (≈190 px en 1080p): 512 sobra
            ti.textureCompression = TextureImporterCompression.CompressedHQ;

            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = pivot;
            st.spriteMeshType = SpriteMeshType.Tight;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void AssignToPrefab(Sprite idle, Dictionary<string, Sprite> s)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                PlayerVisuals v = root.GetComponent<PlayerVisuals>();
                if (v == null) { Debug.LogError("[Personaje] El prefab no tiene PlayerVisuals"); return; }

                Sprite Get(string k) { Sprite r; return s.TryGetValue(k, out r) ? r : null; }

                v.idleSprite = idle;
                List<Sprite> run = new List<Sprite>();
                // ciclo: zancada A -> paso (idle) -> zancada B (o A) -> paso
                Sprite a = Get("nico_run_a");
                Sprite b = Get("nico_run_b");
                if (a != null)
                {
                    run.Add(a);
                    run.Add(idle);
                    run.Add(b != null ? b : a);
                    run.Add(idle);
                }
                v.runSprites = run.ToArray();
                v.jumpSprite = Get("nico_jump") ?? a;
                v.fallSprite = Get("nico_fall") ?? Get("nico_jump") ?? a;
                v.attackSprite = Get("nico_attack") ?? a;
                v.hurtSprite = Get("nico_hurt");
                v.victorySprite = Get("nico_victory");
                v.groundLayers = 1 << 8;

                if (v.spriteRenderer != null) v.spriteRenderer.sprite = idle;
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------ partículas

        static void BuildFxMaterial()
        {
            Directory.CreateDirectory(FxFolder);
            string texPath = FxFolder + "/fx_soft.png";
            const int S = 64;
            Texture2D t = new Texture2D(S, S, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply();
            File.WriteAllBytes(texPath, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            TextureImporter ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            string matPath = FxFolder + "/FxParticle.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, matPath);
            }
            m.shader = sh;
            m.mainTexture = tex;
            EditorUtility.SetDirty(m);
        }
    }
}
