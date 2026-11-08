using System.Collections.Generic;
using System.IO;
using FiveNightAnomaly.Menus;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FiveNightAnomaly.EditorTools
{
    /// <summary>
    /// Builds Scenes/MainMenu.unity from the sprites ArtSource/tools/gen_main_menu.py makes from the team mockup: one
    /// canvas that scales the 1920x1080 design to cover the screen (Shrink: a wider or narrower screen crops a little
    /// off the edges, never shows a bar, and keeps every part where it is in the design). Back to front: the sky, five
    /// scrolling layers (far pines, near pines, ground band, lane, front hills), then the screens: Home (title, PLAY,
    /// SETTING, CREDIT), Setting (volume slider, BACK) and Credit (team from the RPP, BACK). The scrolling layers are
    /// RawImages whose UVs MenuParallax moves at a steady speed. The canvas is Screen Space Camera (its outline then lies on the screen
    /// edge instead of crossing the Game view) and the EventSystem icon is hidden, so the Game view shows the menu like
    /// the design even with gizmos on. Every button clicks with Audio/SFX/UI/Button/Button_Click (ButtonClickSound).
    /// PLAY loads NightLaneDemo; MainMenu and NightLaneDemo go first in the build scene
    /// list (the template SampleScene stays in the list, switched off). The scene that is open stays open: the menu is
    /// built in an extra scene, saved and closed again.
    /// </summary>
    public static class MainMenuBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/_Game/Scenes/NightLaneDemo.unity";
        private const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MenuSprites = "Assets/_Game/Sprites/UI";
        private const string BackgroundSprites = "Assets/_Game/Sprites/Backgrounds/Menu";
        private const string PreviewSound = "Assets/_Game/Audio/SFX/Characters/SlimeAmber/SlimeAmber_Land.wav";
        private const string ClickSound = "Assets/_Game/Audio/SFX/UI/Button/Button_Click.wav";
        private static readonly Vector2 Reference = new Vector2(1920f, 1080f);
        private static readonly Color ClearColour = new Color32(0x15, 0x0E, 0x3D, 0xFF);
        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        // Canvas pixels from the centre, y up (printed by gen_main_menu.py; the sprites are shown 1:1 at 1920x1080).
        private static readonly Vector2 LogoSize = new Vector2(730f, 429f);
        private static readonly Vector2 LogoCentre = new Vector2(4f, 227.5f);
        private static readonly (string name, Vector2 size, Vector2 centre)[] Buttons =
        {
            ("ButtonPlay", new Vector2(487f, 128f), new Vector2(-0.5f, -68f)),
            ("ButtonSetting", new Vector2(412f, 105f), new Vector2(0f, -193.5f)),
            ("ButtonCredit", new Vector2(411f, 103f), new Vector2(-0.5f, -307.5f)),
        };
        private static readonly Vector2 BackSize = new Vector2(412f, 105f);
        private static readonly Vector2 SettingPanelSize = new Vector2(700f, 472f);
        private static readonly Vector2 SliderCentre = new Vector2(0f, -26f);
        private static readonly Vector2 SettingBack = new Vector2(0f, -160f);
        private static readonly Vector2 CreditPanelSize = new Vector2(700f, 782f);
        private static readonly Vector2 CreditBack = new Vector2(0f, -315f);
        private const float TrackWidth = 560f, TrackHeight = 36f, TrackRim = 5f, KnobSize = 60f;

        // Scrolling layers back to front: sprite, height, centre y, speed (pixels moved per pixel the lane moves).
        private static readonly (string name, float height, float centreY, float speed)[] Layers =
        {
            ("MenuFarForest", 480f, -60f, 0.12f),
            ("MenuMidForest", 500f, -50f, 0.28f),
            ("MenuBand", 130f, -265f, 0.65f),
            ("MenuLane", 218f, -431f, 1f),
            ("MenuForeground", 250f, -415f, 1.5f),
        };

        [MenuItem("Tools/5 Night Anomaly/Rebuild Main Menu")]
        private static void RebuildMenu() => Debug.Log(Build());

        public static string Build()
        {
            PengembaraDemoBuilder.ApplyImportRules(MenuSprites);
            PengembaraDemoBuilder.ApplyImportRules(BackgroundSprites);
            SetSlicedBorder($"{MenuSprites}/SliderFill.png", new Vector4(13f, 0f, 13f, 0f));

            Scene active = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen)
            {
                scene = File.Exists(ScenePath)
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            foreach (GameObject root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            SceneManager.SetActiveScene(scene);
            try
            {
                Camera camera = BuildCamera();
                BuildEventSystem();
                BuildCanvas(camera);
            }
            finally
            {
                if (active.IsValid() && active != scene)
                    SceneManager.SetActiveScene(active);
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!wasOpen)
                EditorSceneManager.CloseScene(scene, true);
            SetBuildScenes();
            return $"Main menu built: {ScenePath}; PLAY loads {Path.GetFileNameWithoutExtension(GameScenePath)}; the build scene list starts with the menu";
        }

        private static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.21875f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ClearColour;
            return camera;
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            GizmoUtility.SetIconEnabled(typeof(EventSystem), false);
            GizmoUtility.SetIconEnabled(typeof(InputSystemUIInputModule), false);
        }

        private static void BuildCanvas(Camera camera)
        {
            var root = new GameObject("MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvasComponent = root.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceCamera;
            canvasComponent.worldCamera = camera;
            canvasComponent.planeDistance = 5f;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            scaler.referencePixelsPerUnit = 100f;
            Transform canvas = root.transform;

            Transform background = Group(canvas, "Background");
            Stretch(background);
            AddImage(background, "MenuSky", BackgroundSprite("MenuSky"), Reference, Vector2.zero, Centre);
            var layers = new List<(RawImage image, float speed)>();
            foreach ((string name, float height, float centreY, float speed) in Layers)
                layers.Add((AddStrip(background, name, height, centreY), speed));
            SetLayers(background.gameObject.AddComponent<MenuParallax>(), layers);

            var menu = root.AddComponent<MainMenu>();
            CanvasGroup home = AddScreen(canvas, "Home");
            AddImage(home.transform, "Logo", BackgroundSprite("MenuLogo"), LogoSize, LogoCentre, Centre);
            var buttons = new Button[Buttons.Length];
            for (int i = 0; i < Buttons.Length; i++)
            {
                (string name, Vector2 size, Vector2 position) = Buttons[i];
                buttons[i] = AddButton(home.transform, name, MenuSprite(name), size, position);
            }
            UnityEventTools.AddPersistentListener(buttons[0].onClick, menu.Play);
            UnityEventTools.AddPersistentListener(buttons[1].onClick, menu.OpenSetting);
            UnityEventTools.AddPersistentListener(buttons[2].onClick, menu.OpenCredit);
            for (int i = 0; i < buttons.Length; i++)
                Link(buttons[i], i > 0 ? buttons[i - 1] : null, i < buttons.Length - 1 ? buttons[i + 1] : null);

            CanvasGroup setting = AddScreen(canvas, "Setting");
            AddImage(setting.transform, "Panel", MenuSprite("PanelSetting"), SettingPanelSize, Vector2.zero, Centre);
            Slider slider = BuildVolumeSlider(setting.transform);
            Button settingBack = AddButton(setting.transform, "ButtonBack", MenuSprite("ButtonBack"), BackSize, SettingBack);
            UnityEventTools.AddPersistentListener(settingBack.onClick, menu.Back);
            Link(slider, null, settingBack);
            Link(settingBack, slider, null);

            CanvasGroup credit = AddScreen(canvas, "Credit");
            AddImage(credit.transform, "Panel", MenuSprite("PanelCredit"), CreditPanelSize, Vector2.zero, Centre);
            Button creditBack = AddButton(credit.transform, "ButtonBack", MenuSprite("ButtonBack"), BackSize, CreditBack);
            UnityEventTools.AddPersistentListener(creditBack.onClick, menu.Back);
            Link(creditBack, null, null);

            setting.gameObject.SetActive(false);
            credit.gameObject.SetActive(false);
            PengembaraDemoBuilder.SetFields(menu,
                ("playScene", Path.GetFileNameWithoutExtension(GameScenePath)),
                ("home", home), ("settingScreen", setting), ("creditScreen", credit),
                ("homeFirst", buttons[0].gameObject), ("settingFirst", slider.gameObject), ("creditFirst", creditBack.gameObject));
        }

        /// <summary>VolumeSlider > Track, Fill Area > Fill (9-sliced, from the left end to the handle), Handle Area > Handle.</summary>
        private static Slider BuildVolumeSlider(Transform parent)
        {
            var go = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider), typeof(AudioSource));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Place(rect, new Vector2(TrackWidth, KnobSize), SliderCentre, Centre);
            AddImage(rect, "Track", MenuSprite("SliderTrack"), new Vector2(TrackWidth, TrackHeight), Vector2.zero, Centre);
            float inset = TrackRim + (TrackHeight - 2f * TrackRim) / 2f;
            RectTransform fillArea = Stretch(Group(rect, "Fill Area"), inset, TrackHeight - 2f * TrackRim);
            Image fill = AddImage(fillArea, "Fill", MenuSprite("SliderFill"), Vector2.zero, Vector2.zero, Centre);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = new Vector2(-(inset - TrackRim), 0f);
            fill.rectTransform.offsetMax = Vector2.zero;
            RectTransform handleArea = Stretch(Group(rect, "Handle Area"), inset, KnobSize);
            Image handle = AddImage(handleArea, "Handle", MenuSprite("SliderHandle"), Vector2.zero, Vector2.zero, Centre);
            handle.rectTransform.anchorMin = Vector2.zero; // the slider sets the x anchors; y spans the handle area
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(KnobSize, 0f);
            handle.raycastTarget = true;
            rect.Find("Track").GetComponent<Image>().raycastTarget = true;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            Tint(slider);

            var source = go.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var volume = go.AddComponent<VolumeSlider>();
            PengembaraDemoBuilder.SetFields(volume, ("previewSource", source),
                ("previewSound", AssetDatabase.LoadAssetAtPath<AudioClip>(PreviewSound)));
            var feedback = go.AddComponent<MenuButtonFeedback>();
            PengembaraDemoBuilder.SetFields(feedback, ("hoverScale", 1.15f), ("pressScale", 1.05f), ("target", handle.transform));
            return slider;
        }

        private static Button AddButton(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position)
        {
            Image image = AddImage(parent, name, sprite, size, position, Centre);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Tint(button);
            image.gameObject.AddComponent<MenuButtonFeedback>();
            PengembaraDemoBuilder.SetFields(image.gameObject.AddComponent<ButtonClickSound>(),
                ("clickSound", AssetDatabase.LoadAssetAtPath<AudioClip>(ClickSound)));
            return button;
        }

        private static void Tint(Selectable selectable)
        {
            ColorBlock colours = selectable.colors;
            colours.highlightedColor = Color.white;
            colours.selectedColor = Color.white;
            colours.pressedColor = new Color(0.88f, 0.88f, 0.88f);
            selectable.colors = colours;
        }

        private static void Link(Selectable selectable, Selectable up, Selectable down)
        {
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            selectable.navigation = navigation;
        }

        private static CanvasGroup AddScreen(Transform canvas, string name)
        {
            Transform group = Group(canvas, name);
            Stretch(group);
            return group.gameObject.AddComponent<CanvasGroup>();
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void Stretch(Transform transform)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Stretch(Transform transform, float inset, float height)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(inset, -height / 2f);
            rect.offsetMax = new Vector2(-inset, height / 2f);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position, Vector2 pivot, bool bottomAnchor = false)
        {
            rect.anchorMin = rect.anchorMax = bottomAnchor ? new Vector2(0.5f, 0f) : Centre;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, Vector2 pivot,
            bool bottomAnchor = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, size, position, pivot, bottomAnchor);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A scrolling layer: a RawImage as wide as the design showing part of a strip that repeats.</summary>
        private static RawImage AddStrip(Transform parent, string name, float height, float centreY)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, new Vector2(Reference.x, height), new Vector2(0f, centreY), Centre);
            var image = go.GetComponent<RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{BackgroundSprites}/{name}.png");
            if (image.texture == null)
                throw new FileNotFoundException("Background strip not found", $"{BackgroundSprites}/{name}.png");
            image.uvRect = new Rect(0f, 0f, Reference.x / image.texture.width, 1f);
            image.raycastTarget = false;
            return image;
        }

        private static void SetLayers(MenuParallax parallax, List<(RawImage image, float speed)> layers)
        {
            var serialized = new SerializedObject(parallax);
            SerializedProperty list = serialized.FindProperty("layers");
            list.arraySize = layers.Count;
            for (int i = 0; i < layers.Count; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("image").objectReferenceValue = layers[i].image;
                element.FindPropertyRelative("speed").floatValue = layers[i].speed;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSlicedBorder(string path, Vector4 border)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        private static Sprite MenuSprite(string name) => PengembaraDemoBuilder.LoadSprite($"{MenuSprites}/{name}.png");

        private static Sprite BackgroundSprite(string name) => PengembaraDemoBuilder.LoadSprite($"{BackgroundSprites}/{name}.png");

        private static void SetBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == ScenePath || existing.path == GameScenePath)
                    continue;
                scenes.Add(new EditorBuildSettingsScene(existing.path, existing.enabled && existing.path != TemplateScenePath));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
