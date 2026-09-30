using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OmniScan.Panels
{
    public static class InfoPanelFactory
    {
        private const float CanvasWidth = 600f;
        private const float CanvasHeight = 300f;
        private const float MetersPerUnit = 0.0005f;

        public static GameObject Create(Transform parent, string title, string subtitle, Color accent)
        {
            var root = new GameObject($"Panel_{title}", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            rect.localScale = Vector3.one * MetersPerUnit;

            var background = Stretch("Background", rect).gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            background.raycastTarget = false;

            var bar = Stretch("Accent", rect);
            bar.anchorMax = new Vector2(0.03f, 1f);
            bar.gameObject.AddComponent<Image>().color = accent;

            AddText("Title", rect, title, 44, FontStyles.Bold, new Vector2(0.07f, 0.45f), new Vector2(0.96f, 0.95f));
            AddText("Subtitle", rect, subtitle, 30, FontStyles.Normal, new Vector2(0.07f, 0.05f), new Vector2(0.96f, 0.45f));

            root.AddComponent<BillboardFacing>();
            return root;
        }

        private static RectTransform Stretch(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void AddText(string name, RectTransform parent, string text, float size, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = Stretch(name, parent);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;

            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }
    }
}
