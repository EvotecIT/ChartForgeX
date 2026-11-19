using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    /// <summary>Writes the hidden progress bar the runtime shows during the initial physics layout of a large graph.</summary>
    private static void WriteLayoutProgress(StringBuilder writer, GraphScene scene, HtmlGraphExplorerOptions options) {
        if (!options.IncludeLayoutProgress || scene.Nodes.Count < options.LayoutProgressMinimumNodes) return;
        if (!scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics) || !scene.Options.HasFeature(GraphSceneFeatures.Stabilization)) return;
        var label = options.Localization[HtmlChartText.GraphLayoutProgress];
        writer.Append("<div class=\"cfx-graph-layout-progress\" data-cfx-role=\"graph-layout-progress\" role=\"progressbar\" aria-valuemin=\"0\" aria-valuemax=\"100\" aria-valuenow=\"0\"");
        Attribute(writer, "aria-label", label);
        writer.Append(" hidden><span class=\"cfx-graph-layout-progress__label\">").Append(Text(label));
        writer.Append("</span><span class=\"cfx-graph-layout-progress__value\" data-cfx-role=\"graph-layout-progress-value\">0%</span>");
        writer.Append("<span class=\"cfx-graph-layout-progress__track\" aria-hidden=\"true\"><span class=\"cfx-graph-layout-progress__bar\"></span></span></div>");
    }
}
