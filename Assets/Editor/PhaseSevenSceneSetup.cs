using System;
using System.Collections.Generic;
using System.IO;
using KarakuriLabo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KarakuriLabo.Editor
{
    public static class PhaseSevenSceneSetup
    {
        private const string ChimePath = "Assets/Audio/ClearChime.wav";
        private const string MaterialPath = "Assets/Materials/Clear_Confetti.mat";

        [MenuItem("Karakuri Labo/Set Up Phase 7 Clear Effects")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GameObject hud = GameObject.Find("HUD");
            Transform safeArea = hud != null ? hud.transform.Find("SafeArea") : null;
            GameManager manager = Object.FindAnyObjectByType<GameManager>();
            GoalController goal = Object.FindAnyObjectByType<GoalController>();
            Transform clear = safeArea != null ? safeArea.Find("ClearText") : null;
            if (Application.isPlaying || scene.path != "Assets/Scenes/Main.unity" ||
                manager == null || goal == null || clear == null ||
                safeArea.Find("PartToolbar") == null)
            {
                throw new InvalidOperationException("Open the Phase 6 Main scene in Edit Mode first.");
            }

            ParticleSystem confetti = ConfigureConfetti(goal.transform);
            AudioClip chime = ClearChime();
            AudioManager audio = Object.FindAnyObjectByType<AudioManager>();
            if (audio == null)
            {
                audio = new GameObject("AudioManager").AddComponent<AudioManager>();
            }
            AudioSource source = GetOrAdd<AudioSource>(audio.gameObject);
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.volume = 0.35f;
            source.dopplerLevel = 0;
            source.clip = chime;

            Button sound = ConfigureSoundButton(safeArea);
            audio.Configure(source, chime, sound, sound.transform.Find("Label").GetComponent<Text>());
            ClearCelebration celebration = GetOrAdd<ClearCelebration>(hud);
            celebration.Configure(manager, clear.GetComponent<Text>(), confetti, audio);
            EditorUtility.SetDirty(audio);
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(celebration);
            EditorUtility.SetDirty(confetti);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 7 ready: CLEAR celebrates with confetti, a text animation and an original chime. SOUND toggles audio.");
        }

        private static ParticleSystem ConfigureConfetti(Transform goal)
        {
            Transform existing = goal.Find("ClearEffects");
            GameObject effects = existing != null ? existing.gameObject : new GameObject("ClearEffects");
            effects.transform.SetParent(goal, false);
            effects.transform.localPosition = new Vector3(0, 0.55f, 0);
            effects.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            effects.transform.localScale = Vector3.one;
            ParticleSystem particles = GetOrAdd<ParticleSystem>(effects);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.prewarm = false;
            main.duration = 0.15f;
            main.startDelay = 0;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.8f, 5.5f);
            main.gravityModifier = 0.7f;
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.stopAction = ParticleSystemStopAction.None;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.09f, 0.15f);
            main.startSizeY = 0.06f;
            main.startSizeZ = 0.02f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startRotationY = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            var palette = new Gradient
            {
                mode = GradientMode.Fixed,
                colorKeys = new[]
                {
                    new GradientColorKey(new Color32(255, 202, 82, 255), 0),
                    new GradientColorKey(new Color32(255, 202, 82, 255), 0.25f),
                    new GradientColorKey(new Color32(255, 112, 92, 255), 0.5f),
                    new GradientColorKey(new Color32(128, 205, 178, 255), 0.75f),
                    new GradientColorKey(new Color32(172, 139, 212, 255), 1)
                },
                alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
            };
            main.startColor = new ParticleSystem.MinMaxGradient(palette)
            {
                mode = ParticleSystemGradientMode.RandomColor
            };
            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 80) });
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 26;
            shape.radius = 0.3f;
            shape.radiusThickness = 1;
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-7, 7);
            rotation.y = new ParticleSystem.MinMaxCurve(-5, 5);
            rotation.z = new ParticleSystem.MinMaxCurve(-9, 9);
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1)
                }
            });
            var collision = particles.collision;
            collision.enabled = false;
            var trigger = particles.trigger;
            trigger.enabled = false;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = CubeMesh();
            renderer.sharedMaterial = ConfettiMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enableGPUInstancing = false;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV
            });
            return particles;
        }

        private static Mesh CubeMesh()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                return cube.GetComponent<MeshFilter>().sharedMesh;
            }
            finally
            {
                Object.DestroyImmediate(cube);
            }
        }

        private static Material ConfettiMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                return existing;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                throw new InvalidOperationException("The URP Particles/Unlit shader is required for clear effects.");
            }
            var material = new Material(shader) { name = "Clear_Confetti" };
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_ColorMode", 0);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static AudioClip ClearChime()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }
            if (!File.Exists(ChimePath))
            {
                WriteChime();
                AssetDatabase.ImportAsset(ChimePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (AudioImporter)AssetImporter.GetAtPath(ChimePath);
                importer.forceToMono = true;
                importer.loadInBackground = false;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ChimePath);
            if (clip == null)
            {
                throw new InvalidOperationException("The clear chime could not be imported.");
            }
            return clip;
        }

        private static void WriteChime()
        {
            const int sampleRate = 44100;
            const int sampleCount = 35280;
            const double noteDuration = 0.35;
            // Original C5–E5–G5–C6 arpeggio, synthesized without external samples.
            double[] frequencies = { 523.2511306, 659.2551138, 783.9908720, 1046.5022612 };
            var samples = new double[sampleCount];
            double peak = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                double time = i / (double)sampleRate;
                for (int note = 0; note < frequencies.Length; note++)
                {
                    double elapsed = time - note * 0.13;
                    if (elapsed < 0 || elapsed >= noteDuration)
                    {
                        continue;
                    }
                    double attack = Math.Sin(Math.Min(1, elapsed / 0.012) * Math.PI * 0.5);
                    double release = Math.Sin(Math.Min(1, (noteDuration - elapsed) / 0.04) * Math.PI * 0.5);
                    double envelope = attack * attack * release * release * Math.Exp(-elapsed * 9);
                    double phase = 2 * Math.PI * frequencies[note] * elapsed;
                    samples[i] += envelope * (Math.Sin(phase) + 0.18 * Math.Sin(2 * phase));
                }
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }

            using (var writer = new BinaryWriter(File.Open(ChimePath, FileMode.CreateNew)))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + sampleCount * 2);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(sampleCount * 2);
                foreach (double sample in samples)
                {
                    writer.Write((short)Math.Round(sample / Math.Max(peak, 0.001) * 0.68 * short.MaxValue));
                }
            }
        }

        private static Button ConfigureSoundButton(Transform safeArea)
        {
            Transform existing = safeArea.Find("SoundButton");
            GameObject soundObject = existing != null ? existing.gameObject :
                new GameObject("SoundButton", typeof(RectTransform));
            soundObject.transform.SetParent(safeArea, false);
            RectTransform rect = soundObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24, -96);
            rect.sizeDelta = new Vector2(176, 100);
            Image image = GetOrAdd<Image>(soundObject);
            image.color = new Color32(52, 76, 112, 255);
            image.raycastTarget = true;
            Button button = GetOrAdd<Button>(soundObject);
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Transform simulation = safeArea.Find("PartToolbar/SimulationButton");
            if (simulation != null)
            {
                button.colors = simulation.GetComponent<Button>().colors;
            }

            Transform existingLabel = rect.Find("Label");
            GameObject labelObject = existingLabel != null ? existingLabel.gameObject :
                new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(rect, false);
            Text label = GetOrAdd<Text>(labelObject);
            label.text = "SOUND ON";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 21;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
