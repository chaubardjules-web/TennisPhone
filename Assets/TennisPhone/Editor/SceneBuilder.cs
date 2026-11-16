using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TennisPhone.EditorTools
{
    /// <summary>
    /// Construit les scènes du projet à partir du code, pour qu'elles soient reproductibles.
    /// </summary>
    public static class SceneBuilder
    {
        public const string SensorDebugScenePath = "Assets/TennisPhone/Scenes/SensorDebug.unity";

        [MenuItem("TennisPhone/Creer la scene de debug des capteurs")]
        public static void BuildSensorDebugScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Caméra");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            cameraObject.transform.position = new Vector3(0f, 0.32f, -0.42f);
            cameraObject.transform.LookAt(Vector3.zero);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Lumière");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var app = new GameObject("Téléphone");
            app.AddComponent<PhoneLink>();
            app.AddComponent<SensorDebugScreen>();

            Directory.CreateDirectory(Path.GetDirectoryName(SensorDebugScenePath));
            EditorSceneManager.SaveScene(scene, SensorDebugScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SensorDebugScenePath, true),
            };

            // Le serveur doit continuer à alimenter le jeu quand la fenêtre n'a pas le focus.
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();

            Debug.Log("[TennisPhone] Scène créée : " + SensorDebugScenePath);
        }

        public const string SwingLabScenePath = "Assets/TennisPhone/Scenes/SwingLab.unity";
        public const string GameplayConfigPath = "Assets/TennisPhone/Config/GameplayConfig.asset";

        /// <summary>Charge l'asset de configuration, ou le crée avec ses valeurs par défaut.</summary>
        public static GameplayConfig EnsureGameplayConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameplayConfig>(GameplayConfigPath);
            if (config == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(GameplayConfigPath));
                config = ScriptableObject.CreateInstance<GameplayConfig>();
                AssetDatabase.CreateAsset(config, GameplayConfigPath);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

        [MenuItem("TennisPhone/Creer la scene du labo de swing")]
        public static void BuildSwingLabScene()
        {
            GameplayConfig config = EnsureGameplayConfig();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Caméra");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            cameraObject.transform.position = new Vector3(0f, 1.55f, -1.6f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.15f, 0.3f));
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Lumière");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Sol";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            var groundMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.16f, 0.32f, 0.26f) };
            AssetDatabase.CreateAsset(groundMaterial, "Assets/TennisPhone/Config/Sol.mat");
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var game = new GameObject("Raquette du joueur");
            game.AddComponent<PhoneLink>();
            RacketInput racketInput = game.AddComponent<RacketInput>();
            racketInput.SetConfig(config);
            EditorUtility.SetDirty(racketInput);
            game.AddComponent<SensorRecorder>();
            game.AddComponent<SwingLabScreen>();

            Directory.CreateDirectory(Path.GetDirectoryName(SwingLabScenePath));
            EditorSceneManager.SaveScene(scene, SwingLabScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SwingLabScenePath, true),
                new EditorBuildSettingsScene(SensorDebugScenePath, File.Exists(SensorDebugScenePath)),
            };

            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();

            Debug.Log("[TennisPhone] Scène créée : " + SwingLabScenePath);
        }
    }
}
