using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;

namespace OmniScan.Editor
{
    public static class QuestSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/QuestEmpty.unity";

        [MenuItem("OmniScan/Quest/Create Empty Quest Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var origin = new GameObject("XR Origin");
            var xrOrigin = origin.AddComponent<XROrigin>();

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(origin.transform, false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<TrackedPoseDriver>();

            xrOrigin.Camera = cam;
            xrOrigin.CameraFloorOffsetObject = offset;

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "SmokeTestCube";
            cube.transform.position = new Vector3(0f, 1.3f, 1.5f);
            cube.transform.localScale = Vector3.one * 0.3f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }
    }
}
