using ChartForgeX.Core;
using ChartForgeX.Themes;

public static partial class V2Examples {
    private static void WriteExpandedDiagrams(string output, ICollection<ProofArtifact> artifacts, bool curated) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var suffix = mode.ToString().ToLowerInvariant();
            WriteModel(output, artifacts, V2GalleryModels.CreateGrid(), "chart-grid-wide-" + suffix, "chart-grid", "Requests and capacity", "wide",
                "Three panels share the same axis domains", mode, 960, 700, true, "V2GalleryModels.CreateGrid()", new[] { "Line", "Bar", "Area" });
            WriteModel(output, artifacts, V2GalleryModels.CreateTopology(), "topology-expanded-" + suffix, "topology", "Service request topology", "expanded",
                "Groups, status, details and routed links", mode, 960, 600, true, "V2GalleryModels.CreateTopology()");
            WriteModel(output, artifacts, V2GalleryModels.CreateFlow(), "flow-expanded-" + suffix, "flow", "Assessment workflow", "expanded",
                "Typed lanes, decisions and branched routes", mode, 900, 600, false, "V2GalleryModels.CreateFlow()");
            WriteModel(output, artifacts, V2GalleryModels.CreateSequence(), "sequence-expanded-" + suffix, "sequence", "Request processing sequence", "expanded",
                "Participant notation, self calls, fragments and activation", mode, 960, 760, false, "V2GalleryModels.CreateSequence()");
            if (curated) continue;
            WriteModel(output, artifacts, V2GalleryModels.CreateGrid(), "chart-grid-compact-" + suffix, "chart-grid", "Requests and capacity", "compact",
                "Shared axes and a spanning panel in a compact viewport", mode, 520, 540, true, "V2GalleryModels.CreateGrid()", new[] { "Line", "Bar", "Area" });
            WriteModel(output, artifacts, V2GalleryModels.CreateTopology(), "topology-compact-" + suffix, "topology", "Service request topology", "compact",
                "The same source fitted to a compact viewport", mode, 420, 360, false, "V2GalleryModels.CreateTopology()");
            WriteModel(output, artifacts, V2GalleryModels.CreateTopology(true), "topology-shapes-" + suffix, "topology", "Topology node shapes", "shapes",
                "Every native node shape", mode, 960, 720, false, "V2GalleryModels.CreateTopology(true)");
        }
    }
}
