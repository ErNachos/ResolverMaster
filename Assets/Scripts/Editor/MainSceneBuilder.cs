using ResolverMaster.Core.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace ResolverMaster.Editor
{
    /// <summary>Creates Assets/Scenes/Main.unity (camera, EventSystem with the Input System UI module, AppBootstrap) and puts it in the build.</summary>
    public static class MainSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Resolver Master/Setup/Build Main Scene")]
        public static void Build()
        {
            var style = UiKitAssets.LoadOrCreateStyle();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = style.background;
            cam.orthographic = true;
            cam.cullingMask = 0;   // the UI is a Screen Space Overlay canvas: the camera only clears the screen

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            new GameObject("App", typeof(AppBootstrap));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("MainSceneBuilder: " + ScenePath + " saved and set as the only build scene.");
        }
    }
}
