using System.Collections.Generic;
using UnityEngine;

namespace OmniScan.Panels
{

    public static class PanelLayout
    {
        public const float Radius = 0.45f;
        public const float HeightOffset = 0.15f;
        public const float MaxArcDegrees = 100f;
        public const float StepDegrees = 28f;

        /// <summary>
        /// Local offsets for <paramref name="count"/> panels, centred on the local -Z axis
        /// (the side facing the camera once the anchor is oriented toward it).
        /// </summary>
        public static List<Vector3> Offsets(int count)
        {
            var result = new List<Vector3>(count);
            if (count <= 0) return result;

            var arc = Mathf.Min(MaxArcDegrees, StepDegrees * (count - 1));
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0f : (float)i / (count - 1) - 0.5f;
                var angle = t * arc * Mathf.Deg2Rad;
                result.Add(new Vector3(Mathf.Sin(angle) * Radius, HeightOffset, -Mathf.Cos(angle) * Radius));
            }
            return result;
        }
    }
}
