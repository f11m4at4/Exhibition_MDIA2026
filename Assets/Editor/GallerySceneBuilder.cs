using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Exhibition.Editor
{
    public static class GallerySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Exhibition.unity";
        private const string LegacyScenePath = "Assets/Scenes/ExhibitionGalleryScene.unity";
        private const string MaterialsFolder = "Assets/Materials/Generated";
        private const string ResourcesFolder = "Assets/Resources/Generated";
        private const string VolumeProfilePath = ResourcesFolder + "/ExhibitionGalleryVolumeProfile.asset";

        private static Material sWallMaterial;
        private static Material sTrimMaterial;
        private static Material sFloorMaterial;
        private static Material sAccentMaterial;
        private static Material sFrameMaterial;
        private static Material sPlinthMaterial;
        private static Material sGlassMaterial;

        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (SessionState.GetBool("ExhibitionGallerySceneBuilder.AutoBuildChecked", false))
            {
                return;
            }

            SessionState.SetBool("ExhibitionGallerySceneBuilder.AutoBuildChecked", true);
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ScenePath)))
                {
                    BuildScene();
                    Debug.Log("Exhibition gallery scene was generated automatically at " + ScenePath);
                }
            };
        }

        [MenuItem("Tools/Exhibition/Build Gallery Scene")]
        public static void BuildFromMenu()
        {
            BuildScene();
        }

        public static void BuildFromCommandLine()
        {
            BuildScene();
            EditorApplication.Exit(0);
        }

        private static void BuildScene()
        {
            EnsureFolder("Assets/Materials");
            EnsureFolder(MaterialsFolder);
            EnsureFolder("Assets/Resources");
            EnsureFolder(ResourcesFolder);
            EnsureFolder("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Exhibition";

            CreateMaterials();
            ConfigureRenderSettings();
            CreateGlobalVolume();
            CreateCamera();
            CreateEnvironmentRoot();
            CreateEntrance();
            CreateGarden();
            CreateMainHall();
            CreateRoomA();
            CreateCentralHall();
            CreateSculptureWing();
            CreateRightGallery();
            CreateLighting();
            CreateReflectionProbe();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), LegacyScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.15f, 0.14f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.09f, 0.08f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.035f, 0.03f);
            RenderSettings.ambientIntensity = 0.75f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.11f, 0.10f, 0.09f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.reflectionIntensity = 0.55f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        private static void CreateGlobalVolume()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            profile.components.Clear();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.18f);
            bloom.scatter.Override(0.65f);

            var volumeObject = new GameObject("Global Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            var transform = cameraObject.transform;
            transform.position = new Vector3(0f, 2.2f, -12.5f);
            transform.rotation = Quaternion.Euler(12f, 0f, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.055f, 0.05f);
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 250f;
            camera.allowHDR = true;

            cameraObject.AddComponent<AudioListener>();
            var additionalData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            additionalData.renderPostProcessing = true;
            additionalData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        private static void CreateEnvironmentRoot()
        {
            var root = new GameObject("GalleryRoot");

            CreateFloor("Exterior Ground", root.transform, new Vector3(0f, -0.1f, 4f), new Vector3(34f, 0.2f, 34f), sTrimMaterial);
            CreateFloor("Interior Base", root.transform, new Vector3(0f, 0f, 6f), new Vector3(26f, 0.2f, 22f), sFloorMaterial);

            CreateWall("North Outer Wall", root.transform, new Vector3(0f, 2.1f, 16f), new Vector3(26f, 4.2f, 0.4f), sWallMaterial);
            CreateWall("South Outer Wall Left", root.transform, new Vector3(-6f, 2.1f, -5f), new Vector3(14f, 4.2f, 0.4f), sWallMaterial);
            CreateWall("South Outer Wall Right", root.transform, new Vector3(6f, 2.1f, -5f), new Vector3(14f, 4.2f, 0.4f), sWallMaterial);
            CreateWall("West Outer Wall", root.transform, new Vector3(-13f, 2.1f, 6f), new Vector3(0.4f, 4.2f, 22f), sWallMaterial);
            CreateWall("East Outer Wall", root.transform, new Vector3(13f, 2.1f, 6f), new Vector3(0.4f, 4.2f, 22f), sWallMaterial);
        }

        private static void CreateEntrance()
        {
            var parent = new GameObject("Entrance").transform;

            CreateFloor("Stairs Landing", parent, new Vector3(0f, 0f, -1.4f), new Vector3(3.8f, 0.25f, 3f), sFloorMaterial);
            CreateFloor("Bridge", parent, new Vector3(0f, 0f, 1.0f), new Vector3(2.8f, 0.2f, 4.0f), sFloorMaterial);
            CreateWall("Left Pillar", parent, new Vector3(-1.85f, 1.5f, -1.2f), new Vector3(0.5f, 3f, 1.4f), sTrimMaterial);
            CreateWall("Right Pillar", parent, new Vector3(1.85f, 1.5f, -1.2f), new Vector3(0.5f, 3f, 1.4f), sTrimMaterial);
            CreateFloor("Step 1", parent, new Vector3(0f, -0.25f, -3.5f), new Vector3(4.2f, 0.25f, 0.8f), sTrimMaterial);
            CreateFloor("Step 2", parent, new Vector3(0f, -0.5f, -4.3f), new Vector3(5f, 0.25f, 0.8f), sTrimMaterial);

            CreatePlant("Left Entrance Plant", parent, new Vector3(-2.6f, 0f, -2.0f), 0.9f);
            CreatePlant("Right Entrance Plant", parent, new Vector3(2.6f, 0f, -2.0f), 0.9f);
        }

        private static void CreateGarden()
        {
            var parent = new GameObject("Garden").transform;
            parent.position = new Vector3(-10.6f, 0f, 0.1f);

            CreateFloor("Garden Base", parent, Vector3.zero, new Vector3(3.8f, 0.15f, 6.6f), sGlassMaterial);
            CreateWall("Garden Border Left", parent, new Vector3(-1.95f, 1.1f, 0f), new Vector3(0.18f, 2.2f, 6.6f), sTrimMaterial);
            CreateWall("Garden Border Right", parent, new Vector3(1.95f, 1.1f, 0f), new Vector3(0.18f, 2.2f, 6.6f), sTrimMaterial);
            CreateWall("Garden Border Back", parent, new Vector3(0f, 1.1f, -3.25f), new Vector3(3.8f, 2.2f, 0.18f), sTrimMaterial);

            CreateRock("Rock Cluster A", parent, new Vector3(-0.7f, 0.15f, -0.9f), new Vector3(0.9f, 0.6f, 0.75f));
            CreateRock("Rock Cluster B", parent, new Vector3(0.2f, 0.15f, 0.1f), new Vector3(1.1f, 0.7f, 0.9f));
            CreateRock("Rock Cluster C", parent, new Vector3(-0.2f, 0.15f, 1.2f), new Vector3(0.85f, 0.55f, 0.65f));
            CreatePlant("Bonsai", parent, new Vector3(0.9f, 0f, -1.6f), 1.2f);
            CreatePlant("Shrub", parent, new Vector3(-1.0f, 0f, 2.0f), 0.55f);
            CreatePlant("Shrub 2", parent, new Vector3(0.8f, 0f, 2.2f), 0.45f);
        }

        private static void CreateMainHall()
        {
            var parent = new GameObject("Main Hall").transform;
            parent.position = new Vector3(0f, 0f, 5.4f);

            CreateWall("Left Divider", parent, new Vector3(-4.7f, 2.1f, 0.2f), new Vector3(0.35f, 4.2f, 8.8f), sWallMaterial);
            CreateWall("Right Divider", parent, new Vector3(4.7f, 2.1f, 0.2f), new Vector3(0.35f, 4.2f, 8.8f), sWallMaterial);
            CreateWall("North Divider", parent, new Vector3(0f, 2.1f, 4.6f), new Vector3(9.7f, 4.2f, 0.35f), sWallMaterial);

            CreateBench(parent, new Vector3(0f, 0.25f, 1.5f), new Vector3(3.4f, 0.3f, 0.8f));
            CreatePlinth(parent, new Vector3(0f, 0.55f, -1.5f), new Vector3(0.85f, 1.1f, 0.85f));
            CreateArtworkPanel(parent, "Main Hall Mural", new Vector3(0f, 1.9f, 3.95f), new Vector3(5.3f, 2.3f, 0.08f), "Assets/Storyboard/00_MasterStoryboard_FullJourney.png");
            CreateArtworkPanel(parent, "Vertical Accent", new Vector3(3.85f, 1.6f, 2.3f), new Vector3(0.9f, 2.4f, 0.08f), "Assets/Storyboard/04_RoomC_ExperimentAndExpansion.png");
        }

        private static void CreateRoomA()
        {
            var parent = new GameObject("Room A").transform;
            parent.position = new Vector3(-5.4f, 0f, 11.4f);

            CreateWall("South Wall", parent, new Vector3(0f, 2.1f, -2.6f), new Vector3(6.4f, 4.2f, 0.35f), sWallMaterial);
            CreateWall("North Wall", parent, new Vector3(0f, 2.1f, 2.6f), new Vector3(6.4f, 4.2f, 0.35f), sWallMaterial);
            CreateWall("West Wall", parent, new Vector3(-3.2f, 2.1f, 0f), new Vector3(0.35f, 4.2f, 5.5f), sWallMaterial);
            CreateWall("East Wall", parent, new Vector3(3.2f, 2.1f, 0f), new Vector3(0.35f, 4.2f, 5.5f), sWallMaterial);

            CreateBench(parent, new Vector3(0f, 0.25f, -0.2f), new Vector3(2.6f, 0.3f, 0.8f));
            CreateArtworkPanel(parent, "RoomA Center", new Vector3(0f, 2.0f, 2.35f), new Vector3(2.8f, 1.5f, 0.08f), "Assets/Storyboard/02_RoomA_BirthOfModernity.png");
            CreateArtworkPanel(parent, "RoomA Left", new Vector3(-1.9f, 1.85f, 2.35f), new Vector3(1.2f, 0.9f, 0.08f), "Assets/Storyboard/01_PrologueHall_Entry.png");
            CreateArtworkPanel(parent, "RoomA Right", new Vector3(1.95f, 1.85f, 2.35f), new Vector3(0.95f, 1.1f, 0.08f), "Assets/Storyboard/03_RoomB_CityAndEmotion.png");
        }

        private static void CreateCentralHall()
        {
            var parent = new GameObject("Central Hall").transform;
            parent.position = new Vector3(1.7f, 0f, 11.2f);

            CreateWall("North Display Wall", parent, new Vector3(0f, 2.1f, 3f), new Vector3(5.8f, 4.2f, 0.35f), sWallMaterial);
            CreateArtworkPanel(parent, "Hero Piece", new Vector3(0f, 2.0f, 2.82f), new Vector3(2.6f, 2.8f, 0.08f), "Assets/Storyboard/01_GalleryLayout_Isometric.png");
            CreateArtworkPanel(parent, "Side Left", new Vector3(-2.25f, 1.7f, 2.8f), new Vector3(0.9f, 1.2f, 0.08f), "Assets/Storyboard/01_PrologueHall_Entry.png");
            CreateArtworkPanel(parent, "Side Right", new Vector3(2.25f, 1.7f, 2.8f), new Vector3(0.9f, 1.2f, 0.08f), "Assets/Storyboard/04_RoomC_ExperimentAndExpansion.png");
            CreateBench(parent, new Vector3(0f, 0.25f, 0.2f), new Vector3(1.8f, 0.3f, 0.8f));
        }

        private static void CreateSculptureWing()
        {
            var parent = new GameObject("Sculpture Wing").transform;
            parent.position = new Vector3(7.2f, 0f, 7.5f);

            CreateWall("North Wall", parent, new Vector3(0f, 2.1f, 4.2f), new Vector3(7f, 4.2f, 0.35f), sWallMaterial);
            CreateWall("East Wall", parent, new Vector3(3.5f, 2.1f, 0f), new Vector3(0.35f, 4.2f, 8.4f), sWallMaterial);
            CreateWall("South Wall", parent, new Vector3(0f, 2.1f, -4.2f), new Vector3(7f, 4.2f, 0.35f), sWallMaterial);

            CreateArtworkPanel(parent, "Moon Screen", new Vector3(0f, 2.5f, 3.9f), new Vector3(4.4f, 2.4f, 0.08f), "Assets/Storyboard/03_RoomB_CityAndEmotion.png");
            CreateArtworkPanel(parent, "Floral Work", new Vector3(-1.6f, 1.8f, 0.8f), new Vector3(1.8f, 1.6f, 0.08f), "Assets/Storyboard/04_RoomC_ExperimentAndExpansion.png");
            CreateBench(parent, new Vector3(0f, 0.25f, 2f), new Vector3(2.0f, 0.3f, 0.8f));

            CreateStatue(parent, "Statue A", new Vector3(1.3f, 0f, 0.2f), 1.45f);
            CreateStatue(parent, "Statue B", new Vector3(2.4f, 0f, -1.4f), 1.0f);
            CreateStatue(parent, "Statue C", new Vector3(-1.2f, 0f, -1.8f), 0.85f);
            CreatePlinth(parent, new Vector3(-1.15f, 0.25f, -2.9f), new Vector3(0.7f, 0.5f, 0.7f));
        }

        private static void CreateRightGallery()
        {
            var parent = new GameObject("Right Gallery").transform;
            parent.position = new Vector3(7.5f, 0f, 1.6f);

            CreateWall("East Corridor Wall", parent, new Vector3(3.2f, 2.1f, 1.8f), new Vector3(0.35f, 4.2f, 8.8f), sWallMaterial);
            CreateWall("South Lounge Wall", parent, new Vector3(-0.6f, 2.1f, -3.0f), new Vector3(7.2f, 4.2f, 0.35f), sWallMaterial);
            CreateBench(parent, new Vector3(0f, 0.25f, -1.4f), new Vector3(2.2f, 0.3f, 0.85f));
            CreateArtworkPanel(parent, "Right Wall Art A", new Vector3(2.95f, 1.8f, 1.8f), new Vector3(0.7f, 2.8f, 0.08f), "Assets/Storyboard/01_GalleryLayout_FloorPlan2D.png");
            CreateArtworkPanel(parent, "Right Wall Art B", new Vector3(2.95f, 1.8f, -0.9f), new Vector3(0.7f, 2.8f, 0.08f), "Assets/Storyboard/00_MasterStoryboard_FullJourney.png");
            CreateArtworkPanel(parent, "Blue Piece", new Vector3(0.4f, 1.6f, 0.9f), new Vector3(1.5f, 1.1f, 0.08f), "Assets/Storyboard/05_EpilogueLounge_Exit.png");
            CreateArtworkPanel(parent, "Blue Piece 2", new Vector3(1.9f, 1.6f, -1.6f), new Vector3(1.0f, 1.5f, 0.08f), "Assets/Concept/virtual-gallery-moodboard-modern-korean-art.png");

            CreateStatue(parent, "Lounge Statue A", new Vector3(-2.2f, 0f, 1.2f), 0.85f);
            CreateStatue(parent, "Lounge Statue B", new Vector3(-0.7f, 0f, 1.6f), 0.75f);
        }

        private static void CreateLighting()
        {
            var lightsRoot = new GameObject("Lights").transform;

            CreateDirectionalLight(lightsRoot);

            CreateSpotLight("Entrance Warm Pool", lightsRoot, new Vector3(0f, 3.6f, -0.4f), new Vector3(90f, 0f, 0f), new Color(1f, 0.82f, 0.62f), 4500f, 42f, 14f);
            CreateSpotLight("Main Hall Artwork", lightsRoot, new Vector3(0f, 3.8f, 9.2f), new Vector3(85f, 180f, 0f), new Color(1f, 0.84f, 0.66f), 6500f, 34f, 14f);
            CreateSpotLight("Main Hall Center", lightsRoot, new Vector3(0f, 3.9f, 4.6f), new Vector3(90f, 0f, 0f), new Color(1f, 0.8f, 0.62f), 4200f, 48f, 14f);
            CreateSpotLight("Room A Center", lightsRoot, new Vector3(-5.4f, 3.9f, 11.2f), new Vector3(90f, 0f, 0f), new Color(1f, 0.86f, 0.7f), 4200f, 44f, 12f);
            CreateSpotLight("Central Hero", lightsRoot, new Vector3(1.7f, 4f, 14f), new Vector3(82f, 180f, 0f), new Color(1f, 0.83f, 0.63f), 6200f, 32f, 12f);
            CreateSpotLight("Moon Screen", lightsRoot, new Vector3(7.2f, 4.1f, 11.8f), new Vector3(84f, 180f, 0f), new Color(1f, 0.82f, 0.62f), 6200f, 34f, 12f);
            CreateSpotLight("Sculpture Pool", lightsRoot, new Vector3(8.1f, 3.8f, 7.2f), new Vector3(90f, 0f, 0f), new Color(1f, 0.79f, 0.58f), 4400f, 46f, 12f);
            CreateSpotLight("Lounge Pool", lightsRoot, new Vector3(7.8f, 3.7f, 0.4f), new Vector3(90f, 0f, 0f), new Color(1f, 0.8f, 0.6f), 3600f, 48f, 10f);

            CreatePointLight("Garden Glow", lightsRoot, new Vector3(-10.6f, 1.1f, 0.2f), new Color(0.45f, 0.62f, 0.70f), 2500f, 7f);
        }

        private static void CreateDirectionalLight(Transform parent)
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(35f, -28f, 0f);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.35f, 0.38f, 0.42f);
            light.intensity = 0.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;

            RenderSettings.sun = light;
        }

        private static void CreateReflectionProbe()
        {
            var probeObject = new GameObject("Reflection Probe");
            probeObject.transform.position = new Vector3(0f, 2.5f, 6f);

            var probe = probeObject.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(30f, 10f, 30f);
            probe.intensity = 0.5f;
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        }

        private static void CreateFloor(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = position;
            floor.transform.localScale = scale;
            ApplyMaterial(floor, material);
        }

        private static void CreateWall(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, material);
        }

        private static void CreateBench(Transform parent, Vector3 position, Vector3 scale)
        {
            var bench = new GameObject("Bench");
            bench.transform.SetParent(parent, false);
            bench.transform.localPosition = position;

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Top";
            top.transform.SetParent(bench.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            top.transform.localScale = scale;
            ApplyMaterial(top, sTrimMaterial);

            for (int i = -1; i <= 1; i += 2)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "Leg" + i;
                leg.transform.SetParent(bench.transform, false);
                leg.transform.localPosition = new Vector3(i * (scale.x * 0.35f), 0.05f, 0f);
                leg.transform.localScale = new Vector3(0.12f, 0.3f, scale.z * 0.8f);
                ApplyMaterial(leg, sAccentMaterial);
            }
        }

        private static void CreatePlinth(Transform parent, Vector3 position, Vector3 scale)
        {
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Plinth";
            plinth.transform.SetParent(parent, false);
            plinth.transform.localPosition = position;
            plinth.transform.localScale = scale;
            ApplyMaterial(plinth, sPlinthMaterial);
        }

        private static void CreateArtworkPanel(Transform parent, string name, Vector3 position, Vector3 scale, string texturePath)
        {
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = name + " Frame";
            frame.transform.SetParent(parent, false);
            frame.transform.localPosition = position;
            frame.transform.localScale = new Vector3(scale.x + 0.15f, scale.y + 0.15f, 0.14f);
            ApplyMaterial(frame, sFrameMaterial);

            var art = GameObject.CreatePrimitive(PrimitiveType.Cube);
            art.name = name;
            art.transform.SetParent(parent, false);
            art.transform.localPosition = position + new Vector3(0f, 0f, -0.02f);
            art.transform.localScale = scale;
            ApplyMaterial(art, GetArtworkMaterial(texturePath, name));
        }

        private static void CreateStatue(Transform parent, string name, Vector3 position, float height)
        {
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = name + " Base";
            plinth.transform.SetParent(parent, false);
            plinth.transform.localPosition = position + new Vector3(0f, 0.35f, 0f);
            plinth.transform.localScale = new Vector3(0.75f, 0.7f, 0.75f);
            ApplyMaterial(plinth, sPlinthMaterial);

            var statue = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            statue.name = name;
            statue.transform.SetParent(parent, false);
            statue.transform.localPosition = position + new Vector3(0f, 0.7f + (height * 0.5f), 0f);
            statue.transform.localScale = new Vector3(0.42f, height * 0.5f, 0.42f);
            ApplyMaterial(statue, sAccentMaterial);
        }

        private static void CreatePlant(string name, Transform parent, Vector3 position, float scale)
        {
            var planter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            planter.name = name + " Pot";
            planter.transform.SetParent(parent, false);
            planter.transform.localPosition = position + new Vector3(0f, 0.22f, 0f);
            planter.transform.localScale = new Vector3(0.55f * scale, 0.44f * scale, 0.55f * scale);
            ApplyMaterial(planter, sTrimMaterial);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = name + " Trunk";
            trunk.transform.SetParent(parent, false);
            trunk.transform.localPosition = position + new Vector3(0f, 0.7f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.08f * scale, 0.5f * scale, 0.08f * scale);
            ApplyMaterial(trunk, sAccentMaterial);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = name + " Canopy";
            canopy.transform.SetParent(parent, false);
            canopy.transform.localPosition = position + new Vector3(0f, 1.35f * scale, 0f);
            canopy.transform.localScale = new Vector3(0.95f, 0.8f, 0.95f) * scale;
            ApplyMaterial(canopy, CreateOrLoadMaterial("PlantCanopy", new Color(0.29f, 0.34f, 0.21f), 0f, 0.65f));
        }

        private static void CreateRock(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = name;
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = position;
            rock.transform.localScale = scale;
            ApplyMaterial(rock, CreateOrLoadMaterial("Rock", new Color(0.36f, 0.36f, 0.36f), 0f, 0.95f));
        }

        private static void CreateSpotLight(string name, Transform parent, Vector3 position, Vector3 rotation, Color color, float intensity, float spotAngle, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.Euler(rotation);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.innerSpotAngle = spotAngle * 0.68f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            light.cookieSize = 4f;
        }

        private static void CreatePointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateMaterials()
        {
            sWallMaterial = CreateOrLoadMaterial("GalleryWall", new Color(0.84f, 0.81f, 0.76f), 0f, 0.88f);
            sTrimMaterial = CreateOrLoadMaterial("GalleryTrim", new Color(0.23f, 0.22f, 0.21f), 0f, 0.5f);
            sFloorMaterial = CreateOrLoadMaterial("GalleryFloor", new Color(0.34f, 0.31f, 0.28f), 0f, 0.72f);
            sAccentMaterial = CreateOrLoadMaterial("GalleryAccent", new Color(0.55f, 0.46f, 0.32f), 0.15f, 0.45f);
            sFrameMaterial = CreateOrLoadMaterial("GalleryFrame", new Color(0.25f, 0.21f, 0.16f), 0.2f, 0.4f);
            sPlinthMaterial = CreateOrLoadMaterial("GalleryPlinth", new Color(0.58f, 0.54f, 0.49f), 0f, 0.84f);
            sGlassMaterial = CreateOrLoadMaterial("GalleryWaterStone", new Color(0.16f, 0.26f, 0.28f), 0.05f, 0.08f);
        }

        private static Material CreateOrLoadMaterial(string materialName, Color baseColor, float metallic, float smoothness)
        {
            var path = MaterialsFolder + "/" + materialName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = baseColor;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetArtworkMaterial(string texturePath, string artName)
        {
            var fileName = Path.GetFileNameWithoutExtension(texturePath);
            var path = MaterialsFolder + "/" + fileName + "_" + SanitizeName(artName) + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
            }

            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.22f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string SanitizeName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(" ", "_");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            var name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
