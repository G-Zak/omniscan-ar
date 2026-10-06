using System;
using OmniScan.Machines;
using OmniScan.Recognition;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OmniScan.Editor
{
    /// <summary>Builds the image-tracking reference library from the machine catalog and wires it into the mobile scene.</summary>
    public static class ImageTrackingSetup
    {
        public const string LibraryPath = "Assets/Machines/MachineImageLibrary.asset";
        public const string ImageFolder = "Assets/Machines/ReferenceImages";
        public const string MobileScene = "Assets/Scenes/SampleScene.unity";

        public const string UnlitMaterialPath = "Assets/Resources/" + MachineMarker.MaterialResource + ".mat";

        [MenuItem("OmniScan/Setup/Configure Image Tracking")]
        public static void Configure()
        {
            EnsureUnlitMaterial();
            EnsureArBackgroundFeature();
            var library = BuildLibrary();
            WireScene(library);
        }

        /// <summary>
        /// Runtime-created materials lose their shader in builds (magenta). Overlays clone this asset instead;
        /// living in Resources guarantees the URP Unlit shader ships.
        /// </summary>
        public static Material EnsureUnlitMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit shader not found.");

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(UnlitMaterialPath)!);
            material = new Material(shader) { name = "OmniScanUnlit" };
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, UnlitMaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ImageTracking] Created {UnlitMaterialPath}");
            return material;
        }

        /// <summary>
        /// URP only draws the AR camera feed if the active renderer has an ARBackgroundRendererFeature,
        /// registered in both m_RendererFeatures and m_RendererFeatureMap.
        /// </summary>
        public static void EnsureArBackgroundFeature()
        {
            for (var level = 0; level < QualitySettings.names.Length; level++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(level) is not UniversalRenderPipelineAsset pipeline) continue;

                var pipelineSo = new SerializedObject(pipeline);
                var renderers = pipelineSo.FindProperty("m_RendererDataList");
                for (var r = 0; r < renderers.arraySize; r++)
                {
                    if (renderers.GetArrayElementAtIndex(r).objectReferenceValue is not UniversalRendererData data) continue;

                    var feature = data.rendererFeatures.Find(f => f is ARBackgroundRendererFeature);
                    if (feature == null)
                    {
                        feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                        feature.name = "AR Background Renderer Feature";
                        AssetDatabase.AddObjectToAsset(feature, data);
                        data.rendererFeatures.Add(feature);
                        EditorUtility.SetDirty(data);
                        AssetDatabase.SaveAssets(); // assigns the sub-asset its local file id
                    }

                    // Rebuild the feature map from the feature list so URP serialises them consistently.
                    var so = new SerializedObject(data);
                    var map = so.FindProperty("m_RendererFeatureMap");
                    map.arraySize = data.rendererFeatures.Count;
                    for (var i = 0; i < data.rendererFeatures.Count; i++)
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[i], out _, out long localId);
                        map.GetArrayElementAtIndex(i).longValue = localId;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(data);
                    Debug.Log($"[ImageTracking] AR background feature present on {data.name} (quality '{QualitySettings.names[level]}').");
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Batch entry point: Unity -batchmode -executeMethod OmniScan.Editor.ImageTrackingSetup.ConfigureBatch</summary>
        public static void ConfigureBatch()
        {
            try
            {
                Configure();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static XRReferenceImageLibrary BuildLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            var machines = MachineCatalogLoader.Load().machines;

            // Update images in place (stable GUIDs, no git churn); drop images no longer in the catalog.
            for (var i = library.count - 1; i >= 0; i--)
            {
                if (machines.Find(m => m.referenceImage == library[i].name) == null) library.RemoveAt(i);
            }

            foreach (var machine in machines)
            {
                var texturePath = $"{ImageFolder}/{machine.referenceImage}.png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null)
                    throw new InvalidOperationException(
                        $"Reference image missing for {machine.id}: {texturePath}. Run tools/mockup/generate_panel.py.");

                var index = IndexOf(library, machine.referenceImage);
                if (index < 0)
                {
                    library.Add();
                    index = library.count - 1;
                }
                library.SetName(index, machine.referenceImage);
                library.SetTexture(index, texture, false);
                library.SetSpecifySize(index, true);
                library.SetSize(index, machine.PhysicalSize);
                Debug.Log($"[ImageTracking] Added {machine.referenceImage} ({machine.physicalWidthMeters * 100f:0.#} cm wide) for {machine.id}");
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        static int IndexOf(XRReferenceImageLibrary library, string imageName)
        {
            for (var i = 0; i < library.count; i++)
            {
                if (library[i].name == imageName) return i;
            }
            return -1;
        }

        static void WireScene(XRReferenceImageLibrary library)
        {
            var scene = EditorSceneManager.OpenScene(MobileScene, OpenSceneMode.Single);

            var origin = EnsureArRig();

            var manager = origin.GetComponent<ARTrackedImageManager>();
            if (manager == null) manager = origin.gameObject.AddComponent<ARTrackedImageManager>();
            manager.referenceLibrary = library;
            manager.requestedMaxNumberOfMovingImages = 1;
            EditorUtility.SetDirty(manager);

            var recognizer = UnityEngine.Object.FindAnyObjectByType<MachineRecognizer>();
            if (recognizer == null)
            {
                var go = new GameObject("MachineRecognizer");
                recognizer = go.AddComponent<MachineRecognizer>();
                go.AddComponent<RecognitionHud>();
            }

            // The camera feed is verified now; the temporary smoke-test cube is no longer needed.
            var cube = GameObject.Find("MR Smoke Test Cube");
            if (cube != null) UnityEngine.Object.DestroyImmediate(cube);
            AssetDatabase.DeleteAsset("Assets/Resources/Materials/SmokeTestCube.mat");

            // Taps on world-space panels (info tabs, procedure buttons) need an EventSystem using the Input System.
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var so = new SerializedObject(recognizer);
            so.FindProperty("trackedImageManager").objectReferenceValue = manager;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ImageTracking] {MobileScene}: ARTrackedImageManager on '{origin.name}' with {library.count} image(s), MachineRecognizer added.");
        }

        /// <summary>
        /// Returns a valid AR rig (XR Origin > Camera Offset > Main Camera with camera feed and pose driver).
        /// Broken rigs (an XROrigin without a camera, loose cameras) are replaced with AR Foundation's standard one,
        /// otherwise the camera feed is black and content does not follow the phone.
        /// </summary>
        static XROrigin EnsureArRig()
        {
            XROrigin valid = null;
            foreach (var origin in UnityEngine.Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include))
            {
                if (valid == null && origin.Camera != null && origin.Camera.GetComponent<ARCameraBackground>() != null)
                    valid = origin;
                else
                    UnityEngine.Object.DestroyImmediate(origin.gameObject);
            }

            if (valid == null)
            {
                if (!EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)"))
                    throw new InvalidOperationException("Could not create 'XR Origin (Mobile AR)'. Is AR Foundation installed?");
                valid = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            }

            // Remove cameras outside the rig: a second MainCamera breaks AR rendering.
            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (cam != valid.Camera) UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }

            var rigCamera = valid.Camera.gameObject;
            rigCamera.tag = "MainCamera";
            if (rigCamera.GetComponent<UniversalAdditionalCameraData>() == null)
                rigCamera.AddComponent<UniversalAdditionalCameraData>();

            if (UnityEngine.Object.FindAnyObjectByType<ARSession>() == null)
                new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));

            return valid;
        }
    }
}
