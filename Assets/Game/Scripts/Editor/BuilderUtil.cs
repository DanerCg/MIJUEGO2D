using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rescate.EditorTools
{
    /// <summary>Utilidades de construcción del proyecto (solo editor).</summary>
    public static class BuilderUtil
    {
        public const string Root = "Assets/Game";
        public const string Art = Root + "/Sprites";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";
        public const string Materials = Root + "/Materials";
        public const string ResourcesAudio = Root + "/Resources/Audio";
        public const string SourceArt = "Assets/Generated_Images";

        public const int LayerGround = 8;
        public const int LayerEnemy = 9;
        public const int LayerHazard = 10;

        static Font cachedFont;

        // ---------------------------------------------------------------- carpetas

        public static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static void EnsureProjectFolders()
        {
            EnsureFolder(Root + "/Scripts");
            EnsureFolder(Art);
            EnsureFolder(Prefabs);
            EnsureFolder(Scenes);
            EnsureFolder(Root + "/Animations");
            EnsureFolder(Materials);
            EnsureFolder(Root + "/Audio");
            EnsureFolder(Root + "/UI");
            EnsureFolder(Root + "/Resources");
            EnsureFolder(ResourcesAudio);
        }

        // ---------------------------------------------------------------- capas

        public static void SetupLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Builder] No se pudo abrir TagManager.asset");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null) return;

            SetLayerName(layers, LayerGround, "Ground");
            SetLayerName(layers, LayerEnemy, "Enemy");
            SetLayerName(layers, LayerHazard, "Hazard");
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        static void SetLayerName(SerializedProperty layers, int index, string name)
        {
            if (index < 0 || index >= layers.arraySize) return;
            SerializedProperty element = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(element.stringValue)) element.stringValue = name;
        }

        public static LayerMask MaskOf(params int[] layerIndices)
        {
            int mask = 0;
            for (int i = 0; i < layerIndices.Length; i++) mask |= 1 << layerIndices[i];
            return mask;
        }

        // ---------------------------------------------------------------- sprites

        public static Sprite ImportSprite(string sourceName, float ppu, SpriteAlignment alignment, bool fullRect = false)
        {
            EnsureFolder(Art);
            string src = SourceArt + "/" + sourceName + ".png";
            string dst = Art + "/" + sourceName + ".png";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(src) == null)
            {
                Debug.LogWarning("[Builder] Falta el sprite origen: " + src);
                return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
            }

            if (src != dst)
            {
                AssetDatabase.DeleteAsset(dst);
                string err = AssetDatabase.MoveAsset(src, dst);
                if (!string.IsNullOrEmpty(err))
                {
                    AssetDatabase.CopyAsset(src, dst);
                    Debug.LogWarning("[Builder] MoveAsset fallo: " + err);
                }
            }

            TextureImporter importer = AssetImporter.GetAtPath(dst) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(dst) as TextureImporter;
            }
            if (importer == null) return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = fullRect ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            settings.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            settings.spriteExtrude = 1;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
        }

        public static float NaturalHeight(Sprite sprite)
        {
            if (sprite == null) return 1f;
            return sprite.rect.height / sprite.pixelsPerUnit;
        }

        public static GameObject CreateSpriteObject(string name, Sprite sprite, Transform parent, Vector3 position,
            float desiredHeight = 0f, int sortingOrder = 0)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            if (desiredHeight > 0f && sprite != null)
            {
                float h = NaturalHeight(sprite);
                if (h > 0.001f)
                {
                    float k = desiredHeight / h;
                    go.transform.localScale = new Vector3(k, k, 1f);
                }
            }

            return go;
        }

        public static Material LoadOrCreateLitMaterial()
        {
            EnsureFolder(Materials);
            string path = Materials + "/SpriteLit.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // ---------------------------------------------------------------- prefabs

        public static GameObject SavePrefab(GameObject go, string folder, string prefabName)
        {
            EnsureFolder(folder);
            string path = folder + "/" + prefabName + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        // ---------------------------------------------------------------- UI

        public static Font UIFont
        {
            get
            {
                if (cachedFont != null) return cachedFont;
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (cachedFont == null) cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 24);
                return cachedFont;
            }
        }

        public static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color, bool stretch = true)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();

            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = color.a > 0.01f;
            return rt;
        }

        public static RectTransform CreateAnchored(Transform parent, string name, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size, bool bold = false)
        {
            RectTransform rt = CreateAnchored(parent, name, anchor, pivot, anchoredPos, size);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = UIFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (bold) text.fontStyle = FontStyle.Bold;
            return text;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            RectTransform rt = CreateAnchored(parent, name, anchor, pivot, anchoredPos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, Color bg, Color textColor, int fontSize = 34)
        {
            RectTransform rt = CreateAnchored(parent, name, anchor, pivot, anchoredPos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = bg;

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            button.colors = cb;

            Text text = CreateText(rt, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            text.fontStyle = FontStyle.Bold;

            return button;
        }

        public static Image CreateBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size,
            Color background, Color fill)
        {
            RectTransform bgRt = CreateAnchored(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPos, size);
            Image bgImg = bgRt.gameObject.AddComponent<Image>();
            bgImg.color = background;

            RectTransform fillRt = CreateAnchored(bgRt, "Fill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                new Vector2(size.x - 6f, size.y - 6f));
            Image fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.color = fill;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0.4f;
            fillImg.raycastTarget = false;

            return fillImg;
        }

        public static void OnClick(Button button, UnityAction action)
        {
            if (button == null || action == null) return;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        public static void OnSliderChanged(Slider slider, UnityAction<float> action)
        {
            if (slider == null || action == null) return;
            UnityEventTools.AddPersistentListener(slider.onValueChanged, action);
        }

        public static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color bg, float value)
        {
            RectTransform rt = CreateAnchored(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, size);
            Image bgImg = rt.gameObject.AddComponent<Image>();
            bgImg.color = bg;

            Slider slider = rt.gameObject.AddComponent<Slider>();

            RectTransform fillArea = CreateAnchored(rt, "FillArea", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0f), new Vector2(size.x - 8f, size.y));
            RectTransform fill = CreateAnchored(fillArea, "Fill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(size.x - 8f, size.y));
            Image fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0.4f, 0.9f, 1f, 0.95f);

            RectTransform handleArea = CreateAnchored(rt, "HandleArea", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 0f), new Vector2(size.x - 8f, size.y));
            RectTransform handle = CreateAnchored(handleArea, "Handle", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(26f, size.y + 8f));
            Image handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;

            return slider;
        }

        // ---------------------------------------------------------------- formas

        static Sprite whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null) return whiteSprite;
                EnsureFolder(Art);
                string path = Art + "/white.png";
                Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (existing != null)
                {
                    whiteSprite = existing;
                    return whiteSprite;
                }

                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color32[] px = new Color32[16];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 4f;
                    importer.filterMode = FilterMode.Point;
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }

                whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                return whiteSprite;
            }
        }

        /// <summary>Bloque con sprite repetido (suelo/plataformas) y collider sólido.</summary>
        public static GameObject CreateTiledBox(string name, Transform parent, Vector2 center, Vector2 size,
            Sprite tileSprite, int sortingOrder, int layer, bool withCollider = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = layer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tileSprite;
            sr.sortingOrder = sortingOrder;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;

            if (withCollider)
            {
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = size;
                box.offset = Vector2.zero;
            }

            return go;
        }

        /// <summary>Caja de color plano (decoración, barreras, relleno).</summary>
        public static GameObject CreateSolidBox(string name, Transform parent, Vector2 center, Vector2 size,
            Color color, int sortingOrder, int layer, bool withCollider = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = layer;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            if (withCollider)
            {
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = size;
                box.offset = Vector2.zero;
            }

            return go;
        }

        public static GameObject CreateTriggerBox(string name, Transform parent, Vector2 center, Vector2 size, int layer)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = layer;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = true;
            return go;
        }
    }
}
