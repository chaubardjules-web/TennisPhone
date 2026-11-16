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
    }
}
