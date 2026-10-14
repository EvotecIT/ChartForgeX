using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderWardleyMap(SvgMarkupWriter writer, WardleyMapBlock map) {
        var options = map.Options;
        var theme = options.ResolvedTheme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, map, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildWardleyMapLayout(map, content, y, options.Padding.Bottom, options.Size.Height);
        WriteWardleyAxes(writer, map, layout, theme);
        foreach (var pipeline in map.Pipelines) WriteWardleyPipeline(writer, pipeline, layout, theme);
        foreach (var link in map.Links) WriteWardleyLink(writer, link, layout, theme);
        foreach (var evolution in map.Evolutions) WriteWardleyEvolution(writer, evolution, layout, theme);
        foreach (var note in map.Notes) WriteWardleyNote(writer, note, layout, theme);
        foreach (var marker in map.Markers) WriteWardleyMarker(writer, marker, layout, theme);
        foreach (var placement in layout.Nodes) WriteWardleyNode(writer, placement, theme);
        foreach (var annotation in map.Annotations) WriteWardleyAnnotation(writer, annotation, layout, theme);
    }

    private static void WriteWardleyAxes(SvgMarkupWriter writer, WardleyMapBlock map, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        var plot = layout.Plot;
        writer.StartElement("rect").Attribute("data-cfx-role", "wardley-plot").Attribute("x", F(plot.X)).Attribute("y", F(plot.Y)).Attribute("width", F(plot.Width)).Attribute("height", F(plot.Height)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        for (var index = 1; index < 4; index++) {
            var x = plot.X + plot.Width * index / 4.0;
            writer.StartElement("line").Attribute("data-cfx-role", "wardley-stage-boundary").Attribute("x1", F(x)).Attribute("y1", F(plot.Y)).Attribute("x2", F(x)).Attribute("y2", F(plot.Bottom)).Attribute("stroke", theme.Grid.ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line();
        }

        writer.StartElement("line").Attribute("data-cfx-role", "wardley-axis-x").Attribute("x1", F(plot.X)).Attribute("y1", F(plot.Bottom)).Attribute("x2", F(plot.Right)).Attribute("y2", F(plot.Bottom)).Attribute("stroke", theme.Axis.ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
        writer.StartElement("line").Attribute("data-cfx-role", "wardley-axis-y").Attribute("x1", F(plot.X)).Attribute("y1", F(plot.Y)).Attribute("x2", F(plot.X)).Attribute("y2", F(plot.Bottom)).Attribute("stroke", theme.Axis.ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
        WriteFittedBodyText(writer, "Visibility", Math.Max(0, plot.X - 54), plot.Y + plot.Height * 0.50, 48, 20, TextAlignment.Center, theme.MutedText, theme, 11.5, 700);
        var stageLabelOffset = VisualBlockRendering.WardleyStageLabelOffset(map);
        WriteFittedBodyText(writer, "Evolution", plot.X + plot.Width * 0.38, plot.Bottom + stageLabelOffset + 20, plot.Width * 0.24, 20, TextAlignment.Center, theme.MutedText, theme, 11.5, 700);
        var stages = map.Stages.Count == 0 ? new[] { "Genesis", "Custom", "Product", "Commodity" } : map.Stages;
        for (var index = 0; index < stages.Count; index++) {
            var x = plot.X + plot.Width * (index + 0.5) / stages.Count;
            WriteFittedBodyText(writer, stages[index], x - plot.Width / stages.Count / 2 + 4, plot.Bottom + stageLabelOffset, plot.Width / stages.Count - 8, VisualBlockRendering.WardleyStageLabelHeight, TextAlignment.Center, theme.MutedText, theme, 10.5, 600);
        }
    }

    private static void WriteWardleyLink(SvgMarkupWriter writer, WardleyMapLink link, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        if (!layout.NodeLookup.TryGetValue(link.FromId, out var from) || !layout.NodeLookup.TryGetValue(link.ToId, out var to)) return;
        var color = ChartColorMath.WithOpacity(theme.Axis, VisualBlockRendering.WardleyAccentOpacity);
        writer.StartElement("line").Attribute("data-cfx-role", "wardley-link").Attribute("x1", F(from.X)).Attribute("y1", F(from.Y)).Attribute("x2", F(to.X)).Attribute("y2", F(to.Y)).Attribute("stroke", color.ToCss()).Attribute("stroke-width", 1.2);
        if (link.Dashed) writer.Attribute("stroke-dasharray", "5 5");
        writer.EndEmptyElement().Line();
        WriteWardleyFlowHint(writer, link, from.X, from.Y, to.X, to.Y, color);
        if (link.Label.Length > 0) WriteFittedBodyText(writer, link.Label, (from.X + to.X) / 2 - 50, (from.Y + to.Y) / 2 - 5, 100, 16, TextAlignment.Center, theme.MutedText, theme, 9.5, 600);
    }

    private static void WriteWardleyFlowHint(SvgMarkupWriter writer, WardleyMapLink link, double fromX, double fromY, double toX, double toY, ChartColor color) {
        if (link.Flow == VisualLinkDirection.None) return;
        var dx = toX - fromX;
        var dy = toY - fromY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.000001) return;
        var ux = dx / length;
        var uy = dy / length;
        var midX = (fromX + toX) / 2;
        var midY = (fromY + toY) / 2;
        if (link.Flow == VisualLinkDirection.Forward || link.Flow == VisualLinkDirection.Bidirectional) WriteWardleyFlowArrow(writer, midX + ux * 9, midY + uy * 9, ux, uy, color, "wardley-flow-forward");
        if (link.Flow == VisualLinkDirection.Backward || link.Flow == VisualLinkDirection.Bidirectional) WriteWardleyFlowArrow(writer, midX - ux * 9, midY - uy * 9, -ux, -uy, color, "wardley-flow-backward");
    }

    private static void WriteWardleyFlowArrow(SvgMarkupWriter writer, double centerX, double centerY, double ux, double uy, ChartColor color, string role) {
        var px = -uy;
        var py = ux;
        var tipX = centerX + ux * 7;
        var tipY = centerY + uy * 7;
        var leftX = centerX - ux * 5 + px * 4;
        var leftY = centerY - uy * 5 + py * 4;
        var rightX = centerX - ux * 5 - px * 4;
        var rightY = centerY - uy * 5 - py * 4;
        var path = "M " + F(tipX) + " " + F(tipY) + " L " + F(leftX) + " " + F(leftY) + " L " + F(rightX) + " " + F(rightY) + " Z";
        writer.StartElement("path").Attribute("data-cfx-role", role).Attribute("d", path).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
    }

    private static void WriteWardleyEvolution(SvgMarkupWriter writer, WardleyMapEvolution evolution, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        if (!layout.NodeLookup.TryGetValue(evolution.NodeId, out var from)) return;
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, evolution.TargetEvolution);
        writer.StartElement("line").Attribute("data-cfx-role", "wardley-evolve").Attribute("x1", F(from.X)).Attribute("y1", F(from.Y)).Attribute("x2", F(x)).Attribute("y2", F(from.Y)).Attribute("stroke", VisualBlockRendering.PaletteAt(theme, 2).ToCss()).Attribute("stroke-width", 1.8).Attribute("stroke-dasharray", "4 4").EndEmptyElement().Line();
        writer.StartElement("path").Attribute("data-cfx-role", "wardley-evolve-arrow").Attribute("d", VisualBlockRendering.WardleyEvolutionArrowPath(x, from.Y)).Attribute("fill", VisualBlockRendering.PaletteAt(theme, 2).ToCss()).EndEmptyElement().Line();
    }

    private static void WriteWardleyPipeline(SvgMarkupWriter writer, WardleyMapPipeline pipeline, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        if (!layout.NodeLookup.TryGetValue(pipeline.ParentId, out var parent) || pipeline.Components.Count == 0) return;
        var minX = parent.X;
        var maxX = parent.X;
        foreach (var child in pipeline.Components) {
            var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, child.Evolution);
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            writer.StartElement("circle").Attribute("data-cfx-role", "wardley-pipeline-component").Attribute("cx", F(x)).Attribute("cy", F(parent.Y)).Attribute("r", 4.5).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.Axis.ToCss()).EndEmptyElement().Line();
            WriteFittedBodyText(writer, child.Label, x + 6, parent.Y + 14, 82, 16, TextAlignment.Left, theme.MutedText, theme, 9.5, 600);
        }

        writer.StartElement("rect").Attribute("data-cfx-role", "wardley-pipeline").Attribute("x", F(minX - 14)).Attribute("y", F(parent.Y - 22)).Attribute("width", F(maxX - minX + 28)).Attribute("height", 44).Attribute("rx", 7).Attribute("fill", "none").Attribute("stroke", ChartColorMath.WithOpacity(theme.Axis, VisualBlockRendering.WardleyPipelineOpacity).ToCss()).Attribute("stroke-width", 1).Attribute("stroke-dasharray", "4 4").EndEmptyElement().Line();
    }

    private static void WriteWardleyNode(SvgMarkupWriter writer, VisualBlockRendering.WardleyNodePlacement placement, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        var node = placement.Node;
        var color = node.Kind == WardleyMapNodeKind.Anchor ? theme.Text : VisualBlockRendering.PaletteAt(theme, placement.Index);
        var radius = node.Kind == WardleyMapNodeKind.Anchor ? 7.5 : 6;
        if (!string.IsNullOrWhiteSpace(node.Strategy)) writer.StartElement("circle").Attribute("data-cfx-role", "wardley-strategy").Attribute("cx", F(placement.X)).Attribute("cy", F(placement.Y)).Attribute("r", F(radius + 7)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", ChartColorMath.WithOpacity(color, VisualBlockRendering.WardleyAccentOpacity).ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line();
        writer.StartElement("circle").Attribute("data-cfx-role", node.Kind == WardleyMapNodeKind.Anchor ? "wardley-anchor" : "wardley-node").Attribute("cx", F(placement.X)).Attribute("cy", F(placement.Y)).Attribute("r", F(radius)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", color.ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
        if (node.Inertia) writer.StartElement("line").Attribute("data-cfx-role", "wardley-inertia").Attribute("x1", F(placement.X + 16)).Attribute("y1", F(placement.Y - 9)).Attribute("x2", F(placement.X + 16)).Attribute("y2", F(placement.Y + 9)).Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 4).EndEmptyElement().Line();
        var labelX = placement.X + (node.LabelOffsetX ?? (node.Kind == WardleyMapNodeKind.Anchor ? -45 : 10));
        var labelY = placement.Y + (node.LabelOffsetY ?? -10);
        WriteFittedBodyText(writer, node.Label, labelX, labelY, 120, 20, node.Kind == WardleyMapNodeKind.Anchor ? TextAlignment.Center : TextAlignment.Left, theme.Text, theme, 10.5, node.Kind == WardleyMapNodeKind.Anchor ? 800 : 600);
    }

    private static void WriteWardleyNote(SvgMarkupWriter writer, WardleyMapNote note, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ResolvedVisualTheme theme) =>
        WriteFittedBodyText(writer, note.Text, VisualBlockRendering.ProjectWardleyX(layout.Plot, note.Evolution), VisualBlockRendering.ProjectWardleyY(layout.Plot, note.Visibility), 150, 24, TextAlignment.Left, theme.MutedText, theme, 10.5, 700);

    private static void WriteWardleyAnnotation(SvgMarkupWriter writer, WardleyMapAnnotation annotation, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, annotation.Evolution);
        var y = VisualBlockRendering.ProjectWardleyY(layout.Plot, annotation.Visibility);
        var radius = VisualBlockRendering.WardleyAnnotationRadius(theme);
        writer.StartElement("g").Attribute("data-cfx-role", "wardley-annotation-callout").EndStartElement().Line();
        writer.StartElement("circle").Attribute("data-cfx-role", "wardley-annotation").Attribute("cx", F(x)).Attribute("cy", F(y)).Attribute("r", F(radius)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.Axis.ToCss()).EndEmptyElement().Line();
        WriteFittedBodyText(writer, annotation.Number.ToString(System.Globalization.CultureInfo.InvariantCulture), x - radius * 0.8, y + radius * 0.4, radius * 1.6, radius * 1.6, TextAlignment.Center, theme.Text, theme, 9, 800, "wardley-annotation-number");
        if (annotation.Text.Length > 0) WriteFittedBodyText(writer, annotation.Text, x + radius + 3, y - 4, 150, 20, TextAlignment.Left, theme.MutedText, theme, 9, 600);
        writer.EndElement().Line();
    }

    private static void WriteWardleyMarker(SvgMarkupWriter writer, WardleyMapMarker marker, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, marker.Evolution);
        var y = VisualBlockRendering.ProjectWardleyY(layout.Plot, marker.Visibility);
        var color = marker.Kind == WardleyMapMarkerKind.Accelerator ? VisualBlockRendering.PaletteAt(theme, 1) : VisualBlockRendering.PaletteAt(theme, 3);
        var d = VisualBlockRendering.WardleyMarkerPath(x, y, marker.Kind);
        writer.StartElement("path").Attribute("data-cfx-role", marker.Kind == WardleyMapMarkerKind.Accelerator ? "wardley-accelerator" : "wardley-deaccelerator").Attribute("d", d).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", color.ToCss()).EndEmptyElement().Line();
        WriteFittedBodyText(writer, marker.Label, x - 24, y + 25, 98, 20, TextAlignment.Center, theme.Text, theme, 9, 700);
    }
}
