using OmniScan.Machines;
using OmniScan.Overlay;
using UnityEngine;

namespace OmniScan.Recognition
{
    /// <summary>
    /// "Recognised" marker drawn on a tracked machine panel: a glowing frame around the panel edges
    /// and the machine info panel beside it. Built in the tracked image's local space (panel in XZ, +Y out of the panel).
    /// </summary>
    public class MachineMarker : MonoBehaviour
    {
        private const float FrameThickness = 0.004f;
        private const float FrameHeight = 0.002f;

        private static readonly Color FrameColor = new(0.1f, 0.85f, 1f);

        /// <summary>Unlit material asset kept in Resources so its shader is always included in builds.</summary>
        public const string MaterialResource = "Materials/OmniScanUnlit";

        public Machine Machine { get; private set; }
        public MachineInfoPanel InfoPanel { get; private set; }

        public static MachineMarker Create(Transform trackedImage, Machine machine)
        {
            var root = new GameObject($"Marker_{machine.id}");
            root.transform.SetParent(trackedImage, false);

            var marker = root.AddComponent<MachineMarker>();
            marker.Machine = machine;

            var size = machine.PhysicalSize;
            var material = CreateMaterial(FrameColor);
            AddEdge(root.transform, material, new Vector3(0f, 0f, size.y / 2f), new Vector3(size.x, FrameHeight, FrameThickness));
            AddEdge(root.transform, material, new Vector3(0f, 0f, -size.y / 2f), new Vector3(size.x, FrameHeight, FrameThickness));
            AddEdge(root.transform, material, new Vector3(size.x / 2f, 0f, 0f), new Vector3(FrameThickness, FrameHeight, size.y));
            AddEdge(root.transform, material, new Vector3(-size.x / 2f, 0f, 0f), new Vector3(FrameThickness, FrameHeight, size.y));

            marker.InfoPanel = MachineInfoPanel.Create(root.transform, machine);
            return marker;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }

        /// <summary>
        /// Scales the overlay to the size ARCore actually measured, so it fits the panel whether it is printed
        /// at the catalog size or shown bigger or smaller (for example on a monitor).
        /// </summary>
        public void FitToMeasuredSize(Vector2 measuredSize)
        {
            if (measuredSize.x <= 0f || Machine.physicalWidthMeters <= 0f) return;
            transform.localScale = Vector3.one * (measuredSize.x / Machine.physicalWidthMeters);
        }

        private static void AddEdge(Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "Edge";
            var collider = edge.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider); // edit mode: editor previews
            edge.transform.SetParent(parent, false);
            edge.transform.localPosition = position;
            edge.transform.localScale = scale;
            var renderer = edge.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public static Material CreateMaterial(Color color)
        {
            var template = Resources.Load<Material>(MaterialResource);
            if (template == null)
            {
                Debug.LogError($"[Recognition] Missing Resources/{MaterialResource}.mat. Run OmniScan > Setup > Configure Image Tracking.");
                return new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
            }
            var material = new Material(template);
            material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
