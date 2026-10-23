using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace SiteVisuals;

internal static partial class DocVisuals {
    /// <summary>Registers source-backed native views; the site never synthesizes hierarchy data or axis geometry.</summary>
    private static void WriteNativeCapabilities(VisualOutput output, SortedDictionary<string, object> figures, Dictionary<string, object> examples) {
        var hierarchyProgram = new Dictionary<string, object>();
        SiteRecipeSource.AttachShared(output, hierarchyProgram, "native-hierarchy", nameof(NativeHierarchyWebsiteRecipe),
            "SunburstRecipe", true, "views", "helpers");
        examples["native-hierarchy"] = hierarchyProgram;
        foreach (var kind in new[] { ChartSeriesKind.Treemap, ChartSeriesKind.Sunburst }) {
            foreach (var root in new string?[] { null, "engineering" }) {
                var id = "hierarchy-" + kind.ToString().ToLowerInvariant() + "-" + (root ?? "full");
                var entry = output.Pair("docs/" + id, (mode, _) => NativeHierarchyWebsiteRecipe.Prepare(kind, mode, root).ToSvg(id));
                foreach (var (name, mode, _) in output.Modes())
                    output.Binary("docs/" + id + "-" + name + ".png", NativeHierarchyWebsiteRecipe.Prepare(kind, mode, root).ToPng());
                entry["png"] = "/site/docs/" + id;
                figures[id] = entry;
            }
        }

        var axisProgram = new Dictionary<string, object>();
        SiteRecipeSource.AttachComplete(output, axisProgram, "native-axis", nameof(NativeAxisWebsiteRecipe), true, "views", "helpers");
        examples["native-axis"] = axisProgram;
        foreach (var variant in new[] { "positive-log", "negative-log", "ordinal", "calendar-month" }) {
            var id = "axis-" + variant;
            var entry = output.Pair("docs/" + id, (mode, _) => NativeAxisWebsiteRecipe.Create(variant, mode).ToSvg(id));
            foreach (var (name, mode, _) in output.Modes())
                output.Binary("docs/" + id + "-" + name + ".png", NativeAxisWebsiteRecipe.Create(variant, mode).ToPng());
            entry["png"] = "/site/docs/" + id;
            figures[id] = entry;
        }
    }
}
