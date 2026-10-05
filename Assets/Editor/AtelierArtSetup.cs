using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    /// <summary>Authors the shared toy-workshop art without changing the physics assemblies.</summary>
    public static class AtelierArtSetup
    {
        private const string Art = "Assets/Art";
        private static Font regular;
        private static Font semibold;
        private static Sprite panel;
        private static Cubemap studio;
        private static VolumeProfile grading;

        [MenuItem("Karakuri Labo/Apply Atelier Art Direction")]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Exit Play Mode before updating the art.");
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                {
                    throw new InvalidOperationException("Save open scenes before updating the art.");
                }
            }
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                CreateAssets();
                ConfigurePipeline("Mobile");
                ConfigurePipeline("PC");
                foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path))
                {
                    GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        string physics = PhysicsSignature(prefab);
                        StyleMeshes(prefab);
                        StylePart(prefab);
                        if (PhysicsSignature(prefab) != physics)
                        {
                            throw new InvalidOperationException($"Art changed physics in {path}.");
                        }
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefab);
                    }
                }
                foreach (string name in new[] { "Main", "StageOne", "StageTwo", "StageThree", "StageFour", "StageFive",
                    "StageSpring", "StageCurve", "StageFunnel", "StageLift", "StageFan" })
                {
                    if (!File.Exists($"Assets/Scenes/{name}.unity"))
                    {
                        continue;
                    }
                    Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity", OpenSceneMode.Single);
                    string physics = string.Join("\n", scene.GetRootGameObjects().Select(PhysicsSignature).Where(value => value.Length > 0));
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        StyleMeshes(root);
                    }
                    var parts = Object.FindObjectsByType<DraggableObject>(FindObjectsInactive.Include).Select(p => p.gameObject)
                        .Concat(Object.FindObjectsByType<BallController>(FindObjectsInactive.Include).Select(p => p.gameObject))
                        .Concat(Object.FindObjectsByType<GoalController>(FindObjectsInactive.Include).Select(p => p.gameObject))
                        .Concat(Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include)
                            .Where(p => p.name == "Rolling surface").Select(p => p.transform.parent.gameObject)).Distinct().ToArray();
                    foreach (GameObject part in parts)
                    {
                        StylePart(part);
                    }
                    StyleStage();
                    StyleLighting();
                    StyleHud();
                    JapaneseSceneSetup.Configure(GameObject.Find("HUD").transform);
                    string after = string.Join("\n", scene.GetRootGameObjects().Select(PhysicsSignature).Where(value => value.Length > 0));
                    if (after != physics)
                    {
                        throw new InvalidOperationException($"Art changed physics in {name}.");
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        throw new InvalidOperationException($"Could not save {name}.");
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(original);
            }
            Debug.Log("Atelier art applied: rounded shapes, oak, enamel, brass, studio light and a new HUD.");
        }

        private static string PhysicsSignature(GameObject root)
        {
            return string.Join("\n", root.GetComponentsInChildren<Component>(true)
                .Where(c => c is Collider || c is Rigidbody || c is Joint)
                .Select(c => c.GetType().Name + ":" + c.name + ":" + c.transform.localPosition + ":" +
                    c.transform.localRotation + ":" + c.transform.localScale + ":" + EditorJsonUtility.ToJson(c)));
        }

        private static void CreateAssets()
        {
            foreach (string folder in new[] { Art, Art + "/Meshes", Art + "/Textures", Art + "/Materials", Art + "/Lighting" })
            {
                Directory.CreateDirectory(folder);
            }
            AssetDatabase.Refresh();
            regular = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Fonts/Barlow-Regular.ttf");
            semibold = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Fonts/Barlow-SemiBold.ttf");
            if (regular == null || semibold == null)
            {
                throw new InvalidOperationException("Import the bundled Barlow fonts before applying the art.");
            }
            Material("Ball_Coral", "EC614B", 0.08f, 0.82f);
            Material("Ball_Stripe", "FFF3D6", 0.08f, 0.6f);
            Material("Ramp_Blue", "348D90", 0.12f, 0.45f);
            Material("Ramp_Support", "254D54", 0.16f, 0.38f);
            Material("Seesaw_Violet", "DEA74E", 0.1f, 0.48f);
            Material("Seesaw_Base", "254D54", 0.16f, 0.42f);
            Material("Domino_Amber", "F6E4BA", 0, 0.44f);
            Material("Goal_Mint", "68ACA1", 0.12f, 0.38f);
            Material("Goal_Trim", "277B73", 0.2f, 0.48f);
            Material("Stage_Cream", "EBDAC0", 0, 0.24f);
            Material("Backdrop_Lilac", "E4DED2", 0, 0.12f);
            Material("Brass", "D2A65E", 0.72f, 0.68f, true);
            Material("Ink", "28474D", 0, 0.26f, true);
            Material("Oak", "FFFFFF", 0, 0.32f, true).SetTexture("_BaseMap", Wood());
            Material("Felt", "7DABA5", 0, 0.06f, true);
            Material("Porcelain", "FCF3E2", 0.05f, 0.48f, true);
            Material selected = LoadMaterial("Selection_Gold");
            selected.SetColor("_BaseColor", Hex("F3B95E"));
            EditorUtility.SetDirty(selected);
            panel = CreatePanel();
            studio = CreateStudio();
            grading = CreateGrading();
        }

        private static Material Material(string name, string color, float metal, float smooth, bool art = false)
        {
            string path = art ? $"{Art}/Materials/{name}.mat" : $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", Hex(color));
            material.SetFloat("_Metallic", metal);
            material.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadMaterial(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{Art}/Materials/{name}.mat") ??
            AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{name}.mat");

        private static Texture2D Wood()
        {
            string path = Art + "/Textures/MapleGrain.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(512, 512, TextureFormat.RGBA32, true);
                var colors = new Color[512 * 512];
                Color light = Hex("EBCB9D");
                Color dark = Hex("B98651");
                for (int y = 0; y < 512; y++)
                {
                    for (int x = 0; x < 512; x++)
                    {
                        float u = x / 512f;
                        float v = y / 512f;
                        float wave = Mathf.Sin(v * Mathf.PI * 90 + Mathf.Sin(u * Mathf.PI * 2) * 2.1f +
                            Mathf.Sin(u * Mathf.PI * 6 + v * Mathf.PI * 4) * 0.6f);
                        float fine = Mathf.Sin(v * Mathf.PI * 300 + Mathf.Sin(u * Mathf.PI * 4) * 1.3f);
                        float amount = 0.13f + Mathf.Pow(wave * 0.5f + 0.5f, 12) * 0.22f + fine * 0.025f;
                        colors[y * 512 + x] = Color.Lerp(light, dark, amount);
                    }
                }
                texture.SetPixels(colors);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Sprite CreatePanel()
        {
            string path = Art + "/Textures/RoundedPanel.png";
            const int side = 96;
            const float radius = 24;
            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false);
            var pixels = new Color[side * side];
            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    Vector2 delta = new Vector2(Mathf.Max(0, Mathf.Abs(x + 0.5f - side * 0.5f) - (side * 0.5f - radius)),
                        Mathf.Max(0, Mathf.Abs(y + 0.5f - side * 0.5f) - (side * 0.5f - radius)));
                    pixels[y * side + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - delta.magnitude + 0.5f));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = Vector4.one * radius;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Cubemap CreateStudio()
        {
            string path = Art + "/Lighting/StudioReflection.cubemap";
            Cubemap existing = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (existing != null)
            {
                return existing;
            }
            const int side = 128;
            var cube = new Cubemap(side, TextureFormat.RGBAHalf, true) { name = "StudioReflection" };
            for (int face = 0; face < 6; face++)
            {
                var pixels = new Color[side * side];
                for (int y = 0; y < side; y++)
                {
                    for (int x = 0; x < side; x++)
                    {
                        float u = (x + 0.5f) / side * 2 - 1;
                        float v = (y + 0.5f) / side * 2 - 1;
                        Vector3 direction;
                        switch ((CubemapFace)face)
                        {
                            case CubemapFace.PositiveX: direction = new Vector3(1, -v, -u); break;
                            case CubemapFace.NegativeX: direction = new Vector3(-1, -v, u); break;
                            case CubemapFace.PositiveY: direction = new Vector3(u, 1, v); break;
                            case CubemapFace.NegativeY: direction = new Vector3(u, -1, -v); break;
                            case CubemapFace.PositiveZ: direction = new Vector3(u, -v, 1); break;
                            default: direction = new Vector3(-u, -v, -1); break;
                        }
                        direction.Normalize();
                        Color ambient = Color.Lerp(new Color(0.15f, 0.19f, 0.2f), new Color(0.8f, 0.87f, 0.91f),
                            direction.y * 0.5f + 0.5f);
                        float key = Mathf.Pow(Mathf.Max(0, Vector3.Dot(direction, new Vector3(-0.5f, 0.75f, -0.45f).normalized)), 32) * 4;
                        float rim = Mathf.Pow(Mathf.Max(0, Vector3.Dot(direction, new Vector3(0.6f, 0.5f, 0.7f).normalized)), 64) * 2;
                        pixels[y * side + x] = ambient + new Color(1f, 0.91f, 0.75f) * key + Color.white * rim;
                    }
                }
                cube.SetPixels(pixels, (CubemapFace)face);
            }
            cube.Apply(true, false);
            AssetDatabase.CreateAsset(cube, path);
            return cube;
        }

        private static VolumeProfile CreateGrading()
        {
            string path = Art + "/Lighting/AtelierGrading.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "AtelierGrading";
                AssetDatabase.CreateAsset(profile, path);
            }
            Tonemapping tone = GetVolume<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);
            ColorAdjustments color = GetVolume<ColorAdjustments>(profile);
            color.postExposure.Override(0.1f);
            color.contrast.Override(6);
            color.saturation.Override(3);
            Bloom bloom = GetVolume<Bloom>(profile);
            bloom.intensity.Override(0.08f);
            bloom.threshold.Override(1.2f);
            bloom.scatter.Override(0.6f);
            Vignette vignette = GetVolume<Vignette>(profile);
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.65f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetVolume<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>();
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(component);
            return component;
        }

        private static void ConfigurePipeline(string name)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>($"Assets/Settings/{name}_RPAsset.asset");
            pipeline.renderScale = 1;
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = true;
            pipeline.shadowDistance = 35;
            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serialized.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
            serialized.FindProperty("m_ShadowCascadeCount").intValue = 2;
            serialized.FindProperty("m_ShadowDepthBias").floatValue = 0.35f;
            serialized.FindProperty("m_ShadowNormalBias").floatValue = 0.3f;
            serialized.FindProperty("m_SoftShadowQuality").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>($"Assets/Settings/{name}_Renderer.asset");
            ScreenSpaceAmbientOcclusion ao = renderer.rendererFeatures.OfType<ScreenSpaceAmbientOcclusion>().FirstOrDefault();
            if (ao == null)
            {
                ao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ao.name = "Atelier Contact Shadows";
                AssetDatabase.AddObjectToAsset(ao, renderer);
                renderer.rendererFeatures.Add(ao);
            }
            var settings = new SerializedObject(ao);
            settings.FindProperty("m_Settings.Intensity").floatValue = 0.7f;
            settings.FindProperty("m_Settings.Radius").floatValue = 0.18f;
            settings.FindProperty("m_Settings.DirectLightingStrength").floatValue = 0.12f;
            settings.FindProperty("m_Settings.Downsample").boolValue = true;
            settings.FindProperty("m_Settings.Source").intValue = 1;
            settings.FindProperty("m_Settings.Samples").intValue = 1;
            settings.FindProperty("m_Settings.BlurQuality").intValue = 1;
            settings.FindProperty("m_Settings.AOMethod").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            ao.SetActive(true);
            ao.Create();
            renderer.SetDirty();
            EditorUtility.SetDirty(ao);
            EditorUtility.SetDirty(renderer);
        }

        private static void StyleMeshes(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null && filter.name != "Backdrop" &&
                    (filter.sharedMesh.name == "Cube" || filter.sharedMesh.name.StartsWith("AtelierBox_")))
                {
                    filter.sharedMesh = AtelierGeometry.Box(filter.transform.lossyScale);
                    Record(filter);
                }
            }
            foreach (LineRenderer line in root.GetComponentsInChildren<LineRenderer>(true))
            {
                if (line.name == "SelectionOutline")
                {
                    line.widthMultiplier = 0.028f;
                    line.numCornerVertices = 6;
                    line.numCapVertices = 6;
                    Record(line);
                }
            }
        }

        private static void StylePart(GameObject part)
        {
            if (part.GetComponent<BallController>() != null)
            {
                part.GetComponent<MeshFilter>().sharedMesh = AtelierGeometry.SphereBand("AtelierSphere", -Mathf.PI / 2, Mathf.PI / 2, 0.5f, 32);
                Record(part.GetComponent<MeshFilter>());
                Transform equator = part.transform.Find("Equator");
                equator.localRotation = Quaternion.Euler(25, 0, 32);
                equator.localScale = Vector3.one;
                equator.GetComponent<MeshFilter>().sharedMesh = AtelierGeometry.SphereBand("AtelierIvoryBand", -0.065f, 0.065f, 0.502f, 4);
                Record(equator);
                Record(equator.GetComponent<MeshFilter>());
                MeshPart(part.transform, "Atelier Gold Band A", AtelierGeometry.SphereBand("AtelierGoldBandA", -0.088f, -0.065f, 0.503f, 2),
                    Vector3.zero, Vector3.one, equator.localRotation, LoadMaterial("Brass"));
                MeshPart(part.transform, "Atelier Gold Band B", AtelierGeometry.SphereBand("AtelierGoldBandB", 0.065f, 0.088f, 0.503f, 2),
                    Vector3.zero, Vector3.one, equator.localRotation, LoadMaterial("Brass"));
            }
            Transform surface = part.transform.Find("Rolling surface");
            if (surface != null)
            {
                Transform details = Child(part.transform, "Atelier Details");
                float length = surface.localScale.x;
                Transform rail = part.transform.Find("Front rail");
                float railTop = rail != null ? rail.localPosition.y + rail.localScale.y * 0.5f + 0.004f : 0.339f;
                for (int side = 0; side < 2; side++)
                {
                    for (int end = 0; end < 2; end++)
                    {
                        Disc(details, $"Fastener {side} {end}", new Vector3((end == 0 ? -1 : 1) * (length * 0.5f - 0.18f),
                            railTop, side == 0 ? -0.81f : 0.81f), 0.04f, 0.009f, LoadMaterial("Brass"));
                    }
                }
                for (int index = 0; index < 3; index++)
                {
                    float x = Mathf.Lerp(-length * 0.22f, length * 0.22f, index / 2f);
                    for (int wing = 0; wing < 2; wing++)
                    {
                        Box(details, $"Direction {index} {wing}", new Vector3(x, 0.122f, wing == 0 ? -0.055f : 0.055f),
                            new Vector3(0.16f, 0.003f, 0.024f), LoadMaterial("Ball_Stripe"), Quaternion.Euler(0, wing == 0 ? -40 : 40, 0));
                    }
                }
                foreach (Transform support in part.transform.Cast<Transform>().Where(t => t.name == "Ramp support"))
                {
                    support.GetComponent<Renderer>().sharedMaterial = LoadMaterial("Oak");
                    Record(support.GetComponent<Renderer>());
                }
            }
            Transform board = part.transform.Find("Board");
            if (board != null && board.GetComponent<HingeJoint>() != null)
            {
                SetMaterial(board.Find("Deck"), "Oak");
                SetMaterial(board.Find("Center mark"), "Brass");
                SetMaterial(part.transform.Find("Axle"), "Brass");
                foreach (Transform support in part.transform.Cast<Transform>().Where(t => t.name == "Pivot support"))
                {
                    SetMaterial(support, "Oak");
                }
                Transform details = Child(board, "Atelier Details");
                for (int end = 0; end < 2; end++)
                {
                    for (int side = 0; side < 2; side++)
                    {
                        Disc(details, $"Deck pin {end} {side}", new Vector3(end == 0 ? -0.88f : 0.88f, 0.082f,
                            side == 0 ? -0.45f : 0.45f), 0.035f, 0.004f, LoadMaterial("Brass"));
                    }
                    Box(details, $"Painted end {end}", new Vector3(end == 0 ? -0.99f : 0.99f, 0.081f, 0),
                        new Vector3(0.1f, 0.003f, 1.04f), LoadMaterial("Ramp_Blue"));
                }
                Transform baseDetails = Child(part.transform, "Atelier Details");
                for (int side = 0; side < 2; side++)
                {
                    Transform cap = Disc(baseDetails, $"Axle cap {side}", new Vector3(0, 0.65f, side == 0 ? -0.75f : 0.75f),
                        0.15f, 0.027f, LoadMaterial("Brass"));
                    cap.localRotation = Quaternion.Euler(90, 0, 0);
                    MeshPart(baseDetails, $"Bearing ring {side}", AtelierGeometry.Ring(0.095f, 0.109f),
                        new Vector3(0, 0.65f, side == 0 ? -0.765f : 0.765f), Vector3.one,
                        Quaternion.Euler(side == 0 ? -90 : 90, 0, 0), LoadMaterial("Ink"));
                    Box(baseDetails, $"Axle slot {side}", new Vector3(0, 0.65f, side == 0 ? -0.765f : 0.765f),
                        new Vector3(0.073f, 0.012f, 0.003f), LoadMaterial("Ink"));
                }
            }
            if (part.GetComponent<GoalController>() != null)
            {
                Transform details = Child(part.transform, "Atelier Details");
                Slab(details, "Porcelain landing", new Vector3(0, 0.202f, 0), new Vector3(2.55f, 0.006f, 1.65f), LoadMaterial("Porcelain"));
                MeshPart(details, "Finish ring", AtelierGeometry.Ring(0.31f, 0.34f), new Vector3(0.35f, 0.207f, 0),
                    Vector3.one, Quaternion.identity, LoadMaterial("Brass"));
                MeshPart(details, "Finish center", AtelierGeometry.Ring(0, 0.08f), new Vector3(0.35f, 0.208f, 0),
                    Vector3.one, Quaternion.identity, LoadMaterial("Goal_Trim"));
                Box(details, "Front brass lip", new Vector3(0, 0.601f, -1), new Vector3(2.56f, 0.012f, 0.08f), LoadMaterial("Brass"));
                Box(details, "Back brass lip", new Vector3(0, 0.601f, 1), new Vector3(2.56f, 0.012f, 0.08f), LoadMaterial("Brass"));
            }
        }

        private static void StyleStage()
        {
            Transform stage = GameObject.Find("Stage").transform;
            foreach (Transform box in stage)
            {
                if (box.name != "Tabletop" && box.name != "StartIsland" && box.name != "GoalIsland" && box.name != "ValleyFloor" && !box.name.EndsWith("Tower"))
                {
                    continue;
                }
                bool valley = box.name == "ValleyFloor";
                SetMaterial(box, "Oak");
                Vector3 size = box.lossyScale;
                Transform details = Child(box, "Atelier Details");
                details.localScale = new Vector3(1 / size.x, 1 / size.y, 1 / size.z);
                Slab(details, "Inset surface", new Vector3(0, size.y * 0.5f + 0.002f, 0),
                    new Vector3(size.x - 0.38f, 0.008f, size.z - 0.38f), LoadMaterial(valley ? "Felt" : "Porcelain"));
                Box(details, "Lower plinth", new Vector3(0, -size.y * 0.5f + 0.07f, 0),
                    new Vector3(size.x + 0.025f, 0.11f, size.z + 0.025f), LoadMaterial("Ramp_Support"));
                if (!valley)
                {
                    Box(details, "Maker badge", new Vector3(size.x * 0.5f - 0.52f, -size.y * 0.15f, -size.z * 0.5f - 0.005f),
                        new Vector3(0.64f, 0.23f, 0.012f), LoadMaterial("Brass"));
                    for (int side = 0; side < 2; side++)
                    {
                        Box(details, $"Corner join {side}", new Vector3(side == 0 ? -size.x * 0.5f + 0.12f : size.x * 0.5f - 0.12f,
                            0, -size.z * 0.5f - 0.002f), new Vector3(0.042f, size.y * 0.64f, 0.004f), LoadMaterial("Brass"));
                    }
                }
            }
        }

        private static void StyleLighting()
        {
            GameObject rig = GameObject.Find("Atelier Lighting") ?? new GameObject("Atelier Lighting");
            Volume volume = GetOrAdd<Volume>(rig);
            volume.isGlobal = true;
            volume.priority = 10;
            volume.sharedProfile = grading;
            Light sun = GameObject.Find("Sun").GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(52, -35, 0);
            sun.color = Hex("FFF0D6");
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.25f;
            Light fill = GetOrAdd<Light>(Child(rig.transform, "Cool fill").gameObject);
            fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(35, 145, 0);
            fill.color = Hex("C7E6ED");
            fill.intensity = 0.42f;
            fill.shadows = LightShadows.None;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.72f, 0.77f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.34f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.18f, 0.14f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = studio;
            RenderSettings.reflectionIntensity = 0.8f;
            Camera camera = Camera.main;
            camera.backgroundColor = Hex("E4DED2");
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        private static void StyleHud()
        {
            GameObject hud = GameObject.Find("HUD");
            Transform safe = hud.transform.Find("SafeArea");
            foreach (Text text in hud.GetComponentsInChildren<Text>(true))
            {
                text.font = text.name == "Title" || text.transform.parent.GetComponent<Button>() != null ? semibold : regular;
                text.fontStyle = FontStyle.Normal;
                text.color = Hex("29484E");
                if (text.name == "ClearText")
                {
                    text.font = semibold;
                    text.fontSize = 78;
                    text.color = Hex("FCF5E7");
                    Outline outline = GetOrAdd<Outline>(text.gameObject);
                    outline.effectColor = Hex("28585B");
                    outline.effectDistance = new Vector2(2, -2);
                }
            }
            Text title = safe.Find("Title").GetComponent<Text>();
            title.text = "KARAKURI";
            title.fontSize = 30;
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchoredPosition = new Vector2(82, -28);
            Text stageLabel = safe.Find("StageLabel").GetComponent<Text>();
            stageLabel.color = Hex("657D7D");
            stageLabel.fontSize = 15;
            stageLabel.rectTransform.anchoredPosition = new Vector2(83, -69);
            Image logo = GetOrAdd<Image>(Child(safe, "Atelier Mark").gameObject);
            logo.sprite = panel;
            logo.type = Image.Type.Sliced;
            logo.color = Hex("E97556");
            logo.raycastTarget = false;
            RectTransform mark = logo.rectTransform;
            mark.anchorMin = mark.anchorMax = mark.pivot = new Vector2(0, 1);
            mark.anchoredPosition = new Vector2(32, -34);
            mark.sizeDelta = new Vector2(36, 36);
            Text monogram = GetOrAdd<Text>(Child(mark, "K").gameObject);
            monogram.font = semibold;
            monogram.text = "K";
            monogram.fontSize = 26;
            monogram.color = Hex("FFF4DC");
            monogram.alignment = TextAnchor.MiddleCenter;
            monogram.raycastTarget = false;
            monogram.rectTransform.anchorMin = Vector2.zero;
            monogram.rectTransform.anchorMax = Vector2.one;
            monogram.rectTransform.offsetMin = monogram.rectTransform.offsetMax = Vector2.zero;
            foreach (Button button in hud.GetComponentsInChildren<Button>(true))
            {
                if (button.transform.parent.name == "PartToolbar")
                {
                    LayoutElement layout = button.GetComponent<LayoutElement>();
                    if (layout != null)
                    {
                        layout.minHeight = 80;
                        layout.preferredHeight = 80;
                    }
                }
                Image image = button.GetComponent<Image>();
                image.sprite = panel;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1.5f;
                bool primary = button.name == "SimulationButton" || button.name == "NextButton";
                string color = button.name.Contains("High") || button.name == "AddRampButton" ? "C1DAD4" :
                    button.name.Contains("Low") || button.name == "AddSeesawButton" ? "F0D7A5" :
                    button.name == "AddDominoButton" ? "EFDEC4" :
                    button.name == "DeleteButton" ? "F0D9CE" : primary ? "D9694F" : "F9F2E4";
                image.color = Hex(color);
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1);
                colors.pressedColor = new Color(0.84f, 0.86f, 0.84f, 1);
                colors.disabledColor = new Color(0.8f, 0.82f, 0.8f, 0.58f);
                colors.fadeDuration = 0.1f;
                button.colors = colors;
                Text label = button.GetComponentInChildren<Text>(true);
                label.font = semibold;
                label.fontStyle = FontStyle.Bold;
                label.fontSize = button.name == "SimulationButton" || button.name == "NextButton" ? 27 : 20;
                label.color = Hex(primary ? "FFF7E8" : "29484E");
                Shadow shadow = GetOrAdd<Shadow>(button.gameObject);
                shadow.effectColor = new Color(0.16f, 0.23f, 0.22f, 0.1f);
                shadow.effectDistance = new Vector2(0, -3);
                shadow.useGraphicAlpha = true;
            }
            Image dock = safe.Find("PartToolbar").GetComponent<Image>();
            dock.rectTransform.sizeDelta = new Vector2(-48, 80);
            dock.rectTransform.anchoredPosition = new Vector2(0, 24);
            dock.sprite = panel;
            dock.type = Image.Type.Sliced;
            dock.color = new Color(0.98f, 0.96f, 0.9f, 0.25f);
            Text hint = safe.Find("GoalHint").GetComponent<Text>();
            hint.fontSize = 17;
            hint.color = Hex("3C6265");
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = Vector2.zero;
            hint.rectTransform.anchoredPosition = new Vector2(38, 115);
            hint.rectTransform.sizeDelta = new Vector2(730, 32);
            Transform backdrop = Child(safe, "HintBackdrop");
            Image hintPanel = GetOrAdd<Image>(backdrop.gameObject);
            hintPanel.sprite = panel;
            hintPanel.type = Image.Type.Sliced;
            hintPanel.pixelsPerUnitMultiplier = 2;
            hintPanel.color = new Color(0.98f, 0.96f, 0.9f, 0.87f);
            hintPanel.raycastTarget = false;
            hintPanel.rectTransform.anchorMin = hintPanel.rectTransform.anchorMax = hintPanel.rectTransform.pivot = Vector2.zero;
            hintPanel.rectTransform.anchoredPosition = new Vector2(24, 115);
            hintPanel.rectTransform.sizeDelta = new Vector2(756, 32);
            backdrop.SetAsLastSibling();
            backdrop.SetSiblingIndex(hint.transform.GetSiblingIndex());
            RectTransform sound = safe.Find("SoundButton").GetComponent<RectTransform>();
            sound.sizeDelta = new Vector2(176, 56);
            RectTransform view = safe.Find("ResetViewButton").GetComponent<RectTransform>();
            view.sizeDelta = new Vector2(176, 56);
            view.anchoredPosition = new Vector2(-24, -164);
            safe.Find("ModeLabel").GetComponent<Text>().fontSize = 16;
            Text cameraHint = safe.Find("CameraHint").GetComponent<Text>();
            cameraHint.text = "1 FINGER: ROTATE\n2 FINGERS: PAN / ZOOM";
            cameraHint.rectTransform.sizeDelta = new Vector2(220, 52);
            cameraHint.fontSize = 14;
            cameraHint.color = Hex("657D7D");
            cameraHint.rectTransform.anchoredPosition = new Vector2(-24, hud.GetComponent<StageViewNavigator>() != null ? -300 : -228);
            if (hud.GetComponent<StageViewNavigator>() != null)
            {
                cameraHint.text += "\nTAP AREAS TO EXPLORE";
                cameraHint.rectTransform.sizeDelta = new Vector2(220, 72);
                foreach (Button button in hud.GetComponentsInChildren<Button>(true))
                {
                    if (button.name.StartsWith("AreaView") || button.name.StartsWith("Add"))
                    {
                        button.GetComponentInChildren<Text>().fontSize = 18;
                    }
                }
            }
            hud.GetComponent<SimulationHUD>().ConfigureColors(Hex("D9694F"), Hex("315E63"));
            LayoutRebuilder.ForceRebuildLayoutImmediate(safe.GetComponent<RectTransform>());
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }
            GameObject child = parent is RectTransform ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Transform MeshPart(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale,
            Quaternion rotation, Material material)
        {
            Transform part = Child(parent, name);
            part.localPosition = position;
            part.localRotation = rotation;
            part.localScale = scale;
            GetOrAdd<MeshFilter>(part.gameObject).sharedMesh = mesh;
            GetOrAdd<MeshRenderer>(part.gameObject).sharedMaterial = material;
            Record(part);
            Record(part.GetComponent<MeshFilter>());
            Record(part.GetComponent<MeshRenderer>());
            return part;
        }

        private static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            Quaternion rotation = default)
        {
            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }
            return MeshPart(parent, name, AtelierGeometry.Box(size), position, size, rotation, material);
        }

        private static Transform Disc(Transform parent, string name, Vector3 position, float radius, float height, Material material)
        {
            Mesh mesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            return MeshPart(parent, name, mesh, position, new Vector3(radius * 2, height * 0.5f, radius * 2), Quaternion.identity, material);
        }

        private static Transform Slab(Transform parent, string name, Vector3 position, Vector3 size, Material material) =>
            MeshPart(parent, name, AtelierGeometry.Box(size, 0.13f), position, size, Quaternion.identity, material);

        private static void SetMaterial(Transform target, string material)
        {
            target.GetComponent<Renderer>().sharedMaterial = LoadMaterial(material);
            Record(target.GetComponent<Renderer>());
        }

        private static void Record(Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }
    }
}
