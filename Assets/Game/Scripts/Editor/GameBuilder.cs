using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rescate.EditorTools
{
    /// <summary>
    /// Construye arte, prefabs y escenas del juego. Cada método público es un paso
    /// ejecutable de forma independiente.
    /// </summary>
    public static class GameBuilder
    {
        // ------------------------------------------------------------ helpers

        static Sprite S(string name) { return AssetDatabase.LoadAssetAtPath<Sprite>(BuilderUtil.Art + "/" + name + ".png"); }

        static GameObject P(string name) { return AssetDatabase.LoadAssetAtPath<GameObject>(BuilderUtil.Prefabs + "/" + name + ".prefab"); }

        static GameObject Spawn(string prefabName, Vector3 pos, Transform parent)
        {
            GameObject prefab = P(prefabName);
            if (prefab == null)
            {
                Debug.LogWarning("[Builder] Prefab no encontrado: " + prefabName);
                return null;
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            return go;
        }

        static SpriteRenderer AddVisual(GameObject parent, string spriteName, float height, int order,
            Vector3 localPos, Material mat, Color? tint = null)
        {
            Sprite sprite = S(spriteName);
            float natural = BuilderUtil.NaturalHeight(sprite);
            float k = natural > 0.001f ? height / natural : 1f;

            GameObject vis = new GameObject("Visual");
            vis.transform.SetParent(parent.transform, false);
            vis.transform.localPosition = localPos;
            vis.transform.localScale = new Vector3(k, k, 1f);

            SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (mat != null) sr.sharedMaterial = mat;
            if (tint.HasValue) sr.color = tint.Value;
            return sr;
        }

        // ============================================================ ARTE

        public static void BuildArt()
        {
            BuilderUtil.EnsureProjectFolders();
            BuilderUtil.SetupLayers();

            BuilderUtil.ImportSprite("nico", 100f, SpriteAlignment.BottomCenter);
            BuilderUtil.ImportSprite("slime", 100f, SpriteAlignment.BottomCenter);
            BuilderUtil.ImportSprite("bat", 100f, SpriteAlignment.Center);
            BuilderUtil.ImportSprite("guardian", 100f, SpriteAlignment.BottomCenter);
            BuilderUtil.ImportSprite("maldrak", 100f, SpriteAlignment.BottomCenter);
            BuilderUtil.ImportSprite("elena", 100f, SpriteAlignment.BottomCenter);

            BuilderUtil.ImportSprite("coin", 100f, SpriteAlignment.Center);
            BuilderUtil.ImportSprite("crystal", 100f, SpriteAlignment.Center);
            BuilderUtil.ImportSprite("heart_pickup", 100f, SpriteAlignment.Center);
            BuilderUtil.ImportSprite("key", 100f, SpriteAlignment.Center);
            BuilderUtil.ImportSprite("spike", 100f, SpriteAlignment.Center);

            BuilderUtil.ImportSprite("tile_forest", 512f, SpriteAlignment.Center, true);
            BuilderUtil.ImportSprite("tile_mountain", 512f, SpriteAlignment.Center, true);
            BuilderUtil.ImportSprite("tile_castle", 512f, SpriteAlignment.Center, true);

            BuilderUtil.ImportSprite("bg_forest", 100f, SpriteAlignment.Center);
            BuilderUtil.ImportSprite("bg_mountain", 100f, SpriteAlignment.Center);
            BuilderUtil.ImportSprite("bg_castle", 100f, SpriteAlignment.Center);

            BuilderUtil.LoadOrCreateLitMaterial();
            Sprite white = BuilderUtil.WhiteSprite;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Builder] Arte importado. white=" + (white != null));
        }

        // ============================================================ PREFABS

        public static void BuildPrefabs()
        {
            BuilderUtil.EnsureProjectFolders();
            Material lit = BuilderUtil.LoadOrCreateLitMaterial();

            // ---------- proyectiles
            GameObject magic = new GameObject("MagicProjectile");
            AddVisual(magic, "crystal", 0.55f, 25, Vector3.zero, lit, new Color(0.55f, 0.95f, 1f, 1f));
            Rigidbody2D mrb = magic.AddComponent<Rigidbody2D>();
            mrb.bodyType = RigidbodyType2D.Kinematic;
            mrb.gravityScale = 0f;
            CircleCollider2D mcol = magic.AddComponent<CircleCollider2D>();
            mcol.isTrigger = true;
            mcol.radius = 0.25f;
            MagicProjectile mp = magic.AddComponent<MagicProjectile>();
            mp.hitLayers = BuilderUtil.MaskOf(BuilderUtil.LayerGround);
            BuilderUtil.SavePrefab(magic, BuilderUtil.Prefabs, "MagicProjectile");

            GameObject eproj = new GameObject("EnemyProjectile");
            AddVisual(eproj, "crystal", 0.7f, 25, Vector3.zero, lit, new Color(0.55f, 1f, 0.45f, 1f));
            Rigidbody2D erb = eproj.AddComponent<Rigidbody2D>();
            erb.bodyType = RigidbodyType2D.Kinematic;
            erb.gravityScale = 0f;
            CircleCollider2D ecol = eproj.AddComponent<CircleCollider2D>();
            ecol.isTrigger = true;
            ecol.radius = 0.3f;
            EnemyProjectile ep = eproj.AddComponent<EnemyProjectile>();
            ep.damage = 1;
            ep.groundLayers = BuilderUtil.MaskOf(BuilderUtil.LayerGround);
            BuilderUtil.SavePrefab(eproj, BuilderUtil.Prefabs, "EnemyProjectile");

            // ---------- jugador
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = 0;

            Rigidbody2D prb = player.AddComponent<Rigidbody2D>();
            prb.gravityScale = 2.6f;
            prb.freezeRotation = true;
            prb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            prb.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D pcol = player.AddComponent<BoxCollider2D>();
            pcol.size = new Vector2(0.75f, 1.7f);
            pcol.offset = new Vector2(0f, 0.86f);

            SpriteRenderer pvis = AddVisual(player, "nico", 1.95f, 20, Vector3.zero, lit);

            PlayerVisuals visuals = player.AddComponent<PlayerVisuals>();
            visuals.spriteRenderer = pvis;
            visuals.visualRoot = pvis.transform;

            PlayerController2D ctrl = player.AddComponent<PlayerController2D>();
            ctrl.groundLayers = BuilderUtil.MaskOf(BuilderUtil.LayerGround);
            ctrl.enemyLayers = BuilderUtil.MaskOf(BuilderUtil.LayerEnemy);
            ctrl.visuals = visuals;
            ctrl.projectilePrefab = AssetDatabase.LoadAssetAtPath<MagicProjectile>(BuilderUtil.Prefabs + "/MagicProjectile.prefab");

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            ctrl.groundCheck = groundCheck.transform;

            GameObject attackOrigin = new GameObject("AttackOrigin");
            attackOrigin.transform.SetParent(player.transform, false);
            attackOrigin.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            ctrl.attackOrigin = attackOrigin.transform;

            PlayerStats stats = player.AddComponent<PlayerStats>();
            stats.maxHearts = 3;
            stats.hearts = 3;

            BuilderUtil.SavePrefab(player, BuilderUtil.Prefabs, "Player");

            // ---------- enemigos
            GameObject slime = new GameObject("Slime");
            slime.layer = BuilderUtil.LayerEnemy;
            Rigidbody2D srb = slime.AddComponent<Rigidbody2D>();
            srb.gravityScale = 3f;
            srb.freezeRotation = true;
            BoxCollider2D scol = slime.AddComponent<BoxCollider2D>();
            scol.size = new Vector2(0.8f, 0.65f);
            scol.offset = new Vector2(0f, 0.34f);
            AddVisual(slime, "slime", 0.95f, 10, Vector3.zero, lit);
            SlimeEnemy se = slime.AddComponent<SlimeEnemy>();
            se.maxHealth = 2;
            se.damageToPlayer = 1;
            se.xpReward = 25;
            se.coinsReward = 1;
            se.groundLayers = BuilderUtil.MaskOf(BuilderUtil.LayerGround);
            BuilderUtil.SavePrefab(slime, BuilderUtil.Prefabs, "Slime");

            GameObject bat = new GameObject("Bat");
            bat.layer = BuilderUtil.LayerEnemy;
            CircleCollider2D bcol = bat.AddComponent<CircleCollider2D>();
            bcol.isTrigger = true;
            bcol.radius = 0.5f;
            bcol.offset = new Vector2(0f, 0.1f);
            AddVisual(bat, "bat", 1.15f, 11, Vector3.zero, lit);
            BatEnemy be = bat.AddComponent<BatEnemy>();
            be.maxHealth = 2;
            be.damageToPlayer = 1;
            be.xpReward = 40;
            be.coinsReward = 2;
            BuilderUtil.SavePrefab(bat, BuilderUtil.Prefabs, "Bat");

            GameObject guardian = new GameObject("Guardian");
            guardian.layer = BuilderUtil.LayerEnemy;
            Rigidbody2D grb = guardian.AddComponent<Rigidbody2D>();
            grb.gravityScale = 4f;
            grb.freezeRotation = true;
            BoxCollider2D gcol = guardian.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(1.15f, 1.95f);
            gcol.offset = new Vector2(0f, 0.98f);
            AddVisual(guardian, "guardian", 2.2f, 10, Vector3.zero, lit);
            StoneGuardian sg = guardian.AddComponent<StoneGuardian>();
            sg.maxHealth = 5;
            sg.damageToPlayer = 2;
            sg.xpReward = 70;
            sg.coinsReward = 3;
            sg.groundLayers = BuilderUtil.MaskOf(BuilderUtil.LayerGround);
            BuilderUtil.SavePrefab(guardian, BuilderUtil.Prefabs, "Guardian");

            GameObject boss = new GameObject("Maldrak");
            boss.layer = BuilderUtil.LayerEnemy;
            BoxCollider2D bocol = boss.AddComponent<BoxCollider2D>();
            bocol.isTrigger = true;
            bocol.size = new Vector2(1.4f, 2.4f);
            bocol.offset = new Vector2(0f, 1.2f);
            AddVisual(boss, "maldrak", 2.8f, 14, Vector3.zero, lit);
            BossMaldrak bm = boss.AddComponent<BossMaldrak>();
            bm.maxHealth = 30;
            bm.damageToPlayer = 1;
            bm.xpReward = 250;
            bm.coinsReward = 25;
            bm.projectilePrefab = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(BuilderUtil.Prefabs + "/EnemyProjectile.prefab");
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(boss.transform, false);
            firePoint.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            bm.firePoint = firePoint.transform;
            bm.floatAmplitude = 0.4f;
            BuilderUtil.SavePrefab(boss, BuilderUtil.Prefabs, "Maldrak");

            // ---------- recolectables
            BuildCollectible("CoinPickup", "coin", 0.6f, CollectibleType.Coin, Color.white, 6);
            BuildCollectible("CrystalPickup", "crystal", 0.85f, CollectibleType.Crystal, Color.white, 7);
            BuildCollectible("HeartPickup", "heart_pickup", 0.65f, CollectibleType.Heart, Color.white, 7);
            BuildCollectible("KeyPickup", "key", 0.95f, CollectibleType.Key, Color.white, 9);
            BuildCollectible("XpPickup", "crystal", 0.45f, CollectibleType.Xp, new Color(0.6f, 1f, 0.5f, 1f), 7);

            // ---------- checkpoint
            GameObject checkpoint = new GameObject("Checkpoint");
            BuilderUtil.CreateSolidBox("Pole", checkpoint.transform, new Vector2(0f, 1f), new Vector2(0.16f, 2f),
                new Color(0.3f, 0.22f, 0.15f, 1f), 7, 0, false);
            GameObject flag = BuilderUtil.CreateSolidBox("Flag", checkpoint.transform, new Vector2(0.5f, 1.65f),
                new Vector2(0.9f, 0.6f), Color.white, 8, 0, false);
            BoxCollider2D ccol = checkpoint.AddComponent<BoxCollider2D>();
            ccol.isTrigger = true;
            ccol.size = new Vector2(1.8f, 2.2f);
            ccol.offset = new Vector2(0f, 1.1f);
            Checkpoint cp = checkpoint.AddComponent<Checkpoint>();
            cp.flagRenderer = flag.GetComponent<SpriteRenderer>();
            BuilderUtil.SavePrefab(checkpoint, BuilderUtil.Prefabs, "Checkpoint");

            // ---------- pinchos
            GameObject spike = new GameObject("Spike");
            spike.layer = BuilderUtil.LayerHazard;
            AddVisual(spike, "spike", 2f, 8, Vector3.zero, null);
            BoxCollider2D spcol = spike.AddComponent<BoxCollider2D>();
            spcol.isTrigger = true;
            spcol.size = new Vector2(1.8f, 0.45f);
            spcol.offset = new Vector2(0f, 0.02f);
            Hazard hz = spike.AddComponent<Hazard>();
            hz.damage = 1;
            BuilderUtil.SavePrefab(spike, BuilderUtil.Prefabs, "Spike");

            // ---------- princesa
            GameObject princess = new GameObject("PrincessElena");
            SpriteRenderer elenaSr = AddVisual(princess, "elena", 1.95f, 12, Vector3.zero, lit);
            BoxCollider2D elcol = princess.AddComponent<BoxCollider2D>();
            elcol.isTrigger = true;
            elcol.size = new Vector2(1.2f, 2f);
            elcol.offset = new Vector2(0f, 1f);
            PrincessElena pe = princess.AddComponent<PrincessElena>();
            pe.spriteRenderer = elenaSr;
            GameObject bars = new GameObject("PrisonBars");
            bars.transform.SetParent(princess.transform, false);
            for (int i = 0; i < 5; i++)
            {
                BuilderUtil.CreateSolidBox("Bar" + i, bars.transform, new Vector2(-1.6f + i * 0.8f, 1.3f),
                    new Vector2(0.14f, 2.6f), new Color(0.45f, 0.4f, 0.6f, 0.95f), 15, 0, false);
            }
            pe.prisonBars = bars;
            BuilderUtil.SavePrefab(princess, BuilderUtil.Prefabs, "PrincessElena");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Builder] Prefabs creados.");
        }

        static void BuildCollectible(string name, string spriteName, float height, Rescate.CollectibleType type, Color tint, int order)
        {
            GameObject go = new GameObject(name);
            go.layer = 0;
            AddVisual(go, spriteName, height, order, Vector3.zero, null, tint);
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.42f;
            Collectible c = go.AddComponent<Collectible>();
            c.type = type;
            c.amount = 1;
            BuilderUtil.SavePrefab(go, BuilderUtil.Prefabs, name);
        }

        // ============================================================ ESCENAS

        static Scene CreateEmptyScene(string sceneName)
        {
            BuilderUtil.EnsureFolder(BuilderUtil.Scenes);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, BuilderUtil.Scenes + "/" + sceneName + ".unity");
            return scene;
        }

        static void SaveActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static GameObject CreateBackdrop(string bgName, float height, float y)
        {
            GameObject back = BuilderUtil.CreateSpriteObject("Backdrop", S(bgName), null, new Vector3(0f, y, 0f), height, -100);
            Parallax px = back.AddComponent<Parallax>();
            px.factor = 1f;
            px.followY = true;
            return back;
        }

        static Camera CreateCamera(Vector2 boundsMin, Vector2 boundsMax, Color bgColor, Transform target)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            CameraFollow2D follow = camGo.AddComponent<CameraFollow2D>();
            follow.target = target;
            follow.boundsMin = boundsMin;
            follow.boundsMax = boundsMax;
            follow.useBounds = true;
            follow.verticalOffset = 1.6f;

            camGo.AddComponent<AudioListener>();
            return cam;
        }

        static GameObject CreateEventSystem()
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            return es;
        }

        /// <summary>HUD + controles táctiles + pausa + game over.</summary>
        static Canvas CreateGameHUD(string objective)
        {
            Canvas canvas = BuilderUtil.CreateCanvas("LevelCanvas");
            Transform root = canvas.transform;

            RectTransform hud = BuilderUtil.CreatePanel(root, "HUD", new Color(0f, 0f, 0f, 0f));

            HUDController hudScript = canvas.gameObject.AddComponent<HUDController>();

            Sprite heartSprite = S("heart_pickup");
            Image[] hearts = new Image[5];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = BuilderUtil.CreateImage(hud, "Heart" + i, heartSprite, Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f + i * 60f, -42f), new Vector2(52f, 52f));
            }

            BuilderUtil.CreateImage(hud, "CoinIcon", S("coin"), Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -108f), new Vector2(42f, 42f));
            Text coinText = BuilderUtil.CreateText(hud, "CoinText", "x 0", 34, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -108f), new Vector2(200f, 42f), true);

            BuilderUtil.CreateImage(hud, "KeyIcon", S("key"), Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -160f), new Vector2(42f, 42f));
            Text keyText = BuilderUtil.CreateText(hud, "KeyText", "x 0/3", 34, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -160f), new Vector2(200f, 42f), true);

            Image xpFill = BuilderUtil.CreateBar(hud, "XpBar", new Vector2(44f, -212f), new Vector2(320f, 24f),
                new Color(0f, 0f, 0f, 0.55f), new Color(0.45f, 0.95f, 1f, 0.95f));
            Text levelText = BuilderUtil.CreateText(hud, "LevelText", "NV 1", 28, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(374f, -212f), new Vector2(160f, 24f), true);

            Text objectiveText = BuilderUtil.CreateText(hud, "ObjectiveText", "OBJETIVO: " + objective, 30,
                new Color(1f, 0.95f, 0.55f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -38f), new Vector2(1200f, 40f), true);

            Text messageText = BuilderUtil.CreateText(hud, "MessageText", "", 40, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1500f, 70f), true);

            hudScript.hearts = hearts;
            hudScript.coinsText = coinText;
            hudScript.keysText = keyText;
            hudScript.levelText = levelText;
            hudScript.objectiveText = objectiveText;
            hudScript.messageText = messageText;
            hudScript.xpFill = xpFill;

            // ---------- controles táctiles
            RectTransform touch = BuilderUtil.CreatePanel(root, "TouchControls", new Color(0f, 0f, 0f, 0f));

            Button btnLeft = BuilderUtil.CreateButton(touch, "BtnLeft", "<", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(60f, 60f), new Vector2(130f, 130f), new Color(1f, 1f, 1f, 0.25f), Color.white, 60);
            TouchButton tbLeft = btnLeft.gameObject.AddComponent<TouchButton>();
            tbLeft.action = TouchButton.TouchAction.Left;
            tbLeft.background = btnLeft.GetComponent<Image>();

            Button btnRight = BuilderUtil.CreateButton(touch, "BtnRight", ">", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(210f, 60f), new Vector2(130f, 130f), new Color(1f, 1f, 1f, 0.25f), Color.white, 60);
            TouchButton tbRight = btnRight.gameObject.AddComponent<TouchButton>();
            tbRight.action = TouchButton.TouchAction.Right;
            tbRight.background = btnRight.GetComponent<Image>();

            Button btnRun = BuilderUtil.CreateButton(touch, "BtnRun", "RUN", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(80f, 210f), new Vector2(150f, 70f), new Color(1f, 1f, 1f, 0.2f), Color.white, 28);
            TouchButton tbRun = btnRun.gameObject.AddComponent<TouchButton>();
            tbRun.action = TouchButton.TouchAction.Run;
            tbRun.background = btnRun.GetComponent<Image>();

            Button btnJump = BuilderUtil.CreateButton(touch, "BtnJump", "SALTO", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-90f, 60f), new Vector2(170f, 130f), new Color(0.4f, 0.9f, 1f, 0.35f), Color.white, 34);
            TouchButton tbJump = btnJump.gameObject.AddComponent<TouchButton>();
            tbJump.action = TouchButton.TouchAction.Jump;
            tbJump.background = btnJump.GetComponent<Image>();

            Button btnAttack = BuilderUtil.CreateButton(touch, "BtnAttack", "GOLPE", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-280f, 60f), new Vector2(170f, 130f), new Color(1f, 0.55f, 0.3f, 0.35f), Color.white, 34);
            TouchButton tbAttack = btnAttack.gameObject.AddComponent<TouchButton>();
            tbAttack.action = TouchButton.TouchAction.Attack;
            tbAttack.background = btnAttack.GetComponent<Image>();

            // ---------- pausa
            PauseMenuUI pause = canvas.gameObject.AddComponent<PauseMenuUI>();

            RectTransform pausePanel = BuilderUtil.CreatePanel(root, "PausePanel", new Color(0.02f, 0.03f, 0.08f, 0.88f));
            BuilderUtil.CreateText(pausePanel, "Title", "PAUSA", 96, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(900f, 120f), true);

            Button btnResume = BuilderUtil.CreateButton(pausePanel, "BtnResume", "CONTINUAR", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(560f, 100f), new Color(0.15f, 0.45f, 0.85f, 0.95f), Color.white);
            Button btnRestart = BuilderUtil.CreateButton(pausePanel, "BtnRestart", "REINICIAR NIVEL", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(560f, 100f), new Color(0.25f, 0.3f, 0.45f, 0.95f), Color.white);
            Button btnToMenu = BuilderUtil.CreateButton(pausePanel, "BtnToMenu", "MENU PRINCIPAL", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(560f, 100f), new Color(0.35f, 0.18f, 0.2f, 0.95f), Color.white);

            BuilderUtil.OnClick(btnResume, pause.Resume);
            BuilderUtil.OnClick(btnRestart, pause.RestartLevel);
            BuilderUtil.OnClick(btnToMenu, pause.GoToMenu);
            pause.pausePanel = pausePanel.gameObject;
            pausePanel.gameObject.SetActive(false);

            Button btnPause = BuilderUtil.CreateButton(touch, "BtnPause", "II", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -40f), new Vector2(96f, 96f), new Color(1f, 1f, 1f, 0.28f), Color.white, 40);
            BuilderUtil.OnClick(btnPause, pause.Toggle);

            // ---------- game over
            GameOverUI gameOver = canvas.gameObject.AddComponent<GameOverUI>();

            RectTransform overPanel = BuilderUtil.CreatePanel(root, "GameOverPanel", new Color(0.12f, 0.01f, 0.03f, 0.9f));
            BuilderUtil.CreateText(overPanel, "Title", "FIN DEL JUEGO", 96, new Color(1f, 0.4f, 0.45f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(1100f, 130f), true);
            BuilderUtil.CreateText(overPanel, "Subtitle", "Nico necesita un respiro...", 40, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(1100f, 60f));

            Button btnRetry = BuilderUtil.CreateButton(overPanel, "BtnRetry", "REINTENTAR", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(560f, 100f), new Color(0.85f, 0.45f, 0.15f, 0.95f), Color.white);
            Button btnOverMenu = BuilderUtil.CreateButton(overPanel, "BtnOverMenu", "MENU PRINCIPAL", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(560f, 100f), new Color(0.3f, 0.32f, 0.45f, 0.95f), Color.white);

            BuilderUtil.OnClick(btnRetry, gameOver.Retry);
            BuilderUtil.OnClick(btnOverMenu, gameOver.GoToMenu);
            gameOver.panel = overPanel.gameObject;
            overPanel.gameObject.SetActive(false);

            CreateEventSystem();
            return canvas;
        }

        // ------------------------------------------------------------ nivel 1

        public static void BuildLevel1()
        {
            CreateEmptyScene(GameManager.SceneLevel1);
            Sprite tile = S("tile_forest");

            CreateBackdrop("bg_forest", 15f, 3f);

            GameObject level = new GameObject("Level");
            Transform root = level.transform;

            // suelo
            Ground(root, -10f, 30f, 0f, tile);
            Ground(root, 35f, 62f, 0f, tile);
            Ground(root, 68f, 96f, 0f, tile);
            Ground(root, 102f, 136f, 0f, tile);

            // plataformas
            Platform(root, 24f, 3f, 5f, tile);
            Platform(root, 40f, 4f, 5f, tile);
            Platform(root, 48f, 6.5f, 4f, tile);
            Platform(root, 56f, 4.5f, 4f, tile);
            Platform(root, 74f, 3f, 6f, tile);
            Platform(root, 84f, 6f, 5f, tile);
            Platform(root, 92f, 3.5f, 4f, tile);
            Platform(root, 110f, 3f, 5f, tile);
            Platform(root, 120f, 5.5f, 4f, tile);

            // monedas
            float[,] coins =
            {
                { 10f, 1f }, { 13f, 1f }, { 16f, 1f }, { 24f, 4.2f }, { 28f, 1f },
                { 40f, 5.2f }, { 44f, 5.2f }, { 48f, 7.7f }, { 52f, 7.7f }, { 56f, 5.7f },
                { 70f, 1f }, { 74f, 4.2f }, { 78f, 4.2f }, { 84f, 7.2f }, { 88f, 7.2f },
                { 92f, 4.7f }, { 106f, 1f }, { 110f, 4.2f }, { 114f, 4.2f }, { 120f, 6.7f },
                { 124f, 1f }, { 128f, 1f }
            };
            for (int i = 0; i < coins.GetLength(0); i++) Spawn("CoinPickup", new Vector3(coins[i, 0], coins[i, 1], 0f), root);

            Spawn("CrystalPickup", new Vector3(33f, 3.5f, 0f), root);
            Spawn("CrystalPickup", new Vector3(60f, 3.2f, 0f), root);
            Spawn("CrystalPickup", new Vector3(99f, 3.5f, 0f), root);
            Spawn("HeartPickup", new Vector3(86f, 1f, 0f), root);
            Spawn("KeyPickup", new Vector3(84f, 7f, 0f), root);

            // enemigos
            Spawn("Slime", new Vector3(18f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(26f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(24f, 3.2f, 0f), root);
            Spawn("Slime", new Vector3(45f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(55f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(72f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(90f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(112f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(120f, 0.1f, 0f), root);

            // punto de control y salida
            Spawn("Checkpoint", new Vector3(69f, 0f, 0f), root);

            GameObject exit = BuildExitDoor(root, new Vector3(130f, 0f, 0f), 1, "Puerta del Bosque");

            // zona de caída
            GameObject pit = BuilderUtil.CreateTriggerBox("PitKill", root, new Vector2(63f, -14f), new Vector2(220f, 4f), BuilderUtil.LayerHazard);
            Hazard pitHazard = pit.AddComponent<Hazard>();
            pitHazard.damage = 99;

            // jugador y cámara
            GameObject player = Spawn("Player", new Vector3(-6f, 0.2f, 0f), null);
            CreateCamera(new Vector2(-12f, -4f), new Vector2(138f, 16f), new Color(0.09f, 0.16f, 0.22f, 1f), player.transform);
            CreateGameHUD("Encuentra la llave mágica y abre la puerta del bosque");

            SaveActiveScene();
            AddSceneToBuild(GameManager.SceneLevel1);
            Debug.Log("[Builder] Nivel 1 creado.");
        }

        // ------------------------------------------------------------ nivel 2

        public static void BuildLevel2()
        {
            CreateEmptyScene(GameManager.SceneLevel2);
            Sprite tile = S("tile_mountain");

            CreateBackdrop("bg_mountain", 15f, 4f);

            GameObject level = new GameObject("Level");
            Transform root = level.transform;

            Ground(root, -10f, 22f, 0f, tile);
            Ground(root, 30f, 46f, 2f, tile);
            Ground(root, 56f, 70f, 4f, tile);
            Ground(root, 78f, 92f, 1f, tile);
            Ground(root, 100f, 120f, 3f, tile);
            Ground(root, 126f, 142f, 0f, tile);

            Platform(root, 14f, 3.5f, 4f, tile);
            Platform(root, 38f, 6f, 5f, tile);
            Platform(root, 62f, 8.5f, 5f, tile);
            Platform(root, 86f, 5f, 5f, tile);
            Platform(root, 108f, 7.5f, 5f, tile);
            Platform(root, 132f, 4.5f, 4f, tile);

            // plataformas móviles
            MovingPlatform(root, new Vector2(26f, 1.5f), new Vector2(0f, 3f), 4f, tile, 0.3f);
            MovingPlatform(root, new Vector2(51f, 5f), new Vector2(6f, 0f), 4f, tile, 0.35f);
            MovingPlatform(root, new Vector2(96f, 4f), new Vector2(0f, -2.5f), 4f, tile, 0.4f);

            float[,] coins =
            {
                { 6f, 1f }, { 10f, 1f }, { 14f, 4.7f }, { 18f, 1f },
                { 34f, 3.2f }, { 38f, 7.2f }, { 42f, 3.2f },
                { 58f, 5.2f }, { 62f, 9.7f }, { 66f, 5.2f },
                { 80f, 2.2f }, { 86f, 6.2f }, { 90f, 2.2f },
                { 104f, 4.2f }, { 108f, 8.7f }, { 112f, 4.2f },
                { 128f, 1f }, { 132f, 5.7f }, { 136f, 1f }
            };
            for (int i = 0; i < coins.GetLength(0); i++) Spawn("CoinPickup", new Vector3(coins[i, 0], coins[i, 1], 0f), root);

            Spawn("CrystalPickup", new Vector3(26f, 6f, 0f), root);
            Spawn("CrystalPickup", new Vector3(51f, 8f, 0f), root);
            Spawn("CrystalPickup", new Vector3(96f, 6.5f, 0f), root);
            Spawn("HeartPickup", new Vector3(60f, 5.2f, 0f), root);
            Spawn("KeyPickup", new Vector3(108f, 9f, 0f), root);

            // enemigos voladores y terrestres
            Spawn("Bat", new Vector3(34f, 7f, 0f), root);
            Spawn("Bat", new Vector3(52f, 8f, 0f), root);
            Spawn("Bat", new Vector3(74f, 6f, 0f), root);
            Spawn("Bat", new Vector3(96f, 7f, 0f), root);
            Spawn("Bat", new Vector3(112f, 9f, 0f), root);
            Spawn("Guardian", new Vector3(86f, 1.1f, 0f), root);
            Spawn("Guardian", new Vector3(132f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(10f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(38f, 2.1f, 0f), root);
            Spawn("Slime", new Vector3(62f, 4.1f, 0f), root);
            Spawn("Slime", new Vector3(104f, 3.1f, 0f), root);

            Spawn("Spike", new Vector3(16f, 0f, 0f), root);
            Spawn("Spike", new Vector3(66f, 4f, 0f), root);
            Spawn("Spike", new Vector3(116f, 3f, 0f), root);

            Spawn("Checkpoint", new Vector3(57f, 4f, 0f), root);

            GameObject exit = BuildExitDoor(root, new Vector3(138f, 0f, 0f), 2, "Puerta de la Montana");

            GameObject pit = BuilderUtil.CreateTriggerBox("PitKill", root, new Vector2(66f, -14f), new Vector2(240f, 4f), BuilderUtil.LayerHazard);
            Hazard pitHazard = pit.AddComponent<Hazard>();
            pitHazard.damage = 99;

            GameObject player = Spawn("Player", new Vector3(-6f, 0.2f, 0f), null);
            CreateCamera(new Vector2(-12f, -6f), new Vector2(144f, 22f), new Color(0.1f, 0.12f, 0.22f, 1f), player.transform);
            CreateGameHUD("Consigue la segunda llave y alcanza la puerta de la montaña");

            SaveActiveScene();
            AddSceneToBuild(GameManager.SceneLevel2);
            Debug.Log("[Builder] Nivel 2 creado.");
        }

        // ------------------------------------------------------------ nivel 3

        public static void BuildLevel3()
        {
            CreateEmptyScene(GameManager.SceneLevel3);
            Sprite tile = S("tile_castle");

            CreateBackdrop("bg_castle", 15f, 4f);

            GameObject level = new GameObject("Level");
            Transform root = level.transform;

            Ground(root, -8f, 34f, 0f, tile);
            Ground(root, 38f, 104f, 0f, tile);

            Platform(root, 14f, 3.5f, 4f, tile);
            Platform(root, 22f, 5.5f, 4f, tile);
            Platform(root, 46f, 3f, 5f, tile);
            Platform(root, 54f, 5f, 4f, tile);

            // muros del castillo
            BuilderUtil.CreateSolidBox("WallLeft", root, new Vector2(37f, 4f), new Vector2(0.8f, 8f),
                new Color(0.35f, 0.28f, 0.5f, 1f), 3, BuilderUtil.LayerGround, true);
            BuilderUtil.CreateSolidBox("WallRight", root, new Vector2(103.5f, 4f), new Vector2(1f, 8f),
                new Color(0.35f, 0.28f, 0.5f, 1f), 3, BuilderUtil.LayerGround, true);

            float[,] coins =
            {
                { 2f, 1f }, { 6f, 1f }, { 14f, 4.7f }, { 18f, 1f }, { 22f, 6.7f },
                { 26f, 1f }, { 42f, 1f }, { 46f, 4.2f }, { 50f, 1f }, { 54f, 6.2f },
                { 58f, 1f }, { 76f, 1f }, { 82f, 1f }, { 88f, 1f }, { 94f, 1f }, { 100f, 1f }
            };
            for (int i = 0; i < coins.GetLength(0); i++) Spawn("CoinPickup", new Vector3(coins[i, 0], coins[i, 1], 0f), root);

            Spawn("CrystalPickup", new Vector3(22f, 7f, 0f), root);
            Spawn("CrystalPickup", new Vector3(54f, 6.5f, 0f), root);
            Spawn("HeartPickup", new Vector3(48f, 1f, 0f), root);
            Spawn("HeartPickup", new Vector3(74f, 1f, 0f), root);
            Spawn("KeyPickup", new Vector3(22f, 6.8f, 0f), root);

            Spawn("Slime", new Vector3(8f, 0.1f, 0f), root);
            Spawn("Slime", new Vector3(30f, 0.1f, 0f), root);
            Spawn("Bat", new Vector3(30f, 6f, 0f), root);
            Spawn("Bat", new Vector3(60f, 6f, 0f), root);
            Spawn("Guardian", new Vector3(18f, 0.1f, 0f), root);
            Spawn("Guardian", new Vector3(60f, 0.1f, 0f), root);

            Spawn("Spike", new Vector3(12f, 0f, 0f), root);
            Spawn("Spike", new Vector3(28f, 0f, 0f), root);
            Spawn("Spike", new Vector3(44f, 0f, 0f), root);
            Spawn("Spike", new Vector3(58f, 0f, 0f), root);

            Spawn("Checkpoint", new Vector3(4f, 0f, 0f), root);

            // puerta sellada con 3 llaves
            GameObject doorTrigger = BuilderUtil.CreateTriggerBox("LockedDoor", root, new Vector2(66f, 1.6f), new Vector2(2.4f, 3.2f), 0);
            GameObject barrier = BuilderUtil.CreateSolidBox("DoorBarrier", root, new Vector2(66f, 1.6f), new Vector2(1.2f, 3.2f),
                new Color(0.5f, 0.35f, 0.75f, 1f), 12, BuilderUtil.LayerGround, true);
            BuilderUtil.CreateSolidBox("DoorFrameTop", root, new Vector2(66f, 3.6f), new Vector2(3.2f, 0.6f),
                new Color(0.25f, 0.2f, 0.35f, 1f), 13, 0, false);
            LockedDoor lockedDoor = doorTrigger.AddComponent<LockedDoor>();
            lockedDoor.keysRequired = 3;
            lockedDoor.barrier = barrier;
            lockedDoor.glow = barrier.GetComponent<SpriteRenderer>();

            // arena del jefe + princesa
            GameObject princess = Spawn("PrincessElena", new Vector3(98f, 0f, 0f), root);

            GameObject boss = Spawn("Maldrak", new Vector3(84f, 0.2f, 0f), root);
            BossMaldrak bm = boss.GetComponent<BossMaldrak>();
            bm.arenaMinX = 72f;
            bm.arenaMaxX = 100f;
            bm.princess = princess.GetComponent<PrincessElena>();

            GameObject pit = BuilderUtil.CreateTriggerBox("PitKill", root, new Vector2(48f, -14f), new Vector2(220f, 4f), BuilderUtil.LayerHazard);
            Hazard pitHazard = pit.AddComponent<Hazard>();
            pitHazard.damage = 99;

            GameObject player = Spawn("Player", new Vector3(-4f, 0.2f, 0f), null);
            CreateCamera(new Vector2(-10f, -6f), new Vector2(106f, 18f), new Color(0.06f, 0.03f, 0.1f, 1f), player.transform);
            CreateGameHUD("Consigue las 3 llaves, derrota a Maldrak y libera a Elena");

            SaveActiveScene();
            AddSceneToBuild(GameManager.SceneLevel3);
            Debug.Log("[Builder] Nivel 3 creado.");
        }

        // ------------------------------------------------------------ helpers de nivel

        static void Ground(Transform root, float x0, float x1, float topY, Sprite tile)
        {
            BuilderUtil.CreateTiledBox("Ground", root, new Vector2((x0 + x1) * 0.5f, topY - 1f),
                new Vector2(x1 - x0, 2f), tile, 0, BuilderUtil.LayerGround);
        }

        static void Platform(Transform root, float cx, float cy, float width, Sprite tile, float height = 0.7f)
        {
            BuilderUtil.CreateTiledBox("Platform", root, new Vector2(cx, cy), new Vector2(width, height), tile, 0, BuilderUtil.LayerGround);
        }

        static void MovingPlatform(Transform root, Vector2 center, Vector2 offset, float width, Sprite tile, float speed)
        {
            GameObject go = BuilderUtil.CreateTiledBox("MovingPlatform", root, center, new Vector2(width, 0.7f),
                tile, 0, BuilderUtil.LayerGround, false);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.7f);

            MovingPlatform mp = go.AddComponent<MovingPlatform>();
            mp.offset = offset;
            mp.speed = speed;
        }

        static GameObject BuildExitDoor(Transform root, Vector3 position, int keysRequired, string doorName)
        {
            GameObject exit = new GameObject(doorName);
            exit.transform.SetParent(root, false);
            // NOTA: los hijos se crean con coordenadas de mundo; el objeto debe seguir
            // en el origen hasta que estén creados, o quedarían desplazados.

            BuilderUtil.CreateSolidBox("DoorFrame", exit.transform, new Vector2(0f, 1.55f),
                new Vector2(2.4f, 3.3f), new Color(0.22f, 0.24f, 0.35f, 1f), 10, 0, false);
            GameObject glow = BuilderUtil.CreateSolidBox("DoorGlow", exit.transform, new Vector2(0f, 1.4f),
                new Vector2(1.9f, 2.9f), new Color(0.45f, 0.85f, 1f, 0.85f), 11, 0, false);

            exit.transform.position = position;

            BoxCollider2D col = exit.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3.2f);
            col.offset = new Vector2(0f, 1.6f);

            LevelExit levelExit = exit.AddComponent<LevelExit>();
            levelExit.keysRequired = keysRequired;
            levelExit.glow = glow.GetComponent<SpriteRenderer>();
            return exit;
        }

        static void AddSceneToBuild(string sceneName)
        {
            string path = BuilderUtil.Scenes + "/" + sceneName + ".unity";
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].path == path) return;
            }
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ============================================================ MENUS

        public static void BuildMainMenu()
        {
            CreateEmptyScene(GameManager.SceneMenu);

            CreateBackdrop("bg_forest", 15f, 3f);
            BuilderUtil.CreateSolidBox("Dim", null, new Vector2(0f, 0f), new Vector2(40f, 24f),
                new Color(0.02f, 0.04f, 0.09f, 0.45f), 50, 0, false);

            BuilderUtil.CreateSpriteObject("NicoMenu", S("nico"), null, new Vector3(-7.4f, -6f, 0f), 4.2f, 20);
            BuilderUtil.CreateSpriteObject("ElenaMenu", S("elena"), null, new Vector3(7.4f, -6f, 0f), 4.2f, 20);

            CreateCamera(new Vector2(-11f, -6f), new Vector2(11f, 6f), new Color(0.05f, 0.09f, 0.14f, 1f), null);

            Canvas canvas = BuilderUtil.CreateCanvas("MenuCanvas");
            Transform root = canvas.transform;

            BuilderUtil.CreateText(root, "Title", "EL RESCATE DE LA PRINCESA", 86, new Color(1f, 0.92f, 0.5f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f),
                new Vector2(1600f, 110f), true);
            BuilderUtil.CreateText(root, "Subtitle", "Una aventura de Nico contra el hechicero Maldrak", 34,
                Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -170f), new Vector2(1400f, 50f));

            MainMenuUI menu = canvas.gameObject.AddComponent<MainMenuUI>();

            RectTransform mainPanel = BuilderUtil.CreatePanel(root, "MainPanel", new Color(0f, 0f, 0f, 0f));

            Button btnPlay = BuilderUtil.CreateButton(mainPanel, "BtnPlay", "JUGAR", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(520f, 96f),
                new Color(0.95f, 0.7f, 0.15f, 0.95f), new Color(0.15f, 0.08f, 0f));
            Button btnContinue = BuilderUtil.CreateButton(mainPanel, "BtnContinue", "CONTINUAR", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(520f, 96f),
                new Color(0.2f, 0.5f, 0.85f, 0.95f), Color.white);
            Button btnOptions = BuilderUtil.CreateButton(mainPanel, "BtnOptions", "OPCIONES", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(520f, 96f),
                new Color(0.3f, 0.35f, 0.5f, 0.95f), Color.white);
            Button btnQuit = BuilderUtil.CreateButton(mainPanel, "BtnQuit", "SALIR", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -310f), new Vector2(520f, 96f),
                new Color(0.55f, 0.18f, 0.22f, 0.95f), Color.white);

            BuilderUtil.CreateText(mainPanel, "Hint", "A/D o flechas: mover · ESPACIO: saltar · SHIFT: correr · J: atacar · F: magia · ESC: pausa",
                26, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(1800f, 40f));

            RectTransform optionsPanel = BuilderUtil.CreatePanel(root, "OptionsPanel", new Color(0.02f, 0.03f, 0.08f, 0.92f));
            BuilderUtil.CreateText(optionsPanel, "Title", "OPCIONES", 72, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(900f, 100f), true);

            BuilderUtil.CreateText(optionsPanel, "MusicLabel", "MÚSICA", 40, Color.white, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 100f), new Vector2(300f, 50f), true);
            Slider musicSlider = BuilderUtil.CreateSlider(optionsPanel, "MusicSlider", new Vector2(140f, 100f),
                new Vector2(560f, 40f), new Color(0f, 0f, 0f, 0.6f), GameData.MusicVolume);

            BuilderUtil.CreateText(optionsPanel, "SfxLabel", "EFECTOS", 40, Color.white, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 10f), new Vector2(300f, 50f), true);
            Slider sfxSlider = BuilderUtil.CreateSlider(optionsPanel, "SfxSlider", new Vector2(140f, 10f),
                new Vector2(560f, 40f), new Color(0f, 0f, 0f, 0.6f), GameData.SfxVolume);

            Button btnTestSfx = BuilderUtil.CreateButton(optionsPanel, "BtnTestSfx", "PROBAR SONIDO", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(460f, 80f),
                new Color(0.2f, 0.45f, 0.55f, 0.95f), Color.white, 30);
            Button btnCloseOptions = BuilderUtil.CreateButton(optionsPanel, "BtnCloseOptions", "VOLVER", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(460f, 90f),
                new Color(0.75f, 0.5f, 0.15f, 0.95f), new Color(0.12f, 0.07f, 0f));

            BuilderUtil.OnClick(btnPlay, menu.NewGame);
            BuilderUtil.OnClick(btnContinue, menu.Continue);
            BuilderUtil.OnClick(btnOptions, menu.OpenOptions);
            BuilderUtil.OnClick(btnQuit, menu.Quit);
            BuilderUtil.OnClick(btnTestSfx, menu.TestSfx);
            BuilderUtil.OnClick(btnCloseOptions, menu.CloseOptions);
            BuilderUtil.OnSliderChanged(musicSlider, menu.SetMusicVolume);
            BuilderUtil.OnSliderChanged(sfxSlider, menu.SetSfxVolume);

            menu.mainPanel = mainPanel.gameObject;
            menu.optionsPanel = optionsPanel.gameObject;
            menu.musicSlider = musicSlider;
            menu.sfxSlider = sfxSlider;
            menu.continueButton = btnContinue;
            menu.continueLabel = btnContinue.GetComponentInChildren<Text>();

            optionsPanel.gameObject.SetActive(false);

            CreateEventSystem();
            SaveActiveScene();
            AddSceneToBuild(GameManager.SceneMenu);
            Debug.Log("[Builder] Menu principal creado.");
        }

        public static void BuildVictoryScene()
        {
            CreateEmptyScene(GameManager.SceneVictory);

            CreateBackdrop("bg_castle", 15f, 3f);
            BuilderUtil.CreateSolidBox("Dim", null, new Vector2(0f, 0f), new Vector2(40f, 24f),
                new Color(0.02f, 0.02f, 0.07f, 0.35f), 50, 0, false);

            BuilderUtil.CreateSpriteObject("NicoVictory", S("nico"), null, new Vector3(-3.2f, -5.6f, 0f), 3.6f, 20);
            BuilderUtil.CreateSpriteObject("ElenaVictory", S("elena"), null, new Vector3(3.2f, -5.6f, 0f), 3.6f, 20);

            CreateCamera(new Vector2(-11f, -6f), new Vector2(11f, 6f), new Color(0.04f, 0.03f, 0.1f, 1f), null);

            Canvas canvas = BuilderUtil.CreateCanvas("VictoryCanvas");
            Transform root = canvas.transform;
            VictoryUI ui = canvas.gameObject.AddComponent<VictoryUI>();

            RectTransform victoryPanel = BuilderUtil.CreatePanel(root, "VictoryPanel", new Color(0f, 0f, 0f, 0f));
            BuilderUtil.CreateText(victoryPanel, "Title", "¡ELENA ESTÁ A SALVO!", 96, new Color(1f, 0.92f, 0.5f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f),
                new Vector2(1600f, 130f), true);
            Text summary = BuilderUtil.CreateText(victoryPanel, "Summary", "", 40, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1200f, 300f));

            Button btnCredits = BuilderUtil.CreateButton(victoryPanel, "BtnCredits", "VER CRÉDITOS", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-300f, -180f), new Vector2(520f, 96f),
                new Color(0.95f, 0.7f, 0.15f, 0.95f), new Color(0.12f, 0.07f, 0f));
            Button btnMenu = BuilderUtil.CreateButton(victoryPanel, "BtnMenu", "MENÚ PRINCIPAL", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(300f, -180f), new Vector2(520f, 96f),
                new Color(0.2f, 0.5f, 0.85f, 0.95f), Color.white);

            RectTransform creditsPanel = BuilderUtil.CreatePanel(root, "CreditsPanel", new Color(0.02f, 0.02f, 0.06f, 0.95f));
            BuilderUtil.CreateText(creditsPanel, "CreditsTitle", "CRÉDITOS", 84, new Color(1f, 0.92f, 0.5f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f),
                new Vector2(1200f, 110f), true);
            BuilderUtil.CreateText(creditsPanel, "CreditsBody",
                "EL RESCATE DE LA PRINCESA\n\n" +
                "Dirección y diseño: equipo del proyecto\n" +
                "Programación: Unity C# (MCP)\n" +
                "Arte: sprites originales generados para este proyecto\n" +
                "Música y efectos: síntesis procedural original\n" +
                "Motor: Unity · URP 2D\n\n" +
                "Gracias por jugar.",
                34, Color.white, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(1300f, 560f));
            Button btnBack = BuilderUtil.CreateButton(creditsPanel, "BtnBack", "VOLVER", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(460f, 90f),
                new Color(0.3f, 0.35f, 0.5f, 0.95f), Color.white);

            BuilderUtil.OnClick(btnCredits, ui.ShowCredits);
            BuilderUtil.OnClick(btnMenu, ui.GoToMenu);
            BuilderUtil.OnClick(btnBack, ui.ShowVictory);

            ui.victoryPanel = victoryPanel.gameObject;
            ui.creditsPanel = creditsPanel.gameObject;
            ui.summaryText = summary;

            creditsPanel.gameObject.SetActive(false);

            CreateEventSystem();
            SaveActiveScene();
            AddSceneToBuild(GameManager.SceneVictory);
            Debug.Log("[Builder] Escena de victoria creada.");
        }

        public static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BuilderUtil.Scenes + "/" + GameManager.SceneMenu + ".unity", true),
                new EditorBuildSettingsScene(BuilderUtil.Scenes + "/" + GameManager.SceneLevel1 + ".unity", true),
                new EditorBuildSettingsScene(BuilderUtil.Scenes + "/" + GameManager.SceneLevel2 + ".unity", true),
                new EditorBuildSettingsScene(BuilderUtil.Scenes + "/" + GameManager.SceneLevel3 + ".unity", true),
                new EditorBuildSettingsScene(BuilderUtil.Scenes + "/" + GameManager.SceneVictory + ".unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[Builder] Build settings configurados (5 escenas).");
        }
    }
}
