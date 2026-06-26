// SceneBuilder.cs - Editor工具，一键生成完整主场景
// 菜单: FishGame > Build Main Scene
// 自动创建所有GameObject、挂载脚本、连线引用

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace FishGame.Editor
{
    public static class SceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/MainScene.unity";
        private const float CAMERA_SIZE = 7f;
        private const float SCREEN_W = 1920f;
        private const float SCREEN_H = 1080f;

        // 缓存关键对象引用
        private static GameObject goGameManager, goScoreManager, goTimerManager, goUIManager;
        private static GameObject goFisherman, goHook, goHookPivot, goItemSpawner;
        private static GameObject srSkyGo, srGameGo, srUnderwaterGo, srWaterGo, srSeabedGo;
        private static Canvas mainCanvas;

        [MenuItem("FishGame/Build Main Scene")]
        public static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateSortingLayers();
            CreateCamera();
            CreateEventSystem();
            CreateManagers();
            CreateGameWorld();
            CreateCanvasUI();
            ApplyChineseFont();
            CreateBaseItemPrefab();
            WireUpAllReferences();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            Debug.Log($"[SceneBuilder] 主场景构建完成: {SCENE_PATH}");
        }

        // ==================== Sorting Layers ====================
        private static void CreateSortingLayers()
        {
            // 确保 Background / Midground / Foreground 等 Sorting Layer 存在
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("m_SortingLayers");
            EnsureSortingLayer(layers, "Background");
            EnsureSortingLayer(layers, "Midground");
            EnsureSortingLayer(layers, "Foreground");
            tagManager.ApplyModifiedProperties();
        }

        private static void EnsureSortingLayer(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return;
            }
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var newLayer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            newLayer.FindPropertyRelative("name").stringValue = name;
            newLayer.FindPropertyRelative("uniqueID").intValue = name.GetHashCode() & 0x7FFFFFFF;
        }

        // ==================== 相机 ====================
        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CAMERA_SIZE;
            cam.backgroundColor = new Color(0.05f, 0.1f, 0.25f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.transform.position = new Vector3(0, 0, -10);
            go.AddComponent<AudioListener>();
        }

        // ==================== EventSystem ====================
        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        // ==================== 管理器 ====================
        private static void CreateManagers()
        {
            goGameManager = new GameObject("GameManager"); goGameManager.AddComponent<GameManager>();
            goScoreManager = new GameObject("ScoreManager"); goScoreManager.AddComponent<ScoreManager>();
            goTimerManager = new GameObject("TimerManager"); goTimerManager.AddComponent<TimerManager>();
            goUIManager = new GameObject("UIManager"); goUIManager.AddComponent<UIManager>();
        }

        // ==================== 游戏世界 ====================
        private static void CreateGameWorld()
        {
            var world = new GameObject("GameWorld");

            // ----- 背景 -----
            var bgRoot = new GameObject("Backgrounds"); bgRoot.transform.SetParent(world.transform);

            srSkyGo = NewBg(bgRoot, "SkyBg", "Background", 3.5f);
            srGameGo = NewBg(bgRoot, "GameBg", "Default", 0);
            srUnderwaterGo = NewBg(bgRoot, "UnderwaterBg", "Default", -1);
            srWaterGo = NewBg(bgRoot, "WaterSurface", "Midground", -1.5f);
            srSeabedGo = NewBg(bgRoot, "Seabed", "Default", -5f);

            // ------ 自动设置背景 Sprite ------
            srSkyGo.GetComponent<SpriteRenderer>().sprite        = LoadSpriteResource(ArtResourcePath.SkyBg);
            srGameGo.GetComponent<SpriteRenderer>().sprite       = LoadSpriteResource(ArtResourcePath.GameBg);
            srUnderwaterGo.GetComponent<SpriteRenderer>().sprite  = LoadSpriteResource(ArtResourcePath.UnderwaterBg);
            srWaterGo.GetComponent<SpriteRenderer>().sprite      = LoadSpriteResource(ArtResourcePath.WaterSurface);
            srSeabedGo.GetComponent<SpriteRenderer>().sprite     = LoadSpriteResource(ArtResourcePath.Seabed);

            // ----- 渔夫 + 钩爪 -----
            goFisherman = new GameObject("Fisherman");
            goFisherman.transform.SetParent(world.transform);
            goFisherman.transform.position = new Vector3(0, CAMERA_SIZE - 1.5f, 0);
            goFisherman.AddComponent<FishermanController>();

            var boatGo = new GameObject("Boat"); boatGo.transform.SetParent(goFisherman.transform);
            boatGo.transform.localPosition = Vector3.zero;
            var boatSr = boatGo.AddComponent<SpriteRenderer>(); boatSr.sortingLayerName = "Foreground"; boatSr.sortingOrder = 2;
            boatSr.sprite = LoadSpriteResource(ArtResourcePath.Boat);

            var fisherSrGo = new GameObject("FishermanSprite"); fisherSrGo.transform.SetParent(goFisherman.transform);
            fisherSrGo.transform.localPosition = new Vector3(0.3f, 0.5f, 0);
            var fisherSr = fisherSrGo.AddComponent<SpriteRenderer>(); fisherSr.sortingLayerName = "Foreground"; fisherSr.sortingOrder = 3;
            fisherSr.sprite = LoadSpriteResource(ArtResourcePath.FishermanIdle);

            goHookPivot = new GameObject("HookPivot"); goHookPivot.transform.SetParent(goFisherman.transform);
            goHookPivot.transform.localPosition = new Vector3(0.2f, -0.8f, 0);

            goHook = new GameObject("Hook"); goHook.transform.SetParent(goHookPivot.transform);
            goHook.transform.localPosition = Vector3.zero;
            goHook.AddComponent<HookController>();
            // Rigidbody2D(kinematic) 让子物体 HookHead 的触发器回调传递到 HookController
            var hookRb = goHook.AddComponent<Rigidbody2D>();
            hookRb.bodyType = RigidbodyType2D.Kinematic;
            hookRb.gravityScale = 0f;

            var chainGo = new GameObject("Chain"); chainGo.transform.SetParent(goHook.transform);
            var chainSr = chainGo.AddComponent<SpriteRenderer>();
            chainSr.sortingLayerName = "Foreground"; chainSr.sortingOrder = 4;
            chainSr.drawMode = SpriteDrawMode.Tiled; chainSr.size = new Vector2(0.1f, 1f);
            chainSr.sprite = LoadSpriteResource(ArtResourcePath.Chain);

            var hookHeadGo = new GameObject("HookHead"); hookHeadGo.transform.SetParent(goHook.transform);
            hookHeadGo.transform.localPosition = new Vector3(0, -1f, 0);
            var hookHeadSr = hookHeadGo.AddComponent<SpriteRenderer>(); hookHeadSr.sortingLayerName = "Foreground"; hookHeadSr.sortingOrder = 5;
            hookHeadSr.sprite = LoadSpriteResource(ArtResourcePath.Hook);
            var hookCol = hookHeadGo.AddComponent<BoxCollider2D>(); hookCol.isTrigger = true; hookCol.size = new Vector2(0.5f, 0.5f);

            // 设置 HookController 字段
            {
                var so = new SerializedObject(goHook.GetComponent<HookController>());
                so.FindProperty("pivot").objectReferenceValue = goHookPivot.transform;
                so.FindProperty("hookHead").objectReferenceValue = hookHeadGo.transform;
                so.FindProperty("hookHeadRenderer").objectReferenceValue = hookHeadSr;
                so.FindProperty("chainRenderer").objectReferenceValue = chainSr;
                so.ApplyModifiedProperties();
            }

            // 设置 FishermanController 字段
            {
                var so = new SerializedObject(goFisherman.GetComponent<FishermanController>());
                so.FindProperty("hook").objectReferenceValue = goHook.GetComponent<HookController>();
                so.FindProperty("fishermanRenderer").objectReferenceValue = fisherSrGo.GetComponent<SpriteRenderer>();
                so.FindProperty("boatRenderer").objectReferenceValue = boatGo.GetComponent<SpriteRenderer>();
                so.ApplyModifiedProperties();
            }

            // ----- 物品生成器 -----
            goItemSpawner = new GameObject("ItemSpawner");
            goItemSpawner.transform.SetParent(world.transform);
            goItemSpawner.AddComponent<ItemSpawner>();
        }

        private static GameObject NewBg(GameObject parent, string name, string sortLayer, float y)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform);
            go.transform.localPosition = new Vector3(0, y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = sortLayer; sr.sortingOrder = 0;
            return go;
        }

        // ==================== Canvas / UI ====================
        private static void CreateCanvasUI()
        {
            var canvasGo = new GameObject("Canvas"); canvasGo.transform.SetParent(goUIManager.transform);
            mainCanvas = canvasGo.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(SCREEN_W, SCREEN_H);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // === 主菜单 ===
            var menuPanel = UIPanel(canvasGo.transform, "MainMenuPanel");
            var menuBg = menuPanel.AddComponent<Image>(); menuBg.color = Color.white;
            menuBg.sprite = LoadSpriteResource(ArtResourcePath.MenuBg);

            var titleGo = UIText(menuPanel.transform, "Title", "Gold Fisher", 64,
                new Vector2(0, 250), new Vector2(600, 80), new Color(1f, 0.84f, 0f));
            var logoGo = UIImg(menuPanel.transform, "Logo", new Vector2(0, 100), new Vector2(300, 200));
            logoGo.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.GameLogo);

            var btnStart = UIButton(menuPanel.transform, "BtnStartGame", "开始游戏", new Vector2(0, -20));
            var btnLevel = UIButton(menuPanel.transform, "BtnLevelSelect", "关卡选择", new Vector2(0, -100));
            var btnSettings = UIButton(menuPanel.transform, "BtnSettings", "设置", new Vector2(0, -180));
            var btnExit = UIButton(menuPanel.transform, "BtnExit", "退出游戏", new Vector2(0, -260));

            var mainMenu = menuPanel.AddComponent<MainMenuPanel>();
            var levelSelectPnl = UIPanel(menuPanel.transform, "LevelSelectPanel"); levelSelectPnl.SetActive(false);
            var settingsPnl = UIPanel(menuPanel.transform, "SettingsPanel"); settingsPnl.SetActive(false);

            {
                var so = new SerializedObject(mainMenu);
                so.FindProperty("menuBackground").objectReferenceValue = menuBg;
                so.FindProperty("logoImage").objectReferenceValue = logoGo.GetComponent<Image>();
                so.FindProperty("startGameButton").objectReferenceValue = btnStart.GetComponent<Button>();
                so.FindProperty("levelSelectButton").objectReferenceValue = btnLevel.GetComponent<Button>();
                so.FindProperty("settingsButton").objectReferenceValue = btnSettings.GetComponent<Button>();
                so.FindProperty("exitGameButton").objectReferenceValue = btnExit.GetComponent<Button>();
                so.FindProperty("titleText").objectReferenceValue = titleGo.GetComponent<TextMeshProUGUI>();
                so.FindProperty("levelSelectPanel").objectReferenceValue = levelSelectPnl;
                so.FindProperty("settingsPanel").objectReferenceValue = settingsPnl;
                so.ApplyModifiedProperties();
            }

            // === HUD ===
            var hudPanel = UIPanel(canvasGo.transform, "HUD"); hudPanel.SetActive(false);
            // HUD 背景条：锚定到顶部，全宽，高80px
            var hudBg = UITopBar(hudPanel.transform, "HudBackground", 0, 1920, 80);
            hudBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            hudBg.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.HudBg);

            // HUD 元素全部使用顶部锚点（anchorY=1），y=-40 表示距顶40像素（bar中心）
            var coinIcon    = UITopItem(hudPanel.transform, "CoinIcon",    -860, 40, 40);
            coinIcon.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.IconCoin);
            var moneyText   = UITopText(hudPanel.transform, "MoneyText",   "$ 0",    28, -760, 160, 50, Color.white, TextAlignmentOptions.Left);
            var clockIcon   = UITopItem(hudPanel.transform, "ClockIcon",    -80, 40, 40);
            clockIcon.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.IconClock);
            var timerText   = UITopText(hudPanel.transform, "TimerText",   "00:00",  28,    0, 140, 50);
            var targetIcon  = UITopItem(hudPanel.transform, "TargetIcon",   120, 40, 40);
            targetIcon.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.IconTarget);
            var targetText  = UITopText(hudPanel.transform, "TargetText",  "目标: $0", 28, 220, 200, 50, Color.white, TextAlignmentOptions.Left);
            var fishIcon    = UITopItem(hudPanel.transform, "FishIcon",     400, 40, 40);
            fishIcon.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.IconFish);
            var starIcon    = UITopItem(hudPanel.transform, "StarIcon",     580, 40, 40);
            starIcon.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.IconStar);
            var levelText   = UITopText(hudPanel.transform, "LevelText",   "第 1 关", 24,  730, 140, 40);
            var remainingText = UITopText(hudPanel.transform, "RemainingText", "",   20,  870, 120, 40);
            var bombBtn     = UIButton(hudPanel.transform, "BombButton", "💣", Vector2.zero, new Vector2(60, 60));
            SetTopAnchor(bombBtn, 900);
            bombBtn.GetComponent<Image>().sprite = LoadSpriteResource(ArtResourcePath.IconBomb);

            var hudCtrl = hudPanel.AddComponent<HUDController>();
            {
                var so = new SerializedObject(hudCtrl);
                so.FindProperty("moneyText").objectReferenceValue = moneyText.GetComponent<TextMeshProUGUI>();
                so.FindProperty("timerText").objectReferenceValue = timerText.GetComponent<TextMeshProUGUI>();
                so.FindProperty("targetText").objectReferenceValue = targetText.GetComponent<TextMeshProUGUI>();
                so.FindProperty("levelText").objectReferenceValue = levelText.GetComponent<TextMeshProUGUI>();
                // "remainingText" field - check if it exists
                var remProp = so.FindProperty("remainingText");
                if (remProp != null) remProp.objectReferenceValue = remainingText.GetComponent<TextMeshProUGUI>();
                so.FindProperty("hudBackground").objectReferenceValue = hudBg.GetComponent<Image>();
                so.FindProperty("coinIcon").objectReferenceValue = coinIcon.GetComponent<Image>();
                so.FindProperty("clockIcon").objectReferenceValue = clockIcon.GetComponent<Image>();
                so.FindProperty("targetIcon").objectReferenceValue = targetIcon.GetComponent<Image>();
                so.FindProperty("fishIcon").objectReferenceValue = fishIcon.GetComponent<Image>();
                so.FindProperty("starIcon").objectReferenceValue = starIcon.GetComponent<Image>();
                so.FindProperty("bombIcon").objectReferenceValue = bombBtn.GetComponent<Image>();
                so.ApplyModifiedProperties();
            }

            // === 结果面板 ===
            var resultPanel = UIPanel(canvasGo.transform, "ResultPanel"); resultPanel.SetActive(false);
            var resultBg = resultPanel.AddComponent<Image>(); resultBg.color = new Color(0, 0, 0, 0.85f);
            resultBg.sprite = LoadSpriteResource(ArtResourcePath.PanelBg);
            var rTitle = UIText(resultPanel.transform, "TitleText", "过关！", 48, new Vector2(0, 100), new Vector2(400, 60));
            var rLevel = UIText(resultPanel.transform, "LevelText", "", 28, new Vector2(0, 30), new Vector2(300, 40));
            var rScore = UIText(resultPanel.transform, "ScoreText", "", 28, new Vector2(0, -20), new Vector2(300, 40));
            var rTarget = UIText(resultPanel.transform, "TargetText", "", 28, new Vector2(0, -70), new Vector2(300, 40));
            var btnNext = UIButton(resultPanel.transform, "BtnNextLevel", "下一关", new Vector2(0, -150));
            var btnRetry = UIButton(resultPanel.transform, "BtnRetry", "重试", new Vector2(0, -150));
            var btnReturn1 = UIButton(resultPanel.transform, "BtnReturnMenu", "返回菜单", new Vector2(0, -220));

            var resultCtrl = resultPanel.AddComponent<ResultPanel>();
            {
                var so = new SerializedObject(resultCtrl);
                so.FindProperty("titleText").objectReferenceValue = rTitle.GetComponent<TextMeshProUGUI>();
                so.FindProperty("scoreText").objectReferenceValue = rScore.GetComponent<TextMeshProUGUI>();
                so.FindProperty("targetText").objectReferenceValue = rTarget.GetComponent<TextMeshProUGUI>();
                so.FindProperty("levelText").objectReferenceValue = rLevel.GetComponent<TextMeshProUGUI>();
                so.FindProperty("nextLevelButton").objectReferenceValue = btnNext.GetComponent<Button>();
                so.FindProperty("retryButton").objectReferenceValue = btnRetry.GetComponent<Button>();
                so.FindProperty("returnMenuButton").objectReferenceValue = btnReturn1.GetComponent<Button>();
                so.FindProperty("panelBackground").objectReferenceValue = resultBg;
                so.ApplyModifiedProperties();
            }

            // === 暂停面板 ===
            var pausePanel = UIPanel(canvasGo.transform, "PausePanel"); pausePanel.SetActive(false);
            var pauseBg = pausePanel.AddComponent<Image>(); pauseBg.color = new Color(0, 0, 0, 0.7f);
            pauseBg.sprite = LoadSpriteResource(ArtResourcePath.PanelBg);
            var pTitle = UIText(pausePanel.transform, "TitleText", "暂停", 48, new Vector2(0, 100), new Vector2(300, 60));
            var btnResume = UIButton(pausePanel.transform, "BtnResume", "继续", new Vector2(0, 10));
            var btnRestart = UIButton(pausePanel.transform, "BtnRestart", "重新开始", new Vector2(0, -60));
            var btnReturn2 = UIButton(pausePanel.transform, "BtnReturnMenu", "返回菜单", new Vector2(0, -130));

            var pauseCtrl = pausePanel.AddComponent<PausePanel>();
            {
                var so = new SerializedObject(pauseCtrl);
                so.FindProperty("titleText").objectReferenceValue = pTitle.GetComponent<TextMeshProUGUI>();
                so.FindProperty("resumeButton").objectReferenceValue = btnResume.GetComponent<Button>();
                so.FindProperty("restartButton").objectReferenceValue = btnRestart.GetComponent<Button>();
                so.FindProperty("returnMenuButton").objectReferenceValue = btnReturn2.GetComponent<Button>();
                so.FindProperty("panelBackground").objectReferenceValue = pauseBg;
                so.ApplyModifiedProperties();
            }

            // === 按钮精灵交换样式 ===
            var sprNormal = LoadSpriteResource(ArtResourcePath.BtnNormal);
            var sprHover = LoadSpriteResource(ArtResourcePath.BtnHover);
            var sprPressed = LoadSpriteResource(ArtResourcePath.BtnPressed);

            ApplyButtonSpriteSwap(btnStart, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnLevel, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnSettings, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnExit, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnNext, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnRetry, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnReturn1, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnResume, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnRestart, sprNormal, sprHover, sprPressed);
            ApplyButtonSpriteSwap(btnReturn2, sprNormal, sprHover, sprPressed);

            // === 回设 UIManager ===
            {
                var so = new SerializedObject(goUIManager.GetComponent<UIManager>());
                so.FindProperty("mainCanvas").objectReferenceValue = mainCanvas;
                so.FindProperty("hudController").objectReferenceValue = hudCtrl;
                so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
                so.FindProperty("resultPanel").objectReferenceValue = resultCtrl;
                so.FindProperty("pausePanel").objectReferenceValue = pauseCtrl;
                so.ApplyModifiedProperties();
            }
        }

        // ==================== BaseItem Prefab ====================
        private static void CreateBaseItemPrefab()
        {
            var go = new GameObject("BaseItemPrefab");
            var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 1;
            var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = new Vector2(0.6f, 0.6f);
            go.AddComponent<CatchableItem>();

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            string path = "Assets/Prefabs/BaseItemPrefab.prefab";
            AssetDatabase.DeleteAsset(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            {
                var so = new SerializedObject(goItemSpawner.GetComponent<ItemSpawner>());
                so.FindProperty("baseItemPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
            }
        }

        // ==================== 连线 GameManager ====================
        private static void WireUpAllReferences()
        {
            var so = new SerializedObject(goGameManager.GetComponent<GameManager>());

            so.FindProperty("fisherman").objectReferenceValue = goFisherman.GetComponent<FishermanController>();
            so.FindProperty("hook").objectReferenceValue = goHook.GetComponent<HookController>();
            so.FindProperty("spawner").objectReferenceValue = goItemSpawner.GetComponent<ItemSpawner>();
            so.FindProperty("scoreManager").objectReferenceValue = goScoreManager.GetComponent<ScoreManager>();
            so.FindProperty("timerManager").objectReferenceValue = goTimerManager.GetComponent<TimerManager>();

            so.FindProperty("gameBackground").objectReferenceValue = srGameGo.GetComponent<SpriteRenderer>();
            so.FindProperty("underwaterBackground").objectReferenceValue = srUnderwaterGo.GetComponent<SpriteRenderer>();
            so.FindProperty("skyBackground").objectReferenceValue = srSkyGo.GetComponent<SpriteRenderer>();
            so.FindProperty("waterSurface").objectReferenceValue = srWaterGo.GetComponent<SpriteRenderer>();
            so.FindProperty("seabed").objectReferenceValue = srSeabedGo.GetComponent<SpriteRenderer>();

            so.FindProperty("uiManagerMono").objectReferenceValue = goUIManager.GetComponent<UIManager>();

            // LevelConfig
            var lc = FindOrCreateLevelConfig();
            so.FindProperty("levelConfig").objectReferenceValue = lc;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(goGameManager);
        }

        private static LevelConfig FindOrCreateLevelConfig()
        {
            var guids = AssetDatabase.FindAssets("t:LevelConfig");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<LevelConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (!AssetDatabase.IsValidFolder("Assets/Config"))
                AssetDatabase.CreateFolder("Assets", "Config");

            var lc = ScriptableObject.CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(lc, "Assets/Config/LevelConfig.asset");
            Debug.Log("[SceneBuilder] 已创建默认 LevelConfig.asset（请在编辑器中添加关卡数据）");
            return lc;
        }

        // ==================== 应用字体 ====================
        private static void ApplyChineseFont()
        {
            var fontAsset = ChineseFontCreator.GetOrCreateChineseFontAsset();
            if (fontAsset == null)
            {
                Debug.LogWarning("[SceneBuilder] 未找到中文字体资产，按钮中文可能无法显示。"
                    + "请在 Unity 中执行 FishGame > Create Chinese Font Asset，然后重新 Build Main Scene。");
                return;
            }

            // 遍历 Canvas 下所有 TextMeshProUGUI，设置字体
            var allTMP = mainCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in allTMP)
            {
                tmp.font = fontAsset;
                tmp.fontSharedMaterial = fontAsset.material;
            }

            Debug.Log($"[SceneBuilder] 已为 {allTMP.Length} 个 TextMeshProUGUI 组件应用中文字体。");
        }

        // ==================== 资源加载辅助 ====================
        /// <summary>
        /// 从 Resources 文件夹加载 Sprite（Editor 模式下也可用）
        /// </summary>
        private static Sprite LoadSpriteResource(string resourcePath)
        {
            var s = Resources.Load<Sprite>(resourcePath);
            if (s == null)
                Debug.LogWarning($"[SceneBuilder] 未找到 Sprite 资源: {resourcePath}");
            return s;
        }

        // ==================== UI 辅助 ====================
        private static GameObject UIPanel(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject UIText(Transform parent, string name, string text, int fontSize,
            Vector2 pos, Vector2 size, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.alignment = align; tmp.color = color ?? Color.white;
            return go;
        }

        private static GameObject UIImg(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.AddComponent<Image>().color = Color.white;
            return go;
        }

        private static GameObject UIButton(Transform parent, string name, string label,
            Vector2 pos, Vector2? sizeOverride = null)
        {
            var size = sizeOverride ?? new Vector2(260, 60);
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            var img = go.AddComponent<Image>(); img.color = new Color(0.3f, 0.5f, 0.8f);
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var c = btn.colors;
            c.normalColor = new Color(0.3f, 0.5f, 0.8f);
            c.highlightedColor = new Color(0.4f, 0.65f, 1f);
            c.pressedColor = new Color(0.2f, 0.35f, 0.6f);
            btn.colors = c;

            var lbl = new GameObject("Label"); lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var ltmp = lbl.AddComponent<TextMeshProUGUI>();
            ltmp.text = label; ltmp.fontSize = 28;
            ltmp.alignment = TextAlignmentOptions.Center; ltmp.color = Color.white;

            return go;
        }

        /// <summary>
        /// 将按钮切换为 SpriteSwap 模式，并设置 normal/hover/pressed 精灵
        /// </summary>
        private static void ApplyButtonSpriteSwap(GameObject btnGo, Sprite normal, Sprite hover, Sprite pressed)
        {
            var btn = btnGo.GetComponent<Button>();
            if (btn == null) return;

            btn.transition = Selectable.Transition.SpriteSwap;

            var ss = btn.spriteState;
            if (hover != null)   ss.highlightedSprite = hover;
            if (pressed != null) ss.pressedSprite = pressed;
            btn.spriteState = ss;

            if (normal != null && btn.image != null)
            {
                btn.image.sprite = normal;
                btn.image.type = Image.Type.Sliced;
            }
        }

        // ── HUD 顶部锚点辅助方法 ──────────────────────────────────────────

        /// <summary>全宽顶部背景条</summary>
        private static GameObject UITopBar(Transform parent, string name, float xOffset, float width, float height)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xOffset, -height * 0.5f);
            rt.sizeDelta = new Vector2(0, height);
            go.AddComponent<Image>().color = Color.white;
            return go;
        }

        /// <summary>顶部图标（Image）</summary>
        private static GameObject UITopItem(Transform parent, string name, float x, float w, float h)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -40);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = Color.white;
            return go;
        }

        /// <summary>顶部文本（TextMeshProUGUI）</summary>
        private static GameObject UITopText(Transform parent, string name, string text, int fontSize,
            float x, float w, float h, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -40);
            rt.sizeDelta = new Vector2(w, h);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.alignment = align; tmp.color = color ?? Color.white;
            return go;
        }

        /// <summary>将已有 GameObject 的 RectTransform 改为顶部锚点</summary>
        private static void SetTopAnchor(GameObject go, float x)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -40);
        }
    }
}
