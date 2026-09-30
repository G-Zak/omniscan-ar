using System.Collections.Generic;
using System.Threading.Tasks;
using OmniScan.Api;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OmniScan.Panels
{

    public class AnchoredPanelSpawner : MonoBehaviour
    {
        private const int MaxPanels = 8;
        private const float FallbackDistance = 1.0f;

        private static readonly Color ComponentColor = new(0.25f, 0.65f, 1f);
        private static readonly Color DocumentColor = new(1f, 0.7f, 0.2f);

        private ARRaycastManager raycastManager;
        private ARAnchorManager anchorManager;
        private GameObject current;
        private int generation;

        private void Start()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return;

            raycastManager = GetOrAdd<ARRaycastManager>(origin.gameObject);
            anchorManager = GetOrAdd<ARAnchorManager>(origin.gameObject);
            GetOrAdd<ARPlaneManager>(origin.gameObject); // planes give raycasts something stable to hit
        }

        public async Task ShowAsync(SubgraphResponse subgraph)
        {
            Clear();
            var ticket = ++generation;

            var entries = BuildEntries(subgraph);
            if (entries.Count == 0) return;

            var pose = ResolveAnchorPose();
            var anchor = await CreateAnchorAsync(pose);
            if (ticket != generation)
            {
                if (anchor != null) Destroy(anchor);
                return;
            }

            current = anchor != null ? anchor : new GameObject("PanelAnchor");
            if (anchor == null) current.transform.SetPositionAndRotation(pose.position, pose.rotation);

            var offsets = PanelLayout.Offsets(entries.Count);
            for (var i = 0; i < entries.Count; i++)
            {
                var panel = InfoPanelFactory.Create(current.transform, entries[i].title, entries[i].subtitle, entries[i].accent);
                panel.transform.localPosition = offsets[i];
            }
        }

        public void Clear()
        {
            generation++;
            if (current != null) Destroy(current);
            current = null;
        }

        private static List<(string title, string subtitle, Color accent)> BuildEntries(SubgraphResponse s)
        {
            var list = new List<(string, string, Color)>();
            if (s == null) return list;

            if (s.components != null)
            {
                foreach (var c in s.components)
                {
                    list.Add((c.name, string.IsNullOrEmpty(c.partNumber) ? "Component" : $"Part # {c.partNumber}", ComponentColor));
                }
            }
            if (s.documents != null)
            {
                foreach (var d in s.documents)
                {
                    list.Add((d.title, string.IsNullOrEmpty(d.type) ? "Document" : d.type, DocumentColor));
                }
            }
            if (list.Count > MaxPanels) list.RemoveRange(MaxPanels, list.Count - MaxPanels);
            return list;
        }

        private Pose ResolveAnchorPose()
        {
            var cam = Camera.main;
            var camPos = cam.transform.position;

            Vector3 position;
            var hits = new List<ARRaycastHit>();
            var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (raycastManager != null &&
                raycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                position = hits[0].pose.position;
            }
            else
            {
                position = camPos + cam.transform.forward * FallbackDistance;
            }

            // Orient the anchor so the panel arc opens toward where the user stood.
            var toCam = camPos - position;
            toCam.y = 0f;
            var rotation = toCam.sqrMagnitude > 1e-4f
                ? Quaternion.LookRotation(-toCam.normalized, Vector3.up)
                : Quaternion.identity;
            return new Pose(position, rotation);
        }

        private async Task<GameObject> CreateAnchorAsync(Pose pose)
        {
            if (anchorManager == null || anchorManager.subsystem == null || !anchorManager.subsystem.running) return null;

            var result = await anchorManager.TryAddAnchorAsync(pose);
            if (!result.status.IsSuccess()) return null;
            return result.value.gameObject;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }
}
