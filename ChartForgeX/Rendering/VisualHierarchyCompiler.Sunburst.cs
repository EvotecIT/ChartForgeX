using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    private static void Sunburst(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0]; var surface = new ChartHierarchySurface(chart, colors);
        var areas = HierarchyScaleLayout(chart, context, builder, plot, surface);
        var model = ChartSunburstLayout.Compute(chart, areas.Content);
        var total = model.Nodes[model.Root].Value;
        var cornerRadius = chart.Options.Sunburst.CornerRadius;
        if (total == 0) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.no-data", "The Sunburst has no positive sizes."));
        foreach (var node in model.Nodes.OrderByDescending(node => node.Depth)) {
            var item = series.HierarchyItems[node.Index];
            var sweep = node.EndAngle - node.StartAngle;
            var visible = node.Value > 0 && sweep > 0 && (cornerRadius == 0
                || ChartSlicePathGeometry.HasEncodedFillArea(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, sweep, cornerRadius));
            var blend = surface.Blend(node.Index, node.Depth == 0 ? 0 : node.Index + node.Depth - 1);
            var color = blend.Color; var paint = blend.Paint;
            var state = ChartRelationshipPaint.State(series, node.Index);
            if (surface.Scale == null && node.Depth == 0 && !ChartRelationshipPaint.HasExplicitColor(series, node.Index) && state == ChartSeriesState.None) {
                var source = color; color = ChartColorMath.Blend(colors.Surface, source, .28);
                paint = SvgPaint.Mix(color, colors.Surface, SvgColorRole.Surface, source, ChartRelationshipPaint.Role(series, node.Index), .28);
            }
            var formatted = ChartNumericFormatter.FormatValue(chart.Options, node.Value);
            var full = node.Index < series.PointLabels.Count && series.PointLabels[node.Index] != null ? series.PointLabels[node.Index]! : node.Label;
            var secondary = chart.Options.Sunburst.SecondaryLabelFormatter?.Invoke(new ChartSunburstLabelContext(item, node.Value, formatted));
            if (string.IsNullOrWhiteSpace(secondary)) secondary = null;
            var metadata = ChartRelationshipMetadata.Node(series, node.Id, node.Label, node.Index);
            metadata["data-cfx-full-label"] = full; metadata["data-cfx-state"] = state.ToString();
            if (secondary != null) metadata["data-cfx-secondary-label"] = secondary;
            metadata["data-cfx-depth"] = N(node.Depth); metadata["data-cfx-value"] = N(node.Value);
            metadata["data-cfx-formatted-value"] = formatted; metadata["data-cfx-leaf"] = node.Children.Count == 0 ? "true" : "false";
            if (item.ParentId != null) metadata["data-cfx-parent"] = item.ParentId;
            if (item.Value.HasValue) metadata["data-cfx-authored-value"] = N(item.Value.Value);
            if (item.ColorValue.HasValue) metadata["data-cfx-color-value"] = N(item.ColorValue.Value);
            else if (surface.Scale != null) metadata["data-cfx-color-missing"] = "true";
            if (node.Children.Count > 0) metadata["data-cfx-remainder-value"] = N(node.RemainderValue);
            metadata["data-cfx-geometry-status"] = visible ? "visible" : node.Value == 0 ? "zero" : "precision-collapse";
            if (total > 0) metadata["data-cfx-percent"] = N(node.Value / total);
            metadata["data-cfx-start-angle"] = N(node.StartAngle); metadata["data-cfx-sweep"] = N(sweep);
            metadata["data-cfx-inner-radius"] = N(node.InnerRadius); metadata["data-cfx-outer-radius"] = N(node.OuterRadius);
            var id = ChartRelationshipMetadata.SourceId("node", node.Id);
            using (builder.PushGroup(id, "sunburst-segment", metadata)) if (visible) {
                var stroke = surface.Scale != null && state != ChartSeriesState.None ? ChartSeriesColours.State(state, colors, colors.Foreground) : colors.Surface;
                builder.Slice(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, sweep, color, stroke,
                    surface.Scale != null && state != ChartSeriesState.None ? 2 : 1, "sunburst-segment-mark",
                    paint: new VisualScenePaintBinding(paint, SvgPaint.Of(stroke, surface.Scale != null && state != ChartSeriesState.None ? SvgColorRole.Status : SvgColorRole.Surface)), cornerRadius: cornerRadius);
                var pattern = Pattern(series, node.Index);
                if (pattern != ChartFillPattern.None) builder.PatternSlice(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, sweep, pattern,
                    ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "sunburst-pattern", cornerRadius: cornerRadius);
                if (series.ShowDataLabels ?? chart.Options.ShowDataLabels) SunburstLabel(chart, context, builder, model, node, color, paint, secondary);
            }
            var bounds = visible ? new ChartRect(model.CenterX - node.OuterRadius, model.CenterY - node.OuterRadius, node.OuterRadius * 2, node.OuterRadius * 2)
                : new ChartRect(model.CenterX, model.CenterY, 0, 0);
            var description = full + ": " + formatted;
            if (secondary != null) description += "; " + secondary;
            if (node.Children.Count > 0 && item.Value.HasValue && item.Value.Value != node.Value)
                description += "; " + chart.Options.Labels.AuthoredValue + ": " + ChartNumericFormatter.FormatValue(chart.Options, item.Value.Value);
            if (item.ColorValue.HasValue) description += "; " + (surface.Title ?? chart.Options.Labels.Color) + ": " + ChartNumericFormatter.FormatValue(chart.Options, item.ColorValue.Value);
            else if (surface.Scale != null) description += "; " + chart.Options.Labels.NoData;
            if (node.RemainderValue > 0) description += "; " + chart.Options.Labels.Remainder + ": " + ChartNumericFormatter.FormatValue(chart.Options, node.RemainderValue);
            builder.AddRegion(new VisualSemanticRegion(id, "sunburst-segment", bounds, description));
        }
        DrawHierarchyScale(chart, context, builder, areas.Scale, surface, colors);
    }
}
