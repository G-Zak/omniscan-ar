using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OmniScan.Recognition
{
    /// <summary>Minimal on-screen status for recognition (phone only; world-space UI replaces it on Quest).</summary>
    [RequireComponent(typeof(MachineRecognizer))]
    public class RecognitionHud : MonoBehaviour
    {
        [SerializeField] private bool showDiagnostics = true;

        private MachineRecognizer recognizer;
        private ARCameraManager cameraManager;
        private ARCameraBackground cameraBackground;
        private int framesReceived;
        private GUIStyle style;
        private GUIStyle smallStyle;

        private void Awake() => recognizer = GetComponent<MachineRecognizer>();

        private void OnEnable()
        {
            cameraManager = FindAnyObjectByType<ARCameraManager>();
            cameraBackground = FindAnyObjectByType<ARCameraBackground>();
            if (cameraManager != null) cameraManager.frameReceived += OnFrame;
        }

        private void OnDisable()
        {
            if (cameraManager != null) cameraManager.frameReceived -= OnFrame;
        }

        private void OnFrame(ARCameraFrameEventArgs args) => framesReceived++;

        private string Status()
        {
            if (recognizer.CurrentMachine != null) return $"Recognised: {recognizer.CurrentMachine.name}";
            return recognizer.CurrentTrackingState == TrackingState.Limited
                ? "Hold steady, locking on..."
                : "Point the camera at a machine panel";
        }

        // One line that pinpoints why the camera feed or tracking is missing, without needing adb logs.
        private string Diagnostics() =>
            $"AR: {ARSession.state} | camera frames: {framesReceived} | " +
            $"background: {(cameraBackground != null && cameraBackground.enabled ? "on" : "off")} | " +
            $"gfx: {SystemInfo.graphicsDeviceType}";

        private void OnGUI()
        {
            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            smallStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };

            var width = Screen.width / scale;
            GUI.Box(new Rect(10, 10, width - 20, 50), Status(), style);
            if (showDiagnostics) GUI.Label(new Rect(10, 62, width - 20, 20), Diagnostics(), smallStyle);
        }
    }
}
