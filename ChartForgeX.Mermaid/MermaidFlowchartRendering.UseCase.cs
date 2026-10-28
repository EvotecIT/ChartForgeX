using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidFlowchartRendering {
    private static void ApplyUseCase(TopologyChart chart, MermaidUseCaseDocument document, MermaidFlowchartRenderOptions options) {
        document.Configuration.UseCase?.Apply(chart, options);
        for (var index = 0; index < document.Nodes.Count; index++) {
            var source = document.Nodes[index]; var target = chart.Nodes[index];
            ApplyUseCaseActor(target, source, chart.DefaultRenderOptions?.IconCatalog ?? options.IconCatalog ?? TopologyIconCatalog.Default());
            if (!source.Properties.TryGetValue("usecase.stereotype", out var stereotype)) continue;
            target.Subtitle = "«" + MermaidLabelText.DisplayCaption(stereotype) + "»";
            target.Metadata["mermaid.stereotype"] = stereotype;
            if (source.Properties.TryGetValue("usecase.stereotype.raw", out var raw)) target.Metadata["mermaid.stereotype.raw"] = raw;
            if (source.PropertySpans.TryGetValue("usecase.stereotype", out var span)) {
                target.Metadata["mermaid.stereotype.line"] = span.Line.ToString(CultureInfo.InvariantCulture);
                target.Metadata["mermaid.stereotype.column"] = span.Column.ToString(CultureInfo.InvariantCulture);
                target.Metadata["mermaid.stereotype.length"] = span.Length.ToString(CultureInfo.InvariantCulture);
            }
        }
        MermaidTopologyRendering.AddNotes(chart, document.Notes);
    }
}
