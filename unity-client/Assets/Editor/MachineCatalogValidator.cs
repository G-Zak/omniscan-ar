using System;
using OmniScan.Machines;
using UnityEditor;
using UnityEngine;

namespace OmniScan.Editor
{
    public static class MachineCatalogValidator
    {
        [MenuItem("OmniScan/Validate Machine Catalog")]
        public static void ValidateMenu() => Report();

        /// <summary>Batch entry point: Unity -batchmode -executeMethod OmniScan.Editor.MachineCatalogValidator.ValidateBatch</summary>
        public static void ValidateBatch() => EditorApplication.Exit(Report() ? 0 : 1);

        static bool Report()
        {
            try
            {
                var catalog = MachineCatalogLoader.Load();
                foreach (var m in catalog.machines)
                {
                    Debug.Log($"[Catalog] {m.id}: '{m.name}', {m.hotspots.Count} hotspots, {m.procedures.Count} procedures, " +
                              $"{m.issues.Count} issues, {m.history.Count} history, {m.documents.Count} documents, " +
                              $"panel {m.physicalWidthMeters * 100f:0.#} x {m.physicalHeightMeters * 100f:0.#} cm");

                    var estop = m.FindHotspot("estop");
                    var power = m.FindHotspot("power");
                    if (estop != null && power != null)
                    {
                        Debug.Log($"[Catalog] hotspot local positions (m): power {power.ToLocalPosition(m.PhysicalSize)}, " +
                                  $"estop {estop.ToLocalPosition(m.PhysicalSize)}");
                    }
                }
                Debug.Log($"[Catalog] OK: {catalog.machines.Count} machine(s) valid.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Catalog] INVALID: {e.Message}");
                return false;
            }
        }
    }
}
