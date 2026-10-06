using TMPro;
using UnityEngine;

namespace OmniScan.Overlay
{
    /// <summary>Small helpers for building world-space UGUI hierarchies from code.</summary>
    public static class UiBuilder
    {
        public static RectTransform Stretch(string name, RectTransform parent) =>
            Anchored(name, parent, Vector2.zero, Vector2.one);

        public static RectTransform Anchored(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static TextMeshProUGUI Text(string name, RectTransform parent, string text, float size, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = Anchored(name, parent, anchorMin, anchorMax);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
