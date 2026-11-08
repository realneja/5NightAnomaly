using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FiveNightAnomaly.Backgrounds;
using FiveNightAnomaly.Cameras;
using FiveNightAnomaly.Characters;
using FiveNightAnomaly.Combat;
using FiveNightAnomaly.Effects;
using FiveNightAnomaly.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FiveNightAnomaly.EditorTools
{
    /// <summary>
    /// Builds the Pengembara animation sets of every hand item (clips, the Animator Controller of the lantern staff and
    /// an override controller per other hand), the hand items with their attacks (Data/Hands, Data/Attacks), the effect
    /// and projectile prefabs, the Hand1..Hand4 input actions and the prefab (charge glow and sound, HandCombat) from the
    /// frames, Pengembara_Anim.json and WAVs that ArtSource/tools/sync_unity.py copies in; the lane test
    /// assets (straw dummy prefab with health bar, spark effect, walkable tile and its Tile Palette "Lane"); and the
    /// night lane demo scene with the five parallax layers, the walkable tiles and the dummy. Menu: Tools > 5 Night
    /// Anomaly. Running it again after new frames or sounds are copied in updates the clips, controller and prefabs in
    /// place, so their GUIDs and references stay. Layout: Sprites/&lt;Type&gt;/, Animations/&lt;Type&gt;/&lt;Name&gt;/,
    /// Audio/SFX/&lt;Type&gt;/&lt;Name&gt;/, Prefabs/&lt;Type&gt;/, Tiles/, Scenes/ (all under Assets/_Game).
    /// </summary>
    public static class PengembaraDemoBuilder
    {
        private const string CharacterSprites = "Assets/_Game/Sprites/Characters/Pengembara";
        private const string AnimationFolder = "Assets/_Game/Animations/Characters/Pengembara";
        private const string AnimSpecPath = AnimationFolder + "/Pengembara_Anim.json";
        private const string ControllerPath = AnimationFolder + "/Pengembara.controller";
        private const string SoundFolder = "Assets/_Game/Audio/SFX/Characters/Pengembara";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Characters";
        private const string PrefabPath = PrefabFolder + "/Pengembara.prefab";
        private const string BackgroundSprites = "Assets/_Game/Sprites/Backgrounds/Night";
        private const string FxSprites = "Assets/_Game/Sprites/FX";
        private const string ObjectSprites = "Assets/_Game/Sprites/Objects";
        private const string BarSprites = "Assets/_Game/Sprites/UI/World";
        private const string TileSprites = "Assets/_Game/Sprites/Tiles";
        private const string FxPrefabFolder = "Assets/_Game/Prefabs/FX";
        private const string HandFolder = "Assets/_Game/Data/Hands";
        private const string AttackFolder = "Assets/_Game/Data/Attacks";
        private const int HandSlots = 4;
        private const string SlimeName = "SlimeAmber";
        private const string SlimeSprite = "Assets/_Game/Sprites/Characters/SlimeAmber/SlimeAmber.png";
        private const string SlimeAnimationFolder = "Assets/_Game/Animations/Characters/SlimeAmber";
        private const string SlimePrefabPath = "Assets/_Game/Prefabs/Characters/SlimeAmber.prefab";
        private const float SlimeIdlePeriod = 1.6f;
        private const string SlimeSound = "Assets/_Game/Audio/SFX/Characters/SlimeAmber/SlimeAmber_Land.wav";
        private const string ShadowSprite = "Assets/_Game/Sprites/FX/ShadowGround/ShadowGround.png";
        private static readonly Vector2 SlimeStart = new Vector2(-5.2f, -2.3f);
        private const string ObjectPrefabFolder = "Assets/_Game/Prefabs/Objects";
        private const string DummyPrefabPath = ObjectPrefabFolder + "/DummyStraw.prefab";
        private const string TileFolder = "Assets/_Game/Tiles";
        private const string WalkableTilePath = TileFolder + "/MarkerWalkable.asset";
        private const string PaletteFolder = TileFolder + "/Palettes";
        private const string PalettePath = PaletteFolder + "/Lane.prefab";
        private const string ScenePath = "Assets/_Game/Scenes/NightLaneDemo.unity";
        private const string SceneTemplatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        private const float LayerWidth = 15f;
        private const float LayerHeight = 8.4375f;
        private const float GroundLineFromTop = 6.3f;
        private const float LaneDepth = 0.35f;
        private const float LaneStart = -7.5f;
        private const float LaneEnd = 37.5f;
        private const float PlayerStartX = -3f;
        private const float GlowFramesPerSecond = 12f;
        private const float LightningHeight = 4f;

        // Effect prefabs (Prefabs/FX): frames FX/<Name>/<Name>_<Clip>_<n>, frames per second, played once (destroyed) or looping.
        private static readonly (string name, string clip, float fps, bool loop)[] EffectSpecs =
        {
            ("SparkCharge", "Burst", 24f, false), ("SparkElectric", "Burst", 24f, false), ("LightningSky", "Strike", 20f, false),
            ("PuffFire", "Burst", 24f, false), ("ExplosionFire", "Burst", 20f, false),
            ("BoltZeus", "Fly", 18f, true), ("FireballWizard", "Fly", 16f, true),
        };
        private static readonly string[] Projectiles = { "BoltZeus", "FireballWizard" };
        private static readonly string[] HandOrder = { "Staff", "Zeus", "Wizard", "Fist" };
        private const float DummyX = 2.5f;
        private const float DummyY = -2.9f;
        private const float HealthBarHeight = 1.75f;
        // Walkable cells: two rows covering y -4 to -2 (the ground strip), so the feet stay between -3.8 and -2.2.
        private static readonly int[] WalkableRows = { -4, -3 };

        private static readonly string[] SortingLayerNames =
            { "Sky", "FarForest", "MidForest", "Ground", "Objects", "Characters", "FX", "Foreground", "UI" };

        private static readonly (string sprite, string sortingLayer, float follow)[] Layers =
        {
            ("NightSky", "Sky", 1f),
            ("NightFarForest", "FarForest", 0.8f),
            ("NightMidForest", "MidForest", 0.5f),
            ("NightGround", "Ground", 0f),
            ("NightForeground", "Foreground", -0.35f),
        };

#pragma warning disable CS0649 // filled by JsonUtility
        [Serializable]
        private class AnimSpec
        {
            public string character;
            public float walkSpeed;
            public int sampleRate;
            public HandSpec[] hands;
        }

        [Serializable]
        private class HandSpec
        {
            public string name;
            public string prefix;
            public float[] attackTip;
            public float[] chargeTip;
            public float[] chargeGlow;
            public ClipSpec[] clips;
        }

        [Serializable]
        private class ClipSpec
        {
            public string name;
            public bool loop;
            public FrameSpec[] frames;
            public EventSpec[] events;
        }

        [Serializable]
        private class FrameSpec
        {
            public string sprite;
            public float duration;
        }

        [Serializable]
        private class EventSpec
        {
            public int frame;
            public string function;
        }
#pragma warning restore CS0649

        [MenuItem("Tools/5 Night Anomaly/Rebuild Pengembara Animations")]
        private static void RebuildCharacterMenu() => Debug.Log(BuildCharacter());

        [MenuItem("Tools/5 Night Anomaly/Rebuild Slime Amber")]
        private static void RebuildSlimeMenu() => Debug.Log(BuildSlime());

        [MenuItem("Tools/5 Night Anomaly/Rebuild Dummy And Walkable Tile")]
        private static void RebuildLaneAssetsMenu() => Debug.Log(BuildLaneAssets());

        [MenuItem("Tools/5 Night Anomaly/Add Walkable Lane And Dummy To Open Scene")]
        private static void AddLaneMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            Debug.Log(AddLaneToScene(scene));
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/5 Night Anomaly/Build Night Lane Demo Scene")]
        private static void BuildSceneMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Night Lane Demo",
                    ScenePath + " already exists. Build it again from scratch?", "Rebuild", "Cancel"))
                return;
            Debug.Log(BuildAll());
        }

        public static string BuildAll() => BuildCharacter() + "\n" + BuildLaneAssets() + "\n" + BuildScene();

        public static string BuildCharacter()
        {
            EnsureSortingLayers();
            ApplyImportRules(CharacterSprites);
            ApplyImportRules(SoundFolder);
            ApplyImportRules(FxSprites);
            EnsureHandActions();
            Dictionary<string, GameObject> effects = BuildEffects();
            AnimSpec spec = LoadSpec();
            HandSpec staff = Hand(spec, "Staff");
            Dictionary<string, AnimationClip> staffClips = BuildClips(spec, staff);
            AnimatorController controller = BuildController(staffClips);
            var animations = new Dictionary<string, RuntimeAnimatorController> { ["Staff"] = controller };
            foreach (HandSpec hand in spec.hands)
            {
                if (hand.name != "Staff")
                    animations[hand.name] = BuildOverride(controller, staffClips, BuildClips(spec, hand), hand.prefix);
            }
            HandItem[] hands = BuildHands(spec, animations, effects);
            BuildPrefab(spec, controller, hands);
            GizmoUtility.SetIconEnabled(typeof(AudioSource), false);
            AssetDatabase.SaveAssets();
            return $"{spec.character}: hands {string.Join(", ", hands.Select(h => h.name))}; clips per hand {string.Join(", ", staffClips.Keys)}; "
                + $"{ControllerPath} + {animations.Count - 1} override controllers; effects {string.Join(", ", effects.Keys)}; {PrefabPath}";
        }

        /// <summary>
        /// The amber slime (a painted asset used as it is: ArtSource/raster/Characters/SlimeAmber.png, background cut off,
        /// exported at PPU 128). Idle = the whole sprite breathing on its bottom-centre pivot (Animator on Visual); the
        /// SlimeHopper on the root hops it back and forth (Body = jump height, Squash = squash and stretch, Shadow on the
        /// ground), takes hits (Health 80, footprint trigger, HitShake) and shows the red health bar above it.
        /// Hierarchy: SlimeAmber > Shadow, Body > Squash > Visual, Body > HealthBar > Frame, Trail, Fill. No frame is redrawn.
        /// </summary>
        public static string BuildSlime()
        {
            EnsureSortingLayers();
            ApplyImportRules(Path.GetDirectoryName(SlimeSprite)?.Replace('\\', '/'));
            ApplyImportRules(Path.GetDirectoryName(SlimeSound)?.Replace('\\', '/'));
            ApplyImportRules(Path.GetDirectoryName(ShadowSprite)?.Replace('\\', '/'));
            ApplyImportRules(BarSprites);
            EnsureFolder(SlimeAnimationFolder);
            AnimationClip idle = BuildBreathingIdle($"{SlimeAnimationFolder}/{SlimeName}_Idle.anim");

            string controllerPath = $"{SlimeAnimationFolder}/{SlimeName}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states)
                machine.RemoveState(child.state);
            AnimatorState state = machine.AddState("Idle", new Vector3(300f, 0f));
            state.motion = idle;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);

            EnsureFolder(PrefabFolder);
            var root = new GameObject(SlimeName);
            try
            {
                var footprint = root.AddComponent<BoxCollider2D>();
                footprint.isTrigger = true;
                footprint.size = new Vector2(0.9f, 0.3f);
                var health = root.AddComponent<Health>();
                SetFields(health, ("maxHealth", 80));
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;

                GameObject shadow = NewChild(root.transform, "Shadow");
                shadow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = LoadSprite(ShadowSprite);
                shadowRenderer.sortingLayerName = "Ground";
                shadowRenderer.sortingOrder = 20;

                GameObject body = NewChild(root.transform, "Body");
                GameObject squash = NewChild(body.transform, "Squash");
                GameObject visual = NewChild(squash.transform, "Visual");
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite(SlimeSprite);
                renderer.sortingLayerName = "Characters";
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                visual.AddComponent<Animator>().runtimeAnimatorController = controller;
                var shake = visual.AddComponent<HitShake>();

                GameObject bar = NewChild(body.transform, "HealthBar");
                bar.transform.localPosition = new Vector3(0f, renderer.sprite.bounds.size.y + 0.22f, 0f);
                BarPart(bar.transform, "Frame", "BarEnemyFrame", 0);
                SpriteRenderer trail = BarPart(bar.transform, "Trail", "BarEnemyTrail", 1);
                SpriteRenderer fill = BarPart(bar.transform, "Fill", "BarEnemyFill", 2);
                SetFields(bar.AddComponent<HealthBar>(), ("health", health), ("fill", fill), ("trail", trail));

                SetFields(root.AddComponent<SlimeHopper>(),
                    ("body", body.transform), ("squash", squash.transform), ("sprite", renderer), ("shadow", shadowRenderer),
                    ("shake", shake), ("audioSource", source), ("landSound", AssetDatabase.LoadAssetAtPath<AudioClip>(SlimeSound)));
                PrefabUtility.SaveAsPrefabAsset(root, SlimePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            return $"{SlimeName}: {idle.length:0.0} s idle loop; {controllerPath}; {SlimePrefabPath}";
        }

        /// <summary>Adds the slime to Characters in a scene when it is not there yet (left end of the lane).</summary>
        public static string AddSlimeToScene(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Run Tools > 5 Night Anomaly > Rebuild Slime Amber first.");
            GameObject characters = FindRoot(scene, "Characters") ?? NewRoot(scene, "Characters");
            if (characters.transform.Find(SlimeName) != null)
                return $"{scene.name}: Characters/{SlimeName} already there";
            var slime = (GameObject)PrefabUtility.InstantiatePrefab(prefab, characters.transform);
            slime.transform.position = SlimeStart;
            return $"{scene.name}: Characters/{SlimeName} added at {SlimeStart}";
        }

        private static AnimationClip BuildBreathingIdle(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.ClearCurves();
            clip.frameRate = 60f;
            float period = SlimeIdlePeriod;
            // Taller and thinner, then lower and wider; a small second wobble on top; the sway lags behind the stretch.
            SetCurve(clip, "m_LocalScale.x", Wave(period, 1f, -0.03f, 0f, -0.006f, 0.5f));
            SetCurve(clip, "m_LocalScale.y", Wave(period, 1f, 0.035f, 0f, 0.008f, 0.5f));
            SetCurve(clip, "localEulerAnglesRaw.x", Wave(period, 0f, 0f, 0f, 0f, 0f));
            SetCurve(clip, "localEulerAnglesRaw.y", Wave(period, 0f, 0f, 0f, 0f, 0f));
            SetCurve(clip, "localEulerAnglesRaw.z", Wave(period, 0f, 1.2f, -0.9f, 0f, 0f));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void SetCurve(AnimationClip clip, string property, AnimationCurve curve) =>
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), property), curve);

        /// <summary>mean + a1 sin(2 pi t / period + p1) + a2 sin(4 pi t / period + p2), 17 keys with exact slopes (seamless loop).</summary>
        private static AnimationCurve Wave(float period, float mean, float a1, float p1, float a2, float p2)
        {
            var keys = new Keyframe[17];
            float w = 2f * Mathf.PI / period;
            for (int i = 0; i < keys.Length; i++)
            {
                float t = period * i / (keys.Length - 1);
                float value = mean + a1 * Mathf.Sin(w * t + p1) + a2 * Mathf.Sin(2f * w * t + p2);
                float slope = a1 * w * Mathf.Cos(w * t + p1) + a2 * 2f * w * Mathf.Cos(2f * w * t + p2);
                keys[i] = new Keyframe(t, value, slope, slope);
            }
            return new AnimationCurve(keys);
        }

        public static string BuildLaneAssets()
        {
            EnsureSortingLayers();
            ApplyImportRules(ObjectSprites);
            ApplyImportRules(BarSprites);
            ApplyImportRules(TileSprites);
            BuildDummy();
            Tile tile = BuildWalkableTile();
            BuildPalette(tile);
            UseYSorting();
            AssetDatabase.SaveAssets();
            return $"{DummyPrefabPath}; {WalkableTilePath}; {PalettePath}; sprites on one sorting layer are drawn by their Y position";
        }

        /// <summary>
        /// Adds what the lane test needs to a scene that is already set up: LaneGrid/Walkable (tilemap painted over the
        /// ground strip, only when it is still empty, so tiles painted by hand stay), Objects/DummyStraw, and the
        /// walkable area on every PengembaraController. Existing objects are reused, so running it twice adds nothing.
        /// </summary>
        public static string AddLaneToScene(Scene scene)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(WalkableTilePath);
            var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefabPath);
            if (tile == null || dummyPrefab == null)
                throw new InvalidOperationException("Run Tools > 5 Night Anomaly > Rebuild Dummy And Walkable Tile first.");

            GameObject grid = FindRoot(scene, "LaneGrid") ?? NewRoot(scene, "LaneGrid");
            if (!grid.TryGetComponent(out Grid _))
                grid.AddComponent<Grid>().cellSize = new Vector3(1f, 1f, 0f);
            Transform child = grid.transform.Find("Walkable");
            GameObject walkable = child != null ? child.gameObject : NewChild(grid.transform, "Walkable");
            if (!walkable.TryGetComponent(out Tilemap tilemap))
                tilemap = walkable.AddComponent<Tilemap>();
            if (!walkable.TryGetComponent(out TilemapRenderer tilemapRenderer))
                tilemapRenderer = walkable.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingLayerName = "Ground";
            tilemapRenderer.sortingOrder = 10;
            if (!walkable.TryGetComponent(out WalkableArea area))
                area = walkable.AddComponent<WalkableArea>();
            int painted = 0;
            if (tilemap.GetUsedTilesCount() == 0)
            {
                for (int x = Mathf.CeilToInt(LaneStart); x < Mathf.FloorToInt(LaneEnd); x++)
                {
                    foreach (int y in WalkableRows)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                        painted++;
                    }
                }
            }

            GameObject objects = FindRoot(scene, "Objects") ?? NewRoot(scene, "Objects");
            if (objects.transform.Find("DummyStraw") == null)
            {
                var dummy = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, objects.transform);
                dummy.transform.position = new Vector3(DummyX, DummyY, 0f);
            }

            int players = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (PengembaraController player in root.GetComponentsInChildren<PengembaraController>(true))
                {
                    SetFields(player, ("walkableArea", area));
                    players++;
                }
            }
            return $"{scene.name}: LaneGrid/Walkable ({(painted > 0 ? painted + " tiles painted" : "kept the painted tiles")}), Objects/DummyStraw, walkable area set on {players} Pengembara";
        }

        public static string BuildScene()
        {
            EnsureSortingLayers();
            ApplyImportRules(BackgroundSprites);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException(PrefabPath + " is missing; run Rebuild Pengembara Animations first.");
            Scene loaded = SceneManager.GetSceneByPath(ScenePath);
            if (loaded.IsValid() && loaded.isLoaded)
                throw new InvalidOperationException(ScenePath + " is open; open another scene before rebuilding it.");
            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SceneTemplatePath, ScenePath))
                throw new InvalidOperationException("Could not copy " + SceneTemplatePath);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject cameraObject = RootObject(scene, "Main Camera");
                cameraObject.name = "MainCamera";
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = LayerHeight / 2f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(0x15, 0x0c, 0x33, 0xff);

                GameObject lightObject = RootObject(scene, "Global Light 2D");
                lightObject.name = "GlobalLight2D";
                var globalLight = lightObject.GetComponent<Light2D>();
                globalLight.color = Color.white;
                globalLight.intensity = 1f;
                globalLight.targetSortingLayers = SortingLayer.layers.Select(layer => layer.id).ToArray();

                var background = new GameObject("Background");
                SceneManager.MoveGameObjectToScene(background, scene);
                foreach ((string sprite, string sortingLayer, float follow) in Layers)
                {
                    var layer = new GameObject(sprite);
                    layer.transform.SetParent(background.transform, false);
                    var renderer = layer.AddComponent<SpriteRenderer>();
                    renderer.sprite = LoadSprite($"{BackgroundSprites}/{sprite}.png");
                    renderer.sortingLayerName = sortingLayer;
                    renderer.drawMode = SpriteDrawMode.Tiled;
                    renderer.tileMode = SpriteTileMode.Continuous;
                    renderer.size = new Vector2(LayerWidth * 3f, LayerHeight);
                    var parallax = layer.AddComponent<ParallaxLayer>();
                    SetFields(parallax, ("cameraTransform", camera.transform), ("followFactor", follow), ("repeatWidth", LayerWidth));
                }

                var characters = new GameObject("Characters");
                SceneManager.MoveGameObjectToScene(characters, scene);
                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, characters.transform);
                player.transform.position = new Vector3(PlayerStartX, LayerHeight / 2f - GroundLineFromTop - LaneDepth, 0f);
                AddLaneToScene(scene);

                float halfView = LayerWidth / 2f;
                var cameraFollow = camera.gameObject.AddComponent<CameraFollow2D>();
                SetFields(cameraFollow, ("target", player.transform), ("limitsX", new Vector2(LaneStart + halfView, LaneEnd - halfView)));

                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save " + ScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
            return $"{ScenePath}: MainCamera, GlobalLight2D, Background ({Layers.Length} parallax layers), Characters/Pengembara, LaneGrid/Walkable, Objects/DummyStraw";
        }

        private static AnimSpec LoadSpec()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(AnimSpecPath);
            if (json == null)
                throw new FileNotFoundException("Copy Pengembara_Anim.json from ArtSource/export/Characters/Pengembara", AnimSpecPath);
            return JsonUtility.FromJson<AnimSpec>(json.text);
        }

        private static HandSpec Hand(AnimSpec spec, string name)
        {
            HandSpec hand = spec.hands?.FirstOrDefault(h => h.name == name);
            if (hand == null)
                throw new InvalidOperationException($"{AnimSpecPath} has no {name} hand; run ArtSource/tools/gen_pengembara_anim.py and sync_unity.py again.");
            return hand;
        }

        private static Dictionary<string, AnimationClip> BuildClips(AnimSpec spec, HandSpec hand)
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (ClipSpec clipSpec in hand.clips)
                clips[clipSpec.name] = BuildClip(spec, hand.prefix, clipSpec);
            return clips;
        }

        private static AnimatorOverrideController BuildOverride(AnimatorController baseController,
            IReadOnlyDictionary<string, AnimationClip> baseClips, IReadOnlyDictionary<string, AnimationClip> clips, string prefix)
        {
            string path = $"{AnimationFolder}/{prefix}.overrideController";
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (overrideController == null)
            {
                overrideController = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(overrideController, path);
            }
            overrideController.runtimeAnimatorController = baseController;
            var pairs = baseClips.Select(entry => new KeyValuePair<AnimationClip, AnimationClip>(entry.Value, clips[entry.Key])).ToList();
            overrideController.ApplyOverrides(pairs);
            EditorUtility.SetDirty(overrideController);
            return overrideController;
        }

        private static AnimationClip BuildClip(AnimSpec spec, string prefix, ClipSpec clipSpec)
        {
            EnsureFolder(AnimationFolder);
            string path = $"{AnimationFolder}/{prefix}_{clipSpec.name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            foreach (EditorCurveBinding old in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, old, null);
            clip.frameRate = spec.sampleRate;

            var keys = new List<ObjectReferenceKeyframe>();
            var frameStart = new List<float>();
            float time = 0f;
            Sprite sprite = null;
            foreach (FrameSpec frame in clipSpec.frames)
            {
                sprite = LoadSprite($"{CharacterSprites}/{frame.sprite}.png");
                frameStart.Add(time);
                keys.Add(new ObjectReferenceKeyframe { time = time, value = sprite });
                time += frame.duration;
            }
            float holdKey = time - 1f / spec.sampleRate;
            if (holdKey > frameStart[frameStart.Count - 1] + 0.0001f)
                keys.Add(new ObjectReferenceKeyframe { time = holdKey, value = sprite });
            // Unity plays the last key of a sprite curve for one sample, so the hold key one sample before the end makes the clip exactly as long as its frames.
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = clipSpec.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var events = new List<AnimationEvent>();
            foreach (EventSpec e in clipSpec.events ?? Array.Empty<EventSpec>())
                events.Add(new AnimationEvent { time = frameStart[e.frame], functionName = e.function });
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController BuildController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (AnimatorControllerParameter parameter in controller.parameters)
                controller.RemoveParameter(parameter);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("WalkRate", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Charging", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ChargeRelease", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states)
                machine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
                machine.RemoveAnyStateTransition(transition);

            AnimatorState idle = machine.AddState("Idle", new Vector3(300f, 0f));
            idle.motion = clips["Idle"];
            AnimatorState walk = machine.AddState("Walk", new Vector3(300f, 120f));
            walk.motion = clips["Walk"];
            walk.speedParameter = "WalkRate";
            walk.speedParameterActive = true;
            AnimatorState attack = machine.AddState("Attack", new Vector3(580f, 60f));
            attack.motion = clips["Attack"];
            AnimatorState charge = machine.AddState("Charge", new Vector3(580f, 200f));
            charge.motion = clips["Charge"];
            AnimatorState chargeAttack = machine.AddState("ChargeAttack", new Vector3(860f, 130f));
            chargeAttack.motion = clips["ChargeAttack"];
            machine.defaultState = idle;

            // Attack = a short press let go, Charging = the button held past holdToCharge (straight into the Charge pose),
            // ChargeRelease = let go when the charge is ready. The first transition in a state's list wins.
            Instant(idle, attack).AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            Instant(walk, attack).AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            Instant(idle, charge).AddCondition(AnimatorConditionMode.If, 0f, "Charging");
            Instant(walk, charge).AddCondition(AnimatorConditionMode.If, 0f, "Charging");
            Instant(idle, walk).AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
            Instant(walk, idle).AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
            AtEnd(attack, idle);
            Instant(charge, chargeAttack).AddCondition(AnimatorConditionMode.If, 0f, "ChargeRelease");
            Instant(charge, attack).AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            Instant(charge, idle).AddCondition(AnimatorConditionMode.IfNot, 0f, "Charging");
            AtEnd(chargeAttack, idle);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorStateTransition AtEnd(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
            transition.hasFixedDuration = true;
            return transition;
        }

        private static AnimatorStateTransition Instant(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.exitTime = 0f;
            transition.duration = 0f;
            transition.hasFixedDuration = true;
            return transition;
        }

        private static void BuildPrefab(AnimSpec spec, AnimatorController controller, HandItem[] hands)
        {
            EnsureFolder(PrefabFolder);
            var root = new GameObject(spec.character);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSprite($"{CharacterSprites}/{Hand(spec, "Staff").clips[0].frames[0].sprite}.png");
                renderer.sortingLayerName = "Characters";
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                var animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                var mover = root.AddComponent<PengembaraController>();
                SetFields(mover,
                    ("moveAction", FindAction("Player", "Move")),
                    ("attackAction", FindAction("Player", "Attack")),
                    ("moveSpeed", spec.walkSpeed),
                    ("walkClipSpeed", spec.walkSpeed));
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                var sounds = root.AddComponent<CharacterAudio>();
                SetFields(sounds, ("footsteps", LoadClips($"{spec.character}_Footstep_")));

                GameObject glowObject = NewChild(root.transform, "ChargeGlow");
                Vector2 lantern = Point(Hand(spec, "Staff").chargeGlow, "chargeGlow");
                glowObject.transform.localPosition = lantern;
                var glowRenderer = glowObject.AddComponent<SpriteRenderer>();
                Sprite[] glowFrames = LoadFrames($"{FxSprites}/GlowCharge", "GlowCharge_Loop_");
                glowRenderer.sprite = glowFrames[0];
                glowRenderer.sortingLayerName = "FX";
                glowRenderer.enabled = false;
                SetFields(glowObject.AddComponent<SpriteFlipbook>(),
                    ("frames", glowFrames), ("framesPerSecond", GlowFramesPerSecond), ("loop", true), ("destroyAtEnd", false));
                var glow = glowObject.AddComponent<ChargeGlow>();
                SetFields(glow, ("controller", mover), ("offset", lantern));

                GameObject soundObject = NewChild(root.transform, "ChargeSound");
                var loopSource = soundObject.AddComponent<AudioSource>();
                loopSource.clip = LoadClip($"{spec.character}_ChargeLoop");
                loopSource.loop = true;
                loopSource.playOnAwake = false;
                loopSource.spatialBlend = 0f;
                loopSource.volume = 0f;
                var chargeSound = soundObject.AddComponent<ChargeSound>();
                SetFields(chargeSound, ("controller", mover));

                var handCombat = root.AddComponent<HandCombat>();
                SetFields(handCombat,
                    ("hands", hands),
                    ("handActions", Enumerable.Range(1, HandSlots).Select(i => (UnityEngine.Object)FindAction("Player", "Hand" + i)).ToArray()),
                    ("chargeGlow", glow),
                    ("chargeSound", chargeSound));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Dictionary<string, GameObject> BuildEffects()
        {
            EnsureFolder(FxPrefabFolder);
            var prefabs = new Dictionary<string, GameObject>();
            foreach ((string name, string clip, float fps, bool loop) in EffectSpecs)
            {
                var root = new GameObject(name);
                try
                {
                    var renderer = root.AddComponent<SpriteRenderer>();
                    Sprite[] frames = LoadFrames($"{FxSprites}/{name}", $"{name}_{clip}_");
                    renderer.sprite = frames[0];
                    renderer.sortingLayerName = "FX";
                    SetFields(root.AddComponent<SpriteFlipbook>(),
                        ("frames", frames), ("framesPerSecond", fps), ("loop", loop), ("destroyAtEnd", !loop));
                    if (Projectiles.Contains(name))
                        root.AddComponent<Projectile>();
                    prefabs[name] = PrefabUtility.SaveAsPrefabAsset(root, $"{FxPrefabFolder}/{name}.prefab");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            return prefabs;
        }

        private static T LoadOrCreate<T>(string folder, string name) where T : ScriptableObject
        {
            EnsureFolder(folder);
            string path = $"{folder}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static T Configure<T>(T asset, params (string name, object value)[] fields) where T : UnityEngine.Object
        {
            SetFields(asset, fields);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        /// <summary>The four hand items in slot order (keys 1 to 4) with their attacks. Numbers are the starting balance.</summary>
        private static HandItem[] BuildHands(AnimSpec spec, IReadOnlyDictionary<string, RuntimeAnimatorController> animations,
            IReadOnlyDictionary<string, GameObject> effects)
        {
            string c = spec.character;
            Projectile Shot(string name) => effects[name].GetComponent<Projectile>();
            HandSpec staff = Hand(spec, "Staff"), zeus = Hand(spec, "Zeus"), wizard = Hand(spec, "Wizard"), fist = Hand(spec, "Fist");

            var staffSwing = Configure(LoadOrCreate<MeleeAttack>(AttackFolder, "AttackStaffSwing"),
                ("damage", 10), ("heavy", false), ("tip", Point(staff.attackTip, "attackTip")), ("reach", new Vector2(0.2f, 1.1f)),
                ("depth", 0.3f), ("spark", null), ("releaseSoundOnMiss", true),
                ("swingSound", LoadClip($"{c}_AttackSwing")), ("releaseSound", LoadClip($"{c}_AttackImpact")), ("volume", 0.8f), ("pitch", 1f));
            var staffCharge = Configure(LoadOrCreate<MeleeAttack>(AttackFolder, "AttackStaffCharge"),
                ("damage", 35), ("heavy", true), ("tip", Point(staff.chargeTip, "chargeTip")), ("reach", new Vector2(0.2f, 1.45f)),
                ("depth", 0.4f), ("spark", effects["SparkCharge"]), ("releaseSoundOnMiss", true),
                ("swingSound", LoadClip($"{c}_ChargeSwing")), ("releaseSound", LoadClip($"{c}_ChargeImpact")), ("volume", 1f), ("pitch", 1f));
            var zeusThrow = Configure(LoadOrCreate<ProjectileAttack>(AttackFolder, "AttackZeusThrow"),
                ("damage", 12), ("heavy", false), ("tip", Point(zeus.attackTip, "attackTip")), ("projectile", Shot("BoltZeus")),
                ("speed", 12f), ("range", 8f), ("depth", 0.35f), ("area", Vector2.zero), ("scale", 1f),
                ("impactEffect", effects["SparkElectric"]), ("effectOnGround", false), ("impactSound", LoadClip($"{c}Zeus_Hit")),
                ("swingSound", LoadClip($"{c}Zeus_Throw")), ("releaseSound", null), ("volume", 0.8f), ("pitch", 1f));
            var zeusSky = Configure(LoadOrCreate<SkyStrikeAttack>(AttackFolder, "AttackZeusSky"),
                ("damage", 20), ("heavy", true), ("tip", Point(zeus.chargeTip, "chargeTip")), ("strikes", 3), ("range", 7f),
                ("spacing", 1.6f), ("interval", 0.15f), ("radius", new Vector2(0.6f, 0.45f)), ("lightning", effects["LightningSky"]),
                ("lightningHeight", LightningHeight), ("strikeSound", LoadClip($"{c}Zeus_Strike")),
                ("swingSound", LoadClip($"{c}Zeus_ChargeCall")), ("releaseSound", null), ("volume", 0.9f), ("pitch", 1f));
            var wizardFireball = Configure(LoadOrCreate<ProjectileAttack>(AttackFolder, "AttackWizardFireball"),
                ("damage", 15), ("heavy", false), ("tip", Point(wizard.attackTip, "attackTip")), ("projectile", Shot("FireballWizard")),
                ("speed", 8f), ("range", 10f), ("depth", 0.35f), ("area", Vector2.zero), ("scale", 1f),
                ("impactEffect", effects["PuffFire"]), ("effectOnGround", false), ("impactSound", LoadClip($"{c}Wizard_Hit")),
                ("swingSound", LoadClip($"{c}Wizard_Cast")), ("releaseSound", null), ("volume", 0.8f), ("pitch", 1f));
            var wizardBlast = Configure(LoadOrCreate<ProjectileAttack>(AttackFolder, "AttackWizardBlast"),
                ("damage", 30), ("heavy", true), ("tip", Point(wizard.chargeTip, "chargeTip")), ("projectile", Shot("FireballWizard")),
                ("speed", 7f), ("range", 10f), ("depth", 0.35f), ("area", new Vector2(3f, 3f)), ("scale", 1.7f),
                ("impactEffect", effects["ExplosionFire"]), ("effectOnGround", true), ("impactSound", LoadClip($"{c}Wizard_Explosion")),
                ("swingSound", LoadClip($"{c}Wizard_ChargeCast")), ("releaseSound", null), ("volume", 0.95f), ("pitch", 1f));
            var fistPunch = Configure(LoadOrCreate<MeleeAttack>(AttackFolder, "AttackFistPunch"),
                ("damage", 6), ("heavy", false), ("tip", Point(fist.attackTip, "attackTip")), ("reach", new Vector2(0.15f, 0.75f)),
                ("depth", 0.3f), ("spark", null), ("releaseSoundOnMiss", false),
                ("swingSound", LoadClip($"{c}Fist_Punch")), ("releaseSound", LoadClip($"{c}Fist_Hit")), ("volume", 0.8f), ("pitch", 1f));
            var fistCharge = Configure(LoadOrCreate<MeleeAttack>(AttackFolder, "AttackFistCharge"),
                ("damage", 25), ("heavy", true), ("tip", Point(fist.chargeTip, "chargeTip")), ("reach", new Vector2(0.15f, 1f)),
                ("depth", 0.4f), ("spark", effects["SparkCharge"]), ("releaseSoundOnMiss", false),
                ("swingSound", LoadClip($"{c}Fist_Punch")), ("releaseSound", LoadClip($"{c}Fist_ChargeHit")), ("volume", 1f), ("pitch", 0.85f));

            HandItem Item(string name, string label, Attack attack, Attack charge, float chargeTime, string glow, HandSpec hand, float glowScale, string loop) =>
                Configure(LoadOrCreate<HandItem>(HandFolder, "Hand" + name),
                    ("displayName", label), ("animations", animations[name]), ("attack", attack), ("chargeAttack", charge),
                    ("chargeTime", chargeTime), ("chargeGlow", LoadFrames($"{FxSprites}/{glow}", $"{glow}_Loop_")),
                    ("chargeGlowOffset", Point(hand.chargeGlow, "chargeGlow")), ("chargeGlowScale", glowScale), ("chargeLoop", LoadClip(loop)));

            return new[]
            {
                Item("Staff", "Tongkat lentera", staffSwing, staffCharge, 0.6f, "GlowCharge", staff, 1f, $"{c}_ChargeLoop"),
                Item("Zeus", "Petir Zeus", zeusThrow, zeusSky, 0.8f, "GlowElectric", zeus, 1f, $"{c}Zeus_ChargeLoop"),
                Item("Wizard", "Tongkat penyihir", wizardFireball, wizardBlast, 0.7f, "GlowFire", wizard, 0.9f, $"{c}Wizard_ChargeLoop"),
                Item("Fist", "Tangan kosong", fistPunch, fistCharge, 0.5f, "GlowCharge", fist, 0.7f, $"{c}Fist_ChargeLoop"),
            };
        }

        /// <summary>Adds Player/Hand1..Hand4 (keys 1 to 4) to InputSystem_Actions when they are missing.</summary>
        private static void EnsureHandActions()
        {
            InputActionAsset asset = InputActionAsset.FromJson(File.ReadAllText(InputActionsPath));
            try
            {
                InputActionMap map = asset.FindActionMap("Player", true);
                bool changed = false;
                for (int i = 1; i <= HandSlots; i++)
                {
                    if (map.FindAction("Hand" + i) != null)
                        continue;
                    map.AddAction("Hand" + i, InputActionType.Button, "<Keyboard>/" + i, groups: "Keyboard&Mouse");
                    changed = true;
                }
                if (!changed)
                    return;
                File.WriteAllText(InputActionsPath, asset.ToJson());
                AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static void BuildDummy()
        {
            EnsureFolder(ObjectPrefabFolder);
            var root = new GameObject("DummyStraw");
            try
            {
                var footprint = root.AddComponent<BoxCollider2D>();
                footprint.isTrigger = true;
                footprint.size = new Vector2(0.6f, 0.3f);
                var health = root.AddComponent<Health>();
                var dummy = root.AddComponent<TrainingDummy>();

                GameObject visual = NewChild(root.transform, "Visual");
                var body = visual.AddComponent<SpriteRenderer>();
                body.sprite = LoadSprite($"{ObjectSprites}/DummyStraw.png");
                body.sortingLayerName = "Characters";
                body.spriteSortPoint = SpriteSortPoint.Pivot;
                SetFields(dummy, ("shake", visual.AddComponent<HitShake>()));

                GameObject bar = NewChild(root.transform, "HealthBar");
                bar.transform.localPosition = new Vector3(0f, HealthBarHeight, 0f);
                BarPart(bar.transform, "Frame", "BarEnemyFrame", 0);
                SpriteRenderer trail = BarPart(bar.transform, "Trail", "BarEnemyTrail", 1);
                SpriteRenderer fill = BarPart(bar.transform, "Fill", "BarEnemyFill", 2);
                SetFields(bar.AddComponent<HealthBar>(), ("health", health), ("fill", fill), ("trail", trail));
                PrefabUtility.SaveAsPrefabAsset(root, DummyPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static SpriteRenderer BarPart(Transform bar, string name, string sprite, int order)
        {
            var renderer = NewChild(bar, name).AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite($"{BarSprites}/{sprite}.png");
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Tile BuildWalkableTile()
        {
            EnsureFolder(TileFolder);
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(WalkableTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, WalkableTilePath);
            }
            tile.sprite = LoadSprite($"{TileSprites}/MarkerWalkable.png");
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void BuildPalette(TileBase tile)
        {
            EnsureFolder(PaletteFolder);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) == null)
                GridPaletteUtility.CreateNewPalette(PaletteFolder, "Lane", GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
            GameObject palette = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                palette.GetComponentInChildren<Tilemap>().SetTile(Vector3Int.zero, tile);
                PrefabUtility.SaveAsPrefabAsset(palette, PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(palette);
            }
        }

        /// <summary>Sprites on the same sorting layer and order are drawn by their Y position (lower = nearer = in front).</summary>
        private static void UseYSorting()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Renderer2DData"))
            {
                var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<Renderer2DData>(AssetDatabase.GUIDToAssetPath(guid)));
                data.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
                data.FindProperty("m_TransparencySortAxis").vector3Value = Vector3.up;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Vector2 Point(float[] xy, string name)
        {
            if (xy == null || xy.Length != 2)
                throw new InvalidOperationException($"{AnimSpecPath} has no {name}; run ArtSource/tools/gen_pengembara_anim.py and sync_unity.py again.");
            return new Vector2(xy[0], xy[1]);
        }

        private static InputActionReference FindAction(string map, string action)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath))
            {
                if (asset is InputActionReference reference && (reference.hideFlags & HideFlags.HideInHierarchy) == 0
                    && reference.action != null && reference.action.name == action && reference.action.actionMap?.name == map)
                    return reference;
            }
            throw new InvalidOperationException($"Action {map}/{action} not found in {InputActionsPath}");
        }

        internal static void SetFields(UnityEngine.Object target, params (string name, object value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string name, object value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property == null)
                    throw new InvalidOperationException($"{target.GetType().Name} has no serialized field {name}");
                switch (value)
                {
                    case null: property.objectReferenceValue = null; break;
                    case float number: property.floatValue = number; break;
                    case int whole: property.intValue = whole; break;
                    case bool flag: property.boolValue = flag; break;
                    case string text: property.stringValue = text; break;
                    case Vector2 vector: property.vector2Value = vector; break;
                    case UnityEngine.Object reference: property.objectReferenceValue = reference; break;
                    case UnityEngine.Object[] references:
                        property.arraySize = references.Length;
                        for (int i = 0; i < references.Length; i++)
                            property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
                        break;
                    default: throw new ArgumentException($"Unsupported value for {name}");
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
            var existing = new HashSet<string>();
            for (int i = 0; i < layers.arraySize; i++)
                existing.Add(layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            bool added = false;
            foreach (string name in SortingLayerNames)
            {
                if (existing.Contains(name))
                    continue;
                layers.arraySize++;
                SerializedProperty entry = layers.GetArrayElementAtIndex(layers.arraySize - 1);
                entry.FindPropertyRelative("name").stringValue = name;
                entry.FindPropertyRelative("uniqueID").uintValue = StableId(name);
                entry.FindPropertyRelative("locked").boolValue = false;
                added = true;
            }
            if (added)
                tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static uint StableId(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash == 0 ? 1u : hash;
        }

        internal static void ApplyImportRules(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D t:AudioClip", new[] { folder }))
            {
                AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                bool changed = importer is TextureImporter texture ? AssetImportRules.ApplySprite(texture)
                    : importer is AudioImporter audio && AssetImportRules.ApplySoundEffect(audio);
                if (changed)
                    importer.SaveAndReimport();
            }
        }

        private static AudioClip LoadClip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{name}.wav");

        private static AudioClip[] LoadClips(string prefix)
        {
            if (!AssetDatabase.IsValidFolder(SoundFolder))
                return Array.Empty<AudioClip>();
            return AssetDatabase.FindAssets("t:AudioClip", new[] { SoundFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(prefix))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .ToArray();
        }

        private static Sprite[] LoadFrames(string folder, string prefix)
        {
            Sprite[] frames = AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path).StartsWith(prefix))
                .OrderBy(path => int.Parse(Path.GetFileNameWithoutExtension(path).Substring(prefix.Length)))
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .ToArray();
            if (frames.Length == 0)
                throw new FileNotFoundException($"No {prefix}* sprites (run sync_unity.py)", folder);
            return frames;
        }

        internal static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException("Sprite not found (is the PNG imported as a Sprite?)", path);
            return sprite;
        }

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);

        private static GameObject NewRoot(Scene scene, string name)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject RootObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            throw new InvalidOperationException($"{name} not found in {scene.path}");
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
