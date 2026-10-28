using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.SvgRaster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidTreemapRendering {
    /// <summary>Maps only native color paints through the existing hierarchy ordinal owners.</summary>
    private static void ApplyStyles(ChartSeries series, MermaidTreemapDocument document) {
        var definitions = MermaidStyleParser.Definitions(document.ClassDefinitions);
        for (var index = 0; index < document.Nodes.Count; index++) {
            var node = document.Nodes[index];
            var classes = node.ClassName == null ? new List<string>() : new List<string> { node.ClassName.TrimEnd(';') };
            var styles = MermaidStyleParser.Resolve(definitions, classes, new Dictionary<string, string>(), node.Span, includeDefault: false);
            foreach (var declaration in styles) {
                if (!SvgRasterColor.TryParse(declaration.Value, out var color)) continue;
                if (declaration.Key == "fill") series.WithPointColor(index, color);
                else if (declaration.Key == "stroke") series.WithPointStrokeColor(index, color);
                else if (declaration.Key == "color") series.ConfigurePointDataLabelStyle(index, style => style.Color = color);
            }
        }
    }

    /// <summary>Retains source declarations and class identity as inert portable facts, independent of available painted area.</summary>
    private static void RetainStyles(VisualArtifact artifact, MermaidTreemapDocument document) {
        for (var index = 0; index < document.Nodes.Count; index++) {
            var node = document.Nodes[index];
            if (node.ClassName != null) artifact.Metadata["mermaid.treemap.node." + index.ToString(CultureInfo.InvariantCulture) + ".class"] = node.ClassName;
        }
        for (var index = 0; index < document.ClassDefinitions.Count; index++) {
            var definition = document.ClassDefinitions[index];
            var prefix = "mermaid.treemap.class." + index.ToString(CultureInfo.InvariantCulture);
            artifact.Metadata[prefix + ".name"] = definition.Name;
            foreach (var declaration in definition.Styles) artifact.Metadata[prefix + ".style." + declaration.Key] = declaration.Value;
        }
    }
}
