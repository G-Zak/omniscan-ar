using UnityEngine;

namespace OmniScan.Panels
{

    public class BillboardFacing : MonoBehaviour
    {
        private Transform viewer;

        private void LateUpdate()
        {
            if (viewer == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                viewer = cam.transform;
            }

            var toViewer = viewer.position - transform.position;
            toViewer.y = 0f;
            if (toViewer.sqrMagnitude < 1e-4f) return;

            transform.rotation = Quaternion.LookRotation(-toViewer.normalized, Vector3.up);
        }
    }
}
