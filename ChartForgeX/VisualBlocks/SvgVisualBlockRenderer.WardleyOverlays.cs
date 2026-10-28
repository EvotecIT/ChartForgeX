using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void WriteWardleySourcing(SvgMarkupWriter writer, VisualBlockRendering.WardleyNodePlacement placement,
        double radius, ChartColor color, ResolvedVisualTheme theme) {
        var strategy = placement.Node.Strategy;
        if (string.IsNullOrWhiteSpace(strategy)) return;
        // Keep the incumbent outer ring and its alpha; the inner mark distinguishes the authored strategy.
        if (strategy != "market") {
            var opacity = strategy == "build" ? .10 : strategy == "buy" ? .30 : .65;
            writer.StartElement("circle").Attribute("data-cfx-role", "wardley-sourcing-" + strategy)
                .Attribute("cx", F(placement.X)).Attribute("cy", F(placement.Y)).Attribute("r", F(radius + 3))
                .Attribute("fill", ChartColorMath.WithOpacity(color, opacity).ToCss()).EndEmptyElement().Line();
            return;
        }
        var points = new List<ChartPoint>();
        for (var index = 0; index < 3; index++) {
            var angle = -Math.PI / 2 + index * Math.PI * 2 / 3;
            points.Add(new ChartPoint(placement.X + Math.Cos(angle) * (radius + 3), placement.Y + Math.Sin(angle) * (radius + 3)));
        }
        var path = "M " + F(points[0].X) + " " + F(points[0].Y) + " L " + F(points[1].X) + " " + F(points[1].Y) + " L " + F(points[2].X) + " " + F(points[2].Y) + " Z";
        writer.StartElement("path").Attribute("data-cfx-role", "wardley-sourcing-market").Attribute("d", path)
            .Attribute("fill", "none").Attribute("stroke", color.ToCss()).EndEmptyElement().Line();
        foreach (var point in points) writer.StartElement("circle").Attribute("data-cfx-role", "wardley-market-member")
            .Attribute("cx", F(point.X)).Attribute("cy", F(point.Y)).Attribute("r", 2.2).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
    }

    private static void WriteWardleyStyledPipeline(SvgMarkupWriter writer, WardleyMapPipeline pipeline,
        VisualBlockRendering.WardleyNodePlacement parent, VisualBlockRendering.WardleyMapLayout layout, ResolvedVisualTheme theme, WardleyMapStyle? style) {
        var children = pipeline.Components.Select((child, index) => new { Child = child, Index = index,
            X = VisualBlockRendering.ProjectWardleyX(layout.Plot, child.Evolution) }).ToArray();
        var stroke = pipeline.Outline;
        var color = ChartColorMath.WithOpacity(stroke?.Color ?? style?.ComponentStroke ?? style?.AxisColor ?? theme.Axis,
            VisualBlockRendering.WardleyPipelineOpacity * (stroke?.Opacity ?? 1));
        var minX = children.Min(child => child.X);
        var maxX = children.Max(child => child.X);
        if (pipeline.ShowEvolutionLinks) {
            var ordered = children.OrderBy(child => child.X).ThenBy(child => child.Index).ToArray();
            for (var index = 1; index < ordered.Length; index++) writer.StartElement("line")
                .Attribute("data-cfx-role", "wardley-pipeline-evolution").Attribute("x1", F(ordered[index - 1].X)).Attribute("y1", F(parent.Y))
                .Attribute("x2", F(ordered[index].X)).Attribute("y2", F(parent.Y)).Attribute("stroke", (style?.LinkStroke ?? color).ToCss())
                .Attribute("stroke-width", stroke?.Width ?? 1).Attribute("stroke-dasharray", "4 4").EndEmptyElement().Line();
        }
        foreach (var item in children) {
            writer.StartElement("circle").Attribute("data-cfx-role", "wardley-pipeline-component")
                .Attribute("cx", F(item.X)).Attribute("cy", F(parent.Y)).Attribute("r", 4.5)
                .Attribute("fill", (style?.ComponentFill ?? theme.PlotBackground).ToCss()).Attribute("stroke", (style?.ComponentStroke ?? theme.Axis).ToCss());
            if (item.Child.Id != null) writer.Attribute("data-node-id", item.Child.Id);
            writer.EndEmptyElement().Line();
            WriteFittedBodyText(writer, item.Child.Label, item.X + (item.Child.LabelOffsetX ?? 6), parent.Y + (item.Child.LabelOffsetY ?? 14),
                120, 20, TextAlignment.Left, style?.ComponentLabelColor ?? theme.MutedText, theme, 9.5, 600, "wardley-pipeline-label");
        }
        writer.StartElement("rect").Attribute("data-cfx-role", "wardley-pipeline").Attribute("x", F(minX - 14)).Attribute("y", F(parent.Y - 22))
            .Attribute("width", F(maxX - minX + 28)).Attribute("height", 44).Attribute("rx", 7).Attribute("fill", "none")
            .Attribute("stroke", color.ToCss()).Attribute("stroke-width", stroke?.Width ?? 1);
        var dash = stroke?.Dash;
        if (dash == null) writer.Attribute("stroke-dasharray", "4 4");
        else if (dash.Count > 0) writer.Attribute("stroke-dasharray", string.Join(" ", dash.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture))));
        writer.EndEmptyElement().Line();
    }

    private static void WriteWardleyAnnotationBox(SvgMarkupWriter writer, WardleyMapBlock map,
        VisualBlockRendering.WardleyMapLayout scene, ResolvedVisualTheme theme) {
        var count = map.Annotations.Count(annotation => annotation.Text.Length > 0);
        if (count == 0) return;
        var origin = map.AnnotationsBox!;
        var width = Math.Min(320, scene.Plot.Width); const double padding = 10;
        if (width <= padding * 2) throw new InvalidOperationException("Wardley annotation legends require a wider native frame.");
        var textStyle = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = 600 }, FontSize = VisualBlockRendering.BodyFontSize(theme, 10), LineHeight = 1.2 };
        // Bound layout/sort allocations by the actual frame and native font before measuring every entry.
        var minimumEntry = TextLayoutEngine.Measure("Mg", textStyle).Height;
        if (count > (scene.Plot.Height - padding * 2 + 6) / (minimumEntry + 6))
            throw new InvalidOperationException("Wardley annotation legend text does not fit the native frame; increase its width or height.");
        var entries = map.Annotations.Select((annotation, index) => new { Annotation = annotation, Index = index })
            .Where(entry => entry.Annotation.Text.Length > 0).OrderBy(entry => entry.Annotation.Number).ThenBy(entry => entry.Index).ToArray();
        var layouts = entries.Select(entry => TextLayoutEngine.Layout(entry.Annotation.Number.ToString(CultureInfo.InvariantCulture) + ". " + entry.Annotation.Text,
            width - padding * 2, textStyle, TextWrapMode.Word)).ToArray();
        var height = padding * 2 + layouts.Sum(layout => layout.Metrics.Height) + Math.Max(0, layouts.Length - 1) * 6;
        if (double.IsNaN(height) || double.IsInfinity(height) || height > scene.Plot.Height || layouts.Any(layout => layout.Trimmed))
            throw new InvalidOperationException("Wardley annotation legend text does not fit the native frame; increase its width or height.");
        var x = Math.Max(scene.Plot.Left, Math.Min(scene.Plot.Right - width, VisualBlockRendering.ProjectWardleyX(scene.Plot, origin.Evolution)));
        var y = Math.Max(scene.Plot.Top, Math.Min(scene.Plot.Bottom - height, VisualBlockRendering.ProjectWardleyY(scene.Plot, origin.Visibility)));
        writer.StartElement("g").Attribute("data-cfx-role", "wardley-annotation-box").EndStartElement().Line();
        writer.StartElement("rect").Attribute("x", F(x)).Attribute("y", F(y)).Attribute("width", F(width)).Attribute("height", F(height)).Attribute("rx", 4)
            .Attribute("fill", (map.Style?.AnnotationFill ?? theme.PlotBackground).ToCss()).Attribute("stroke", (map.Style?.AnnotationStroke ?? theme.Axis).ToCss()).EndEmptyElement().Line();
        var top = y + padding;
        for (var index = 0; index < layouts.Length; index++) {
            var annotation = entries[index].Annotation;
            writer.StartElement("g").Attribute("data-cfx-role", "wardley-annotation-legend-entry").Attribute("data-annotation-number", annotation.Number).EndStartElement();
            writer.StartElement("title").Text(annotation.Text).EndElement();
            var baseline = top + VisualBlockRendering.TextAscent(textStyle);
            foreach (var line in layouts[index].Lines) {
                writer.StartElement("text").Attribute("data-cfx-role", "wardley-annotation-legend-text").Attribute("x", F(x + padding)).Attribute("y", F(baseline))
                    .Attribute("fill", (map.Style?.AnnotationTextColor ?? theme.Text).ToCss()).Attribute("font-family", theme.FontFamily)
                    .Attribute("font-size", F(textStyle.FontSize)).Attribute("font-weight", 600).Text(line.Text).EndElement();
                baseline += layouts[index].Metrics.LineHeight;
            }
            writer.EndElement().Line(); top += layouts[index].Metrics.Height + 6;
        }
        writer.EndElement().Line();
    }
}
