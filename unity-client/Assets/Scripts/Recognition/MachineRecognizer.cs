using System;
using System.Collections.Generic;
using OmniScan.Machines;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OmniScan.Recognition
{
    /// <summary>
    /// Recognises known machines through AR Foundation image tracking: each tracked reference image is mapped
    /// to a catalog machine, and a marker is attached to it while it is tracked.
    /// </summary>
    public class MachineRecognizer : MonoBehaviour
    {
        [SerializeField] private ARTrackedImageManager trackedImageManager;

        private readonly Dictionary<TrackableId, MachineMarker> markers = new();

        public Machine CurrentMachine { get; private set; }
        public Transform CurrentAnchor { get; private set; }
        public TrackingState CurrentTrackingState { get; private set; } = TrackingState.None;

        /// <summary>Raised when a machine becomes tracked. The transform follows the machine's front panel.</summary>
        public event Action<Machine, Transform> MachineRecognized;

        /// <summary>Raised when the recognised machine stops being tracked.</summary>
        public event Action<Machine> MachineLost;

        private void Awake()
        {
            if (trackedImageManager == null) trackedImageManager = FindAnyObjectByType<ARTrackedImageManager>();
            if (trackedImageManager == null)
            {
                Debug.LogError("[Recognition] No ARTrackedImageManager in the scene. Run OmniScan > Setup > Configure Image Tracking.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (trackedImageManager != null) trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        private void OnDisable()
        {
            if (trackedImageManager != null) trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (var image in args.added) Refresh(image);
            foreach (var image in args.updated) Refresh(image);
            foreach (var removed in args.removed) Remove(removed.Key);
        }

        private void Refresh(ARTrackedImage image)
        {
            if (!markers.TryGetValue(image.trackableId, out var marker))
            {
                var machine = MachineCatalogLoader.FindByReferenceImage(image.referenceImage.name);
                if (machine == null)
                {
                    Debug.LogWarning($"[Recognition] Tracked image '{image.referenceImage.name}' is not in the machine catalog.");
                    return;
                }
                marker = MachineMarker.Create(image.transform, machine);
                markers[image.trackableId] = marker;
            }

            var tracked = image.trackingState == TrackingState.Tracking;
            marker.SetVisible(tracked);
            if (tracked) marker.FitToMeasuredSize(image.size);

            if (tracked && CurrentMachine != marker.Machine)
            {
                CurrentMachine = marker.Machine;
                CurrentAnchor = image.transform;
                Debug.Log($"[Recognition] Recognised {marker.Machine.name}");
                MachineRecognized?.Invoke(marker.Machine, image.transform);
            }
            else if (!tracked && CurrentMachine == marker.Machine)
            {
                Lose(marker.Machine);
            }

            if (CurrentMachine == null || CurrentMachine == marker.Machine) CurrentTrackingState = image.trackingState;
        }

        private void Remove(TrackableId id)
        {
            if (!markers.TryGetValue(id, out var marker)) return;
            markers.Remove(id);
            if (CurrentMachine == marker.Machine) Lose(marker.Machine);
            if (marker != null) Destroy(marker.gameObject);
        }

        private void Lose(Machine machine)
        {
            Debug.Log($"[Recognition] Lost {machine.name}");
            CurrentMachine = null;
            CurrentAnchor = null;
            MachineLost?.Invoke(machine);
        }
    }
}
