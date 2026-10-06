using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace OmniScan.Machines
{
    /// <summary>Loads the bundled machine catalog from Resources/Machines/machines.json.</summary>
    public static class MachineCatalogLoader
    {
        public const string ResourcePath = "Machines/machines";

        private static MachineCatalog cached;

        public static MachineCatalog Catalog => cached ??= Load();

        public static Machine Find(string machineId) => Catalog.machines.Find(m => m.id == machineId);

        public static Machine FindByReferenceImage(string referenceImage) =>
            Catalog.machines.Find(m => m.referenceImage == referenceImage);

        public static MachineCatalog Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath)
                ?? throw new InvalidOperationException($"Machine catalog not found at Resources/{ResourcePath}.json");
            return Parse(asset.text);
        }

        public static MachineCatalog Parse(string json)
        {
            var catalog = JsonConvert.DeserializeObject<MachineCatalog>(json)
                ?? throw new InvalidOperationException("Machine catalog is empty.");

            var problems = Validate(catalog);
            if (problems.Count > 0)
            {
                throw new InvalidOperationException("Invalid machine catalog:\n- " + string.Join("\n- ", problems));
            }
            return catalog;
        }

        /// <summary>Returns human-readable problems; empty when the catalog is consistent.</summary>
        public static List<string> Validate(MachineCatalog catalog)
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var machine in catalog.machines)
            {
                if (string.IsNullOrEmpty(machine.id)) { problems.Add("A machine has no id."); continue; }
                if (!ids.Add(machine.id)) problems.Add($"Duplicate machine id '{machine.id}'.");
                if (string.IsNullOrEmpty(machine.referenceImage)) problems.Add($"{machine.id}: no referenceImage.");
                if (machine.physicalWidthMeters <= 0f || machine.physicalHeightMeters <= 0f)
                    problems.Add($"{machine.id}: physical size must be positive.");

                foreach (var hotspot in machine.hotspots)
                {
                    if (hotspot.x < 0f || hotspot.x > 1f || hotspot.y < 0f || hotspot.y > 1f)
                        problems.Add($"{machine.id}: hotspot '{hotspot.id}' centre is outside the panel.");
                }

                foreach (var procedure in machine.procedures)
                {
                    foreach (var step in procedure.steps)
                    {
                        if (machine.FindHotspot(step.hotspotId) == null)
                            problems.Add($"{machine.id}/{procedure.id}: step points at unknown hotspot '{step.hotspotId}'.");
                    }
                }

                foreach (var issue in machine.issues)
                {
                    if (!string.IsNullOrEmpty(issue.procedureId) && machine.FindProcedure(issue.procedureId) == null)
                        problems.Add($"{machine.id}: issue {issue.code} references unknown procedure '{issue.procedureId}'.");
                }
            }
            return problems;
        }
    }
}
