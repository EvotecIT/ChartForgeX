using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderWardleyMap(SvgMarkupWriter writer, WardleyMapBlock map) {
        var options = map.Options;
        var theme = options.Theme;
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

    private static void WriteWardleyAxes(SvgMarkupWriter writer, WardleyMapBlock map, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ChartTheme theme) {
        var plot = layout.Plot;
        writer.StartElement("rect").Attribute("data-cfx-role", "wardley-plot").Attribute("x", F(plot.X)).Attribute("y", F(plot.Y)).Attribute("width", F(plot.Width)).Attribute("height", F(plot.Height)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        for (var index = 1; index < 4; index++) {
            var x = plot.X + plot.Width * index / 4.0;
            writer.StartElement("line").Attribute("data-cfx-role", "wardley-stage-boundary").Attribute("x1", F(x)).Attribute("y1", F(plot.Y)).Attribute("x2", F(x)).Attribute("y2", F(plot.Bottom)).Attribute("stroke", theme.Grid.ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line();
        }

        writer.StartElement("line").Attribute("data-cfx-role", "wardley-axis-x").Attribute("x1", F(plot.X)).Attribute("y1", F(plot.Bottom)).Attribute("x2", F(plot.Right)).Attribute("y2", F(plot.Bottom)).Attribute("stroke", theme.Axis.ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
        writer.StartElement("line").Attribute("data-cfx-role", "wardley-axis-y").Attribute("x1", F(plot.X)).Attribute("y1", F(plot.Y)).Attribute("x2", F(plot.X)).Attribute("y2", F(plot.Bottom)).Attribute("stroke", theme.Axis.ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
        WriteText(writer, "Visibility", Math.Max(0, plot.X - 54), plot.Y + plot.Height * 0.50, 48, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 1), "700");
        WriteText(writer, "Evolution", plot.X + plot.Width * 0.38, plot.Bottom + 38, plot.Width * 0.24, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 1), "700");
        var stages = map.Stages.Count == 0 ? new[] { "Genesis", "Custom", "Product", "Commodity" } : map.Stages;
        for (var index = 0; index < stages.Count; index++) {
            var x = plot.X + plot.Width * (index + 0.5) / stages.Count;
            WriteText(writer, stages[index], x - plot.Width / stages.Count / 2 + 4, plot.Bottom + 18, plot.Width / stages.Count - 8, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(8, theme.SubtitleFontSize - 2), "600");
        }
    }

    private static void WriteWardleyLink(SvgMarkupWriter writer, WardleyMapLink link, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ChartTheme theme) {
        if (!layout.NodeLookup.TryGetValue(link.FromId, out var from) || !layout.NodeLookup.TryGetValue(link.ToId, out var to)) return;
        var color = theme.Axis.WithAlpha(155);
        writer.StartElement("line").Attribute("data-cfx-role", "wardley-link").Attribute("x1", F(from.X)).Attribute("y1", F(from.Y)).Attribute("x2", F(to.X)).Attribute("y2", F(to.Y)).Attribute("stroke", color.ToCss()).Attribute("stroke-width", 1.2);
        if (link.Dashed) writer.Attribute("stroke-dasharray", "5 5");
        writer.EndEmptyElement().Line();
        WriteWardleyFlowHint(writer, link, from.X, from.Y, to.X, to.Y, color);
        if (link.Label.Length > 0) WriteText(writer, link.Label, (from.X + to.X) / 2 - 50, (from.Y + to.Y) / 2 - 5, 100, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(8, theme.SubtitleFontSize - 3), "600");
    }

    private static void WriteWardleyFlowHint(SvgMarkupWriter writer, WardleyMapLink link, double fromX, double fromY, double toX, double toY, ChartColor color) {
        if (link.Flow == WardleyMapFlow.None) return;
        var dx = toX - fromX;
        var dy = toY - fromY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.000001) return;
        var ux = dx / length;
        var uy = dy / length;
        var midX = (fromX + toX) / 2;
        var midY = (fromY + toY) / 2;
        if (link.Flow == WardleyMapFlow.Forward || link.Flow == WardleyMapFlow.Bidirectional) WriteWardleyFlowArrow(writer, midX + ux * 9, midY + uy * 9, ux, uy, color, "wardley-flow-forward");
        if (link.Flow == WardleyMapFlow.Backward || link.Flow == WardleyMapFlow.Bidirectional) WriteWardleyFlowArrow(writer, midX - ux * 9, midY - uy * 9, -ux, -uy, color, "wardley-flow-backward");
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

    private static void WriteWardleyEvolution(SvgMarkupWriter writer, WardleyMapEvolution evolution, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ChartTheme theme) {
        if (!layout.NodeLookup.TryGetValue(evolution.NodeId, out var from)) return;
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, evolution.TargetEvolution);
        writer.StartElement("line").Attribute("data-cfx-role", "wardley-evolve").Attribute("x1", F(from.X)).Attribute("y1", F(from.Y)).Attribute("x2", F(x)).Attribute("y2", F(from.Y)).Attribute("stroke", VisualBlockRendering.PaletteAt(theme, 2).ToCss()).Attribute("stroke-width", 1.8).Attribute("stroke-dasharray", "4 4").EndEmptyElement().Line();
        writer.StartElement("path").Attribute("data-cfx-role", "wardley-evolve-arrow").Attribute("d", "M " + F(x) + " " + F(from.Y) + " l -7 -4 v 8 z").Attribute("fill", VisualBlockRendering.PaletteAt(theme, 2).ToCss()).EndEmptyElement().Line();
    }

    private static void WriteWardleyPipeline(SvgMarkupWriter writer, WardleyMapPipeline pipeline, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ChartTheme theme) {
        if (!layout.NodeLookup.TryGetValue(pipeline.ParentId, out var parent) || pipeline.Components.Count == 0) return;
        var minX = parent.X;
        var maxX = parent.X;
        foreach (var child in pipeline.Components) {
            var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, child.Evolution);
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            writer.StartElement("circle").Attribute("data-cfx-role", "wardley-pipeline-component").Attribute("cx", F(x)).Attribute("cy", F(parent.Y)).Attribute("r", 4.5).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.Axis.ToCss()).EndEmptyElement().Line();
            WriteText(writer, child.Label, x + 6, parent.Y + 14, 82, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(8, theme.SubtitleFontSize - 3), "600");
        }

        writer.StartElement("rect").Attribute("data-cfx-role", "wardley-pipeline").Attribute("x", F(minX - 14)).Attribute("y", F(parent.Y - 22)).Attribute("width", F(maxX - minX + 28)).Attribute("height", 44).Attribute("rx", 7).Attribute("fill", "none").Attribute("stroke", theme.Axis.WithAlpha(145).ToCss()).Attribute("stroke-width", 1).Attribute("stroke-dasharray", "4 4").EndEmptyElement().Line();
    }

    private static void WriteWardleyNode(SvgMarkupWriter writer, VisualBlockRendering.WardleyNodePlacement placement, ChartForgeX.Themes.ChartTheme theme) {
        var node = placement.Node;
        var color = node.Kind == WardleyMapNodeKind.Anchor ? theme.Text : VisualBlockRendering.PaletteAt(theme, placement.Index);
        var radius = node.Kind == WardleyMapNodeKind.Anchor ? 7.5 : 6;
        if (!string.IsNullOrWhiteSpace(node.Strategy)) writer.StartElement("circle").Attribute("data-cfx-role", "wardley-strategy").Attribute("cx", F(placement.X)).Attribute("cy", F(placement.Y)).Attribute("r", F(radius + 7)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", color.WithAlpha(155).ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line();
        writer.StartElement("circle").Attribute("data-cfx-role", node.Kind == WardleyMapNodeKind.Anchor ? "wardley-anchor" : "wardley-node").Attribute("cx", F(placement.X)).Attribute("cy", F(placement.Y)).Attribute("r", F(radius)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", color.ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
        if (node.Inertia) writer.StartElement("line").Attribute("data-cfx-role", "wardley-inertia").Attribute("x1", F(placement.X + 16)).Attribute("y1", F(placement.Y - 9)).Attribute("x2", F(placement.X + 16)).Attribute("y2", F(placement.Y + 9)).Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 4).EndEmptyElement().Line();
        var labelX = placement.X + (node.LabelOffsetX ?? (node.Kind == WardleyMapNodeKind.Anchor ? -45 : 10));
        var labelY = placement.Y + (node.LabelOffsetY ?? -10);
        WriteText(writer, node.Label, labelX, labelY, 120, node.Kind == WardleyMapNodeKind.Anchor ? VisualTextAlignment.Center : VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(8.5, theme.SubtitleFontSize - 2), node.Kind == WardleyMapNodeKind.Anchor ? "800" : "600");
    }

    private static void WriteWardleyNote(SvgMarkupWriter writer, WardleyMapNote note, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ChartTheme theme) =>
        WriteText(writer, note.Text, VisualBlockRendering.ProjectWardleyX(layout.Plot, note.Evolution), VisualBlockRendering.ProjectWardleyY(layout.Plot, note.Visibility), 150, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(8, theme.SubtitleFontSize - 2), "700");

    private static void WriteWardleyAnnotation(SvgMarkupWriter writer, WardleyMapAnnotation annotation, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ChartTheme theme) {
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, annotation.Evolution);
        var y = VisualBlockRendering.ProjectWardleyY(layout.Plot, annotation.Visibility);
        writer.StartElement("circle").Attribute("data-cfx-role", "wardley-annotation").Attribute("cx", F(x)).Attribute("cy", F(y)).Attribute("r", 10).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.Axis.ToCss()).EndEmptyElement().Line();
        WriteText(writer, annotation.Number.ToString(System.Globalization.CultureInfo.InvariantCulture), x - 8, y + 4, 16, VisualTextAlignment.Center, theme.Text, theme.FontFamily, 9, "800");
        if (annotation.Text.Length > 0) WriteText(writer, annotation.Text, x + 13, y - 4, 150, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, 9, "600");
    }

    private static void WriteWardleyMarker(SvgMarkupWriter writer, WardleyMapMarker marker, VisualBlockRendering.WardleyMapLayout layout, ChartForgeX.Themes.ChartTheme theme) {
        var x = VisualBlockRendering.ProjectWardleyX(layout.Plot, marker.Evolution);
        var y = VisualBlockRendering.ProjectWardleyY(layout.Plot, marker.Visibility);
        var color = marker.Kind == WardleyMapMarkerKind.Accelerator ? VisualBlockRendering.PaletteAt(theme, 1) : VisualBlockRendering.PaletteAt(theme, 3);
        var direction = marker.Kind == WardleyMapMarkerKind.Accelerator ? 1 : -1;
        var d = direction > 0
            ? "M " + F(x) + " " + F(y - 10) + " h 32 v -7 l 17 17 l -17 17 v -7 h -32 z"
            : "M " + F(x + 49) + " " + F(y - 10) + " h -32 v -7 l -17 17 l 17 17 v -7 h 32 z";
        writer.StartElement("path").Attribute("data-cfx-role", marker.Kind == WardleyMapMarkerKind.Accelerator ? "wardley-accelerator" : "wardley-deaccelerator").Attribute("d", d).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", color.ToCss()).EndEmptyElement().Line();
        WriteText(writer, marker.Label, x - 24, y + 25, 98, VisualTextAlignment.Center, theme.Text, theme.FontFamily, 9, "700");
    }
}
