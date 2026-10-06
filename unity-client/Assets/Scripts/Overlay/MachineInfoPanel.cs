using System.Text;
using OmniScan.Machines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OmniScan.Overlay
{
    /// <summary>
    /// World-space info panel shown beside a recognised machine, with tabs for Overview, History,
    /// Issues & fixes and Docs. Built in the tracked image's local space (machine panel in XZ, +Y out of it,
    /// +Z towards its top), lying in the same plane so it reads naturally on a table or a wall.
    /// </summary>
    public class MachineInfoPanel : MonoBehaviour
    {
        private const float CanvasWidth = 1000f;
        private const float CanvasHeight = 820f;
        private const float WidthRatio = 1.25f;  // panel width relative to the machine panel width
        private const float GapRatio = 0.06f;    // gap between machine and info panel, relative to machine width
        private const float Lift = 0.004f;       // float just above the surface to avoid z-fighting

        private static readonly string[] TabNames = { "Overview", "History", "Issues & fixes", "Docs" };

        private static readonly Color Background = new(0.06f, 0.08f, 0.12f, 0.92f);
        private static readonly Color TabIdle = new(0.16f, 0.2f, 0.28f, 1f);
        private static readonly Color TabActive = new(0.1f, 0.85f, 1f, 1f);
        private static readonly Color Accent = new(0.1f, 0.85f, 1f, 1f);

        private Machine machine;
        private TextMeshProUGUI content;
        private readonly Image[] tabBackgrounds = new Image[TabNames.Length];
        private readonly TextMeshProUGUI[] tabLabels = new TextMeshProUGUI[TabNames.Length];

        public int ActiveTab { get; private set; }

        public static MachineInfoPanel Create(Transform machineRoot, Machine machine)
        {
            var size = machine.PhysicalSize;
            var panelWidth = size.x * WidthRatio;

            var root = new GameObject("MachineInfoPanel", typeof(RectTransform));
            root.transform.SetParent(machineRoot, false);
            root.transform.localPosition = new Vector3(size.x / 2f + size.x * GapRatio + panelWidth / 2f, Lift, 0f);
            // Canvas faces out of the machine panel (+Y), with its top towards the machine's top (+Z).
            root.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            root.AddComponent<GraphicRaycaster>();

            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            rect.localScale = Vector3.one * (panelWidth / CanvasWidth);

            var panel = root.AddComponent<MachineInfoPanel>();
            panel.machine = machine;
            panel.Build(rect);
            panel.ShowTab(0);
            return panel;
        }

        public void ShowTab(int index)
        {
            ActiveTab = Mathf.Clamp(index, 0, TabNames.Length - 1);
            for (var i = 0; i < TabNames.Length; i++)
            {
                var active = i == ActiveTab;
                tabBackgrounds[i].color = active ? TabActive : TabIdle;
                tabLabels[i].color = active ? Color.black : Color.white;
            }

            content.text = ActiveTab switch
            {
                0 => OverviewText(machine),
                1 => HistoryText(machine),
                2 => IssuesText(machine),
                _ => DocsText(machine),
            };
        }

        private void Build(RectTransform rect)
        {
            var background = UiBuilder.Stretch("Background", rect).gameObject.AddComponent<Image>();
            background.color = Background;
            background.raycastTarget = false;

            var accentBar = UiBuilder.Anchored("AccentBar", rect, new Vector2(0f, 0.985f), Vector2.one);
            accentBar.gameObject.AddComponent<Image>().color = Accent;

            UiBuilder.Text("Title", rect, machine.name, 46, FontStyles.Bold, new Vector2(0.04f, 0.88f), new Vector2(0.7f, 0.98f));
            UiBuilder.Text("Subtitle", rect, $"{machine.manufacturer}  ·  {machine.model}", 24, FontStyles.Normal,
                new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.885f)).color = new Color(0.75f, 0.8f, 0.9f);

            var chip = UiBuilder.Anchored("RecognisedChip", rect, new Vector2(0.72f, 0.9f), new Vector2(0.96f, 0.965f));
            chip.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.7f, 0.35f, 1f);
            var chipText = UiBuilder.Text("Label", chip, "RECOGNISED", 24, FontStyles.Bold, Vector2.zero, Vector2.one);
            chipText.alignment = TextAlignmentOptions.Center;

            var tabWidth = 0.92f / TabNames.Length;
            for (var i = 0; i < TabNames.Length; i++)
            {
                var x0 = 0.04f + i * tabWidth;
                var tab = UiBuilder.Anchored($"Tab_{TabNames[i]}", rect, new Vector2(x0 + 0.005f, 0.72f), new Vector2(x0 + tabWidth - 0.005f, 0.8f));
                tabBackgrounds[i] = tab.gameObject.AddComponent<Image>();
                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = tabBackgrounds[i];
                var index = i;
                button.onClick.AddListener(() => ShowTab(index));

                tabLabels[i] = UiBuilder.Text("Label", tab, TabNames[i], 26, FontStyles.Bold, Vector2.zero, Vector2.one);
                tabLabels[i].alignment = TextAlignmentOptions.Center;
                tabLabels[i].raycastTarget = false;
            }

            content = UiBuilder.Text("Content", rect, "", 26, FontStyles.Normal, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.69f));
            content.alignment = TextAlignmentOptions.TopLeft;
            content.enableAutoSizing = true;
            content.fontSizeMin = 16f;
            content.fontSizeMax = 26f;
            content.richText = true;
        }

        private static string OverviewText(Machine m)
        {
            var sb = new StringBuilder();
            sb.AppendLine(m.summary).AppendLine();
            foreach (var spec in m.specs) sb.AppendLine($"<color=#7FD8FF>{spec.label}</color>   {spec.value}");
            return sb.ToString();
        }

        private static string HistoryText(Machine m)
        {
            if (m.history.Count == 0) return "No history recorded.";
            var sb = new StringBuilder();
            foreach (var h in m.history)
            {
                sb.AppendLine($"<b>{h.date}</b>  <color=#7FD8FF>{h.type.ToUpperInvariant()}</color>  {h.title}");
                sb.AppendLine($"<size=85%>{h.description}  <i>({h.technician})</i></size>").AppendLine();
            }
            return sb.ToString();
        }

        private static string IssuesText(Machine m)
        {
            if (m.issues.Count == 0) return "No known issues.";
            var sb = new StringBuilder();
            foreach (var issue in m.issues)
            {
                var color = issue.severity == "critical" ? "#FF5A5A" : "#FFB020";
                sb.AppendLine($"<color={color}><b>{issue.code}</b></color>  <b>{issue.title}</b>  <size=80%>({issue.severity})</size>");
                sb.AppendLine($"<size=85%><color=#9FB0C8>Symptom</color> {issue.symptom}");
                sb.AppendLine($"<color=#9FB0C8>Cause</color> {issue.cause}");
                sb.AppendLine($"<color=#9FB0C8>Fix</color> {issue.fix}</size>");
                var procedure = string.IsNullOrEmpty(issue.procedureId) ? null : m.FindProcedure(issue.procedureId);
                if (procedure != null) sb.AppendLine($"<size=80%><color=#7FD8FF>Guided procedure: {procedure.title}</color></size>");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string DocsText(Machine m)
        {
            if (m.documents.Count == 0) return "No documents.";
            var sb = new StringBuilder();
            foreach (var doc in m.documents)
            {
                sb.AppendLine($"<b>{doc.title}</b>  <color=#7FD8FF><size=80%>{doc.type.ToUpperInvariant()}</size></color>");
                sb.AppendLine($"<size=85%>{doc.summary}</size>").AppendLine();
            }
            return sb.ToString();
        }
    }
}
