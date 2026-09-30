using System.Text;
using Newtonsoft.Json;
using OmniScan.Api;
using OmniScan.Panels;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace OmniScan.Scan
{
  
    public class ScanController : MonoBehaviour
    {
        [SerializeField] private string baseUrl = "http://localhost:8000/api/v1";
        [SerializeField, Range(0f, 1f)] private float minConfidence = 0.6f;

        private ARCameraManager cameraManager;
        private ScanPipeline pipeline;
        private AnchoredPanelSpawner panels;
        private bool scanning;
        private string overlayText = "Point at a machine and tap Scan.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<ScanController>() == null)
            {
                new GameObject(nameof(ScanController)).AddComponent<ScanController>();
            }
        }

        private void Start()
        {
            cameraManager = FindFirstObjectByType<ARCameraManager>();
            pipeline = new ScanPipeline(new ApiClient(baseUrl), minConfidence);
            panels = gameObject.AddComponent<AnchoredPanelSpawner>();
        }

        private async void Scan()
        {
            if (scanning) return;
            scanning = true;
            overlayText = "Scanning...";

            try
            {
                var jpeg = CameraFrameCapture.CaptureJpeg(cameraManager);
                var outcome = await pipeline.RunAsync(jpeg);
                overlayText = outcome.IsSuccess ? Describe(outcome) : outcome.Message;

                if (outcome.IsSuccess)
                {
                    await panels.ShowAsync(outcome.Subgraph);
                    Debug.Log($"[Scan] {outcome.Classification.label} ({outcome.Classification.confidence:P0}) subgraph:\n" +
                              JsonConvert.SerializeObject(outcome.Subgraph, Formatting.Indented));
                }
                else
                {
                    panels.Clear();
                    Debug.LogWarning($"[Scan] {outcome.Message}");
                }
            }
            catch (System.Exception e)
            {
                // Last-resort guard: the scan button must never take the app down.
                Debug.LogException(e);
                overlayText = "Something went wrong. Please try again.";
            }
            finally
            {
                scanning = false;
            }
        }

        private static string Describe(ScanOutcome outcome)
        {
            var s = outcome.Subgraph;
            var sb = new StringBuilder(outcome.Message);
            sb.Append($" ({outcome.Classification.confidence:P0})");
            sb.Append($"\nComponents: {s.components?.Count ?? 0}");
            sb.Append($"\nDocuments: {s.documents?.Count ?? 0}");
            sb.Append($"\n3D model: {(s.model3d != null ? "yes" : "no")}");
            return sb.ToString();
        }

        private void OnGUI()
        {
            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            GUI.enabled = !scanning;
            if (GUI.Button(new Rect(width / 2 - 60, height - 70, 120, 50), scanning ? "..." : "Scan"))
            {
                Scan();
            }
            GUI.enabled = true;

            GUI.Box(new Rect(10, 10, width - 20, 90), overlayText);
        }
    }
}
