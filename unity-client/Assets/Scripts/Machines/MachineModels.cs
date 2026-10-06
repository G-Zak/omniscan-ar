using System.Collections.Generic;
using UnityEngine;

namespace OmniScan.Machines
{
    /// <summary>Root of Resources/Machines/machines.json.</summary>
    public class MachineCatalog
    {
        public int version;
        public List<Machine> machines = new List<Machine>();
    }

    public class Machine
    {
        public string id;
        public string name;
        public string manufacturer;
        public string model;
        public string summary;

        /// <summary>Name of the reference image used to recognise this machine (image tracking).</summary>
        public string referenceImage;
        public int imagePixelWidth;
        public int imagePixelHeight;
        public float physicalWidthMeters;
        public float physicalHeightMeters;

        public List<Spec> specs = new List<Spec>();
        public List<Hotspot> hotspots = new List<Hotspot>();
        public List<HistoryEntry> history = new List<HistoryEntry>();
        public List<Issue> issues = new List<Issue>();
        public List<DocumentRef> documents = new List<DocumentRef>();
        public List<Procedure> procedures = new List<Procedure>();

        public Vector2 PhysicalSize => new Vector2(physicalWidthMeters, physicalHeightMeters);

        public Hotspot FindHotspot(string hotspotId) => hotspots.Find(h => h.id == hotspotId);

        public Procedure FindProcedure(string procedureId) => procedures.Find(p => p.id == procedureId);
    }

    public class Spec
    {
        public string label;
        public string value;
    }

    /// <summary>
    /// A button, screen or indicator on the machine's front panel.
    /// x/y are the centre and w/h the size, normalised to the reference image (origin top-left, y down).
    /// </summary>
    public class Hotspot
    {
        public string id;
        public string label;
        public string kind;
        public string shape;
        public float x;
        public float y;
        public float w;
        public float h;

        /// <summary>
        /// Position in the tracked image's local space, in meters.
        /// AR Foundation tracked images lie in their local XZ plane, centred on the origin, with +Z towards the image top.
        /// </summary>
        public Vector3 ToLocalPosition(Vector2 physicalSize) =>
            new Vector3((x - 0.5f) * physicalSize.x, 0f, (0.5f - y) * physicalSize.y);

        public Vector2 ToPhysicalSize(Vector2 physicalSize) => new Vector2(w * physicalSize.x, h * physicalSize.y);
    }

    public class HistoryEntry
    {
        public string date;
        public string type;
        public string title;
        public string description;
        public string technician;
    }

    public class Issue
    {
        public string code;
        public string severity;
        public string title;
        public string symptom;
        public string cause;
        public string fix;
        public string procedureId;
    }

    public class DocumentRef
    {
        public string id;
        public string title;
        public string type;
        public string summary;
    }

    public class Procedure
    {
        public string id;
        public string title;
        public string description;
        public List<ProcedureStep> steps = new List<ProcedureStep>();
    }

    public class ProcedureStep
    {
        public string instruction;
        public string hotspotId;
        public string expectedResult;
    }
}
