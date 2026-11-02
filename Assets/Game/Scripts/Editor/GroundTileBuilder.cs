using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rescate.EditorTools
{
    /// <summary>
    /// Genera losetas de terreno y plataformas 100% opacas y repetibles sin costuras.
    /// Sustituye los PNG defectuosos (cuerpo de tierra casi transparente) que hacían
    /// que el suelo del Nivel 1 dejara ver el fondo a través de él.
    /// Se escribe sobre la misma ruta para conservar el GUID y todas las referencias.
    /// </summary>
    public static class GroundTileBuilder
    {
        const string Art = "Assets/Game/Sprites";
        const float TilePPU = 256f;

        [MenuItem("Rescate/Arte/Regenerar losetas de terreno", priority = 100)]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Art);

            // suelo (2 x 2 unidades, coincide con la altura de los bloques de suelo)
            Write("tile_forest", GroundForest(512, 512));
            Write("tile_mountain", GroundMountain(512, 512));
            Write("tile_castle", GroundCastle(512, 512));

            // plataformas (1 x 0,70 unidades, coincide con la altura de las plataformas)
            Write("plat_forest", Platform(256, 180, 3));
            Write("plat_mountain", Platform(256, 180, 4));
            Write("plat_castle", Platform(256, 180, 5));

            // relleno subterráneo (sin franja de hierba) para alargar el suelo
            // sin que la loseta repita la hierba al repetirse en vertical
            Write("fill_forest", Fill256(512, 512, 3));
            Write("fill_mountain", Fill256(512, 512, 4));
            Write("fill_castle", Fill256(512, 512, 5));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tiles] Losetas regeneradas (opacas y sin costuras).");
        }

        /// <summary>Color opaco a partir de componentes 0-255.</summary>
        static Color C(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        // ============================================================ SUELO

        static Color32[] GroundForest(int w, int h)
        {
            Color32[] px = Fill(w, h, new Color32(112, 70, 40, 255));

            Color dirtTop = C(126, 80, 46);
            Color dirtBottom = C(58, 34, 20);
            Color pebble = C(158, 138, 122);
            Color pebbleShade = C(96, 76, 62);

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float n = Noise(u, v, 11) * 0.5f + 0.5f;

                    Color c = Color.Lerp(dirtBottom, dirtTop, Mathf.Pow(v, 0.75f));
                    c = Color.Lerp(c, Shade(c, 1.14f), n * 0.55f);

                    // vetas finas (ruido de alta frecuencia, sin bandas grandes)
                    float vein = Noise(u, v * 3.2f, 77) * 0.5f + 0.5f;
                    if (vein > 0.86f) c = Color.Lerp(c, Shade(dirtBottom, 0.66f), 0.45f);
                    px[y * w + x] = c;
                }
            }

            System.Random rnd = new System.Random(4021);
            for (int i = 0; i < 46; i++)
            {
                int cx = rnd.Next(0, w);
                int cy = rnd.Next(8, h - 130);
                int rx = rnd.Next(6, 14);
                int ry = rnd.Next(5, 10);
                // sombra -> cuerpo -> luz superior: la piedra se lee como volumen
                Blob(px, w, h, cx, cy - 1, rx, ry, Shade(pebbleShade, 0.55f));
                Blob(px, w, h, cx, cy, rx - 1, ry - 1, pebbleShade);
                Blob(px, w, h, cx - 1, cy + 1, rx - 3, ry - 2, pebble);
                Blob(px, w, h, cx - 2, cy + 2, rx - 5, ry - 4, Shade(pebble, 1.22f));
            }

            Color grassLight = C(132, 192, 78);
            Color grassMid = C(88, 152, 52);
            Color grassDark = C(48, 98, 34);
            Color outline = C(38, 62, 22);

            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w;

                // borde superior: varias armónicas enteras para que la repetición
                // horizontal siga siendo perfecta pero el dibujo no sea un zigzag igual
                float edge = h * 0.74f
                           + 20f * Mathf.Sin(u * Mathf.PI * 2f * 1f + 0.4f)
                           + 13f * Mathf.Sin(u * Mathf.PI * 2f * 3f)
                           +  8f * Mathf.Sin(u * Mathf.PI * 2f * 5f + 1.1f)
                           +  5f * Mathf.Sin(u * Mathf.PI * 2f * 7f + 2.3f);

                // matas altas que rompen la línea recta de césped
                float clump = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(u * Mathf.PI * 2f * 11f + 0.7f)), 6f) * 24f;

                int edgeY = Mathf.Clamp(Mathf.RoundToInt(edge + clump), 8, h - 8);
                int skirt = Mathf.Max(0, edgeY - 12);

                for (int y = skirt; y < h; y++)
                {
                    float k = Mathf.InverseLerp(edgeY, h - 1, y);
                    Color c = k < 0.5f
                        ? Color.Lerp(grassDark, grassMid, k / 0.5f)
                        : Color.Lerp(grassMid, grassLight, (k - 0.5f) / 0.5f);

                    float blade = Mathf.Sin(u * w * 0.7f) * Mathf.Sin(u * w * 0.23f + y * 0.6f);
                    c = Color.Lerp(c, grassDark, Mathf.Clamp01(blade) * 0.16f);

                    px[y * w + x] = c;
                }

                // el césped se funde con la tierra en lugar de cortarse en seco
                for (int d = 0; d < 12; d++)
                {
                    int y = skirt + d;
                    if (y < 0 || y >= h) continue;
                    float a = 1f - d / 11f;
                    px[y * w + x] = Color.Lerp(px[y * w + x], outline, a * a * 0.7f);
                }
            }

            // briznas dentro de la franja superior (sin pasarse del borde de colisión)
            for (int i = 0; i < 150; i++)
            {
                int x = rnd.Next(0, w);
                int len = rnd.Next(6, 18);
                for (int k = 0; k < len; k++)
                {
                    int y = h - 1 - k;
                    if (y < 0) break;
                    Color c = Color.Lerp(grassLight, grassMid, 0.35f + 0.65f * k / (float)len);
                    px[y * w + Mathf.Clamp(x + (k / 6), 0, w - 1)] = c;
                }
            }

            return px;
        }

        static Color32[] GroundMountain(int w, int h)
        {
            Color32[] px = Fill(w, h, new Color32(96, 100, 110, 255));

            Color rockTop = C(146, 150, 160);
            Color rockBottom = C(48, 52, 62);
            Color crack = C(30, 33, 42);
            Color pebble = C(186, 190, 200);

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float n = Noise(u, v, 27) * 0.5f + 0.5f;

                    Color c = Color.Lerp(rockBottom, rockTop, Mathf.Pow(v, 0.85f));
                    c = Color.Lerp(c, Shade(c, 1.18f), n * 0.5f);

                    float strata = Mathf.Sin(v * Mathf.PI * 2f * 9f + n * 2.5f);
                    if (strata > 0.82f) c = Color.Lerp(c, crack, 0.55f);

                    px[y * w + x] = c;
                }
            }

            System.Random rnd = new System.Random(9021);
            for (int i = 0; i < 40; i++)
            {
                int cx = rnd.Next(0, w);
                int cy = rnd.Next(6, h - 120);
                int rx = rnd.Next(6, 16);
                int ry = rnd.Next(5, 11);
                Blob(px, w, h, cx, cy, rx, ry, C(60, 64, 76));
                Blob(px, w, h, cx, cy + 2, rx - 3, ry - 3, pebble);
            }

            Color cap = C(168, 174, 184);
            Color capDark = C(104, 110, 122);
            Color moss = C(92, 132, 78);
            Color outline = C(24, 28, 36);

            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w;
                float edge = h * 0.80f + 12f * Mathf.Sin(u * Mathf.PI * 2f * 2f)
                                       + 6f * Mathf.Sin(u * Mathf.PI * 2f * 5f + 2.3f);
                int edgeY = Mathf.Clamp(Mathf.RoundToInt(edge), 8, h - 8);

                for (int y = edgeY - 5; y < h; y++)
                {
                    if (y < 0) continue;
                    float k = Mathf.InverseLerp(edgeY, h - 1, y);
                    Color c = Color.Lerp(capDark, cap, k);
                    if (Noise(u, (float)y / h, 55) > 0.35f) c = Color.Lerp(c, moss, 0.28f);
                    px[y * w + x] = c;
                }

                for (int d = 1; d <= 4; d++)
                {
                    int y = edgeY - 5 + d;
                    if (y < 0 || y >= h) continue;
                    px[y * w + x] = Color.Lerp(px[y * w + x], outline, (1f - (d - 1) / 4f) * 0.8f);
                }
            }

            return px;
        }

        static Color32[] GroundCastle(int w, int h)
        {
            Color32[] px = new Color32[w * h];

            Color mortar = C(30, 27, 42);
            Color brickA = C(96, 88, 124);
            Color brickB = C(78, 71, 104);
            Color brickC = C(110, 101, 140);
            Color outline = C(18, 16, 28);

            int rows = 8;
            int rowH = h / rows;          // 64 px -> costura vertical exacta
            int brickW = w / 4;           // 128 px -> 4 ladrillos por fila
            int gap = 5;

            for (int y = 0; y < h; y++)
            {
                int row = y / rowH;
                int offY = y % rowH;
                int shift = (row % 2 == 0) ? 0 : brickW / 2;

                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    int bx = ((x + shift) % w + w) % w;
                    int brick = bx / brickW;
                    int offX = bx % brickW;

                    bool isGap = offY < gap || offY > rowH - gap - 1 || offX < gap || offX > brickW - gap - 1;

                    if (isGap)
                    {
                        px[y * w + x] = mortar;
                    }
                    else
                    {
                        int tone = (brick * 37 + row * 53) % 3;
                        Color baseCol = tone == 0 ? brickA : (tone == 1 ? brickB : brickC);
                        float shade = Mathf.Lerp(0.72f, 1.12f, 1f - Mathf.InverseLerp(0, rowH, offY));
                        Color c = Shade(baseCol, shade);

                        if (offY < gap + 3) c = Color.Lerp(c, Color.white, 0.16f);
                        if (offY > rowH - gap - 4) c = Color.Lerp(c, Color.black, 0.22f);

                        float n = Noise((float)x / w, v, 61) * 0.5f + 0.5f;
                        c = Color.Lerp(c, Shade(c, 0.86f), n * 0.5f);

                        px[y * w + x] = c;
                    }
                }
            }

            System.Random rnd = new System.Random(1777);
            for (int i = 0; i < 26; i++)
            {
                int cx = rnd.Next(0, w);
                int cy = rnd.Next(20, h - 20);
                int rx = rnd.Next(4, 10);
                Blob(px, w, h, cx, cy, rx, rx - 2, C(70, 62, 92));
            }

            for (int x = 0; x < w; x++)
            {
                for (int y = h - 14; y < h; y++)
                {
                    float k = Mathf.InverseLerp(h - 14, h - 1, y);
                    px[y * w + x] = Color.Lerp(C(120, 112, 152), C(150, 142, 180), k);
                }
                px[(h - 15) * w + x] = outline;
            }

            return px;
        }

        // ============================================================ PLATAFORMAS

        static Color32[] Platform(int w, int h, int theme)
        {
            Color32[] px = new Color32[w * h];

            Color bodyTop, bodyBottom, capLight, capDark, outline, pebble;
            if (theme == 3)      // bosque
            {
                bodyTop = C(112, 72, 42); bodyBottom = C(58, 34, 20);
                capLight = C(158, 220, 70); capDark = C(58, 118, 26);
                outline = C(34, 24, 14); pebble = C(150, 130, 114);
            }
            else if (theme == 4) // montaña
            {
                bodyTop = C(120, 124, 134); bodyBottom = C(52, 56, 68);
                capLight = C(178, 184, 194); capDark = C(96, 102, 114);
                outline = C(24, 28, 36); pebble = C(190, 194, 204);
            }
            else                 // castillo
            {
                bodyTop = C(100, 92, 128); bodyBottom = C(46, 41, 62);
                capLight = C(148, 140, 178); capDark = C(74, 68, 100);
                outline = C(18, 16, 28); pebble = C(146, 138, 176);
            }

            int capH = Mathf.RoundToInt(h * 0.34f);
            float bodyH = Mathf.Max(1, h - capH);

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float n = Noise(u, v, theme * 13) * 0.5f + 0.5f;
                    Color c;
                    if (y >= h - capH)
                    {
                        float k = Mathf.InverseLerp(h - capH, h - 1, y);
                        c = Color.Lerp(capDark, capLight, k);
                    }
                    else
                    {
                        c = Color.Lerp(bodyBottom, bodyTop, Mathf.Pow(y / bodyH, 0.8f));
                    }
                    c = Color.Lerp(c, Shade(c, 1.15f), n * 0.5f);
                    px[y * w + x] = c;
                }
            }

            System.Random rnd = new System.Random(theme * 991 + 17);
            for (int i = 0; i < 12; i++)
            {
                int cx = rnd.Next(0, w);
                int cy = rnd.Next(6, Mathf.Max(7, h - capH - 4));
                int rx = rnd.Next(3, 7);
                Blob(px, w, h, cx, cy, rx, rx - 2, pebble);
            }

            for (int x = 0; x < w; x++)
            {
                for (int k = 0; k < 4; k++)
                    px[k * w + x] = Color.Lerp(px[k * w + x], outline, 0.85f - k * 0.18f);
                px[(h - 1) * w + x] = Color.Lerp(px[(h - 1) * w + x], Color.white, 0.18f);

                int edgeY = h - capH;
                if (edgeY > 0 && edgeY < h) px[edgeY * w + x] = Color.Lerp(px[edgeY * w + x], outline, 0.55f);
            }

            return px;
        }

        // ============================================================ RELLENO

        static Color32[] Fill256(int w, int h, int theme)
        {
            Color32[] px = new Color32[w * h];
            Color top, bottom, pebble;
            if (theme == 3)      { top = C(112, 70, 40); bottom = C(44, 26, 15); pebble = C(140, 120, 104); }
            else if (theme == 4) { top = C(110, 114, 124); bottom = C(38, 42, 52); pebble = C(172, 176, 186); }
            else                 { top = C(88, 80, 112); bottom = C(34, 30, 46); pebble = C(126, 118, 152); }

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float n = Noise(u, v, theme * 7 + 3) * 0.5f + 0.5f;
                    Color c = Color.Lerp(bottom, top, Mathf.Pow(v, 0.7f));
                    c = Color.Lerp(c, Shade(c, 1.12f), n * 0.5f);
                    float vein = Noise(u, v * 3.4f, theme * 11 + 5) * 0.5f + 0.5f;
                    if (vein > 0.87f) c = Color.Lerp(c, Shade(bottom, 0.7f), 0.45f);
                    px[y * w + x] = c;
                }
            }

            System.Random rnd = new System.Random(theme * 137 + 29);
            for (int i = 0; i < 34; i++)
            {
                int cx = rnd.Next(0, w);
                int cy = rnd.Next(6, h - 6);
                int rx = rnd.Next(6, 14);
                int ry = rnd.Next(5, 10);
                Blob(px, w, h, cx, cy - 1, rx, ry, Shade(pebble, 0.45f));
                Blob(px, w, h, cx - 1, cy, rx - 3, ry - 2, pebble);
            }

            return px;
        }

        // ============================================================ UTILIDADES

        /// <summary>Multiplica solo el RGB: conserva alfa 255 y evita zonas semitransparentes.</summary>
        static Color Shade(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, 1f);
        }

        static Color32[] Fill(int w, int h, Color32 c)
        {
            Color32[] px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            return px;
        }

        /// <summary>Elipse rellena con envolvimiento horizontal (costura invisible).</summary>
        static void Blob(Color32[] px, int w, int h, int cx, int cy, int rx, int ry, Color color)
        {
            if (rx <= 0 || ry <= 0) return;
            for (int dy = -ry; dy <= ry; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= h) continue;
                for (int dx = -rx; dx <= rx; dx++)
                {
                    float d = (dx * dx) / (float)(rx * rx) + (dy * dy) / (float)(ry * ry);
                    if (d > 1f) continue;
                    int x = ((cx + dx) % w + w) % w;
                    float a = Mathf.Clamp01((1f - d) * 2.2f);
                    px[y * w + x] = Color.Lerp(px[y * w + x], color, a * 0.9f);
                }
            }
        }

        /// <summary>Ruido periódico en X (frecuencias enteras) => repetición sin costuras.</summary>
        static float Noise(float u, float v, int seed)
        {
            float s = seed * 0.618f;
            float n = Mathf.Sin(2f * Mathf.PI * (u * 1f) + s) * 0.55f;
            n += Mathf.Sin(2f * Mathf.PI * (u * 3f + v * 1.7f) + s * 1.7f) * 0.30f;
            n += Mathf.Sin(2f * Mathf.PI * (u * 7f - v * 2.3f) + s * 2.9f) * 0.16f;
            n += Mathf.Sin(2f * Mathf.PI * (u * 13f + v * 4.1f) + s * 4.1f) * 0.09f;
            return Mathf.Clamp(n, -1f, 1f);
        }

        static void Write(string name, Color32[] pixels)
        {
            int w = name.StartsWith("plat") ? 256 : 512;
            int h = name.StartsWith("plat") ? 180 : 512;
            if (pixels.Length != w * h)
            {
                Debug.LogError("[Tiles] Longitud inesperada para " + name + ": " + pixels.Length);
                return;
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();

            string path = Art + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Tiles] No se pudo configurar el importador de " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TilePPU;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;   // requisito de SpriteDrawMode.Tiled
            settings.spriteExtrude = 1;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
