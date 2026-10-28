using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    private static void Sunburst(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors, ChartHierarchySurface surface) {
        var series = chart.Series[0];
        var areas = HierarchyScaleLayout(chart, context, builder, plot, surface);
        var model = ChartSunburstLayout.Compute(chart, areas.Content);
        var total = model.Nodes[model.Root].Value;
        var cornerRadius = chart.Options.Sunburst.CornerRadius;
        var sectorGap = chart.Options.Sunburst.SectorGap;
        var frameSurface = HierarchyCaptionSurface.Frame(context);
        if (total == 0) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.no-data", "The Sunburst has no positive sizes."));
        foreach (var node in model.Nodes.OrderByDescending(node => node.Depth)) {
            var item = series.HierarchyItems[node.Index];
            var sweep = node.EndAngle - node.StartAngle;
            var geometry = SunburstPaintGeometry(node, sectorGap);
            var visible = (chart.HierarchyPreparation?.Contains(node.Index) ?? true) && node.Value > 0 && geometry.Sweep > 0 && !geometry.PaddingCollapsed && (sectorGap == 0 && cornerRadius == 0
                || ChartSlicePathGeometry.HasEncodedFillArea(model.CenterX, model.CenterY, geometry.OuterRadius, geometry.InnerRadius, geometry.StartAngle, geometry.Sweep, cornerRadius));
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
            var branchRoot = chart.HierarchyPreparation?.BranchRoot(node.Index) ?? (chart.HierarchyPreparation == null ? series.Relationships!.HierarchyBranchRoot(node.Index) : null);
            if (branchRoot != null) metadata["data-cfx-branch-root"] = branchRoot;
            if (item.Value.HasValue) metadata["data-cfx-authored-value"] = N(item.Value.Value);
            if (item.ColorValue.HasValue) {
                metadata["data-cfx-color-value"] = N(item.ColorValue.Value);
                if (surface.Presentation.HasFormat) metadata["data-cfx-formatted-color-value"] = surface.Presentation.Value(item.ColorValue.Value);
            }
            else if (surface.Scale != null) metadata["data-cfx-color-missing"] = "true";
            if (node.Children.Count > 0) metadata["data-cfx-remainder-value"] = N(node.RemainderValue);
            metadata["data-cfx-geometry-status"] = visible ? "visible" : node.Value == 0 ? "zero" : geometry.PaddingCollapsed ? "padding-collapse" : "precision-collapse";
            if (sectorGap > 0) {
                metadata["data-cfx-sector-gap"] = N(sectorGap);
                metadata["data-cfx-paint-start-angle"] = N(geometry.StartAngle); metadata["data-cfx-paint-sweep"] = N(geometry.Sweep);
                metadata["data-cfx-paint-inner-radius"] = N(geometry.InnerRadius); metadata["data-cfx-paint-outer-radius"] = N(geometry.OuterRadius);
                if (!visible) { metadata["data-cfx-visible"] = "false"; metadata["data-cfx-passive"] = "true"; }
            }
            ApplyHierarchyViewMetadata(metadata, chart.HierarchyPreparation, node.Index);
            if (total > 0 && (chart.HierarchyPreparation?.Contains(node.Index) ?? true)) metadata["data-cfx-percent"] = N(node.Value / total);
            metadata["data-cfx-start-angle"] = N(node.StartAngle); metadata["data-cfx-sweep"] = N(sweep);
            metadata["data-cfx-inner-radius"] = N(node.InnerRadius); metadata["data-cfx-outer-radius"] = N(node.OuterRadius);
            var id = ChartRelationshipMetadata.SourceId("node", node.Id);
            using (builder.PushGroup(id, "sunburst-segment", metadata)) if (visible) {
                var explicitStroke = node.Index < series.PointStrokeColors.Count ? series.PointStrokeColors[node.Index] : null;
                var stateStroke = surface.Scale != null && state != ChartSeriesState.None;
                var stroke = explicitStroke ?? (stateStroke ? ChartSeriesColours.State(state, colors, colors.Foreground) : colors.Surface);
                builder.Slice(model.CenterX, model.CenterY, geometry.OuterRadius, geometry.InnerRadius, geometry.StartAngle, geometry.Sweep, color, stroke,
                    explicitStroke.HasValue ? Math.Min(series.StrokeWidth, (geometry.OuterRadius - geometry.InnerRadius) / 2) : stateStroke ? 2 : 1, "sunburst-segment-mark",
                    paint: new VisualScenePaintBinding(paint, SvgPaint.Of(stroke, explicitStroke.HasValue ? SvgColorRole.Series : stateStroke ? SvgColorRole.Status : SvgColorRole.Surface)), cornerRadius: cornerRadius);
                var pattern = Pattern(series, node.Index);
                if (pattern != ChartFillPattern.None) builder.PatternSlice(model.CenterX, model.CenterY, geometry.OuterRadius, geometry.InnerRadius, geometry.StartAngle, geometry.Sweep, pattern,
                    ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(90), role: "sunburst-pattern", cornerRadius: cornerRadius);
                if (series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false)) SunburstLabel(chart, context, builder, model, node, geometry, color, paint, secondary, color.A == 255 ? null : frameSurface.Over(color, paint));
            }
            var bounds = visible ? sectorGap > 0
                ? ChartSlicePathGeometry.Bounds(model.CenterX, model.CenterY, geometry.OuterRadius, geometry.InnerRadius, geometry.StartAngle, geometry.Sweep)
                : new ChartRect(model.CenterX - node.OuterRadius, model.CenterY - node.OuterRadius, node.OuterRadius * 2, node.OuterRadius * 2)
                : new ChartRect(model.CenterX, model.CenterY, 0, 0);
            var description = full + ": " + formatted;
            if (secondary != null) description += "; " + secondary;
            if (node.Children.Count > 0 && item.Value.HasValue && item.Value.Value != node.Value)
                description += "; " + chart.Options.Labels.AuthoredValue + ": " + ChartNumericFormatter.FormatValue(chart.Options, item.Value.Value);
            if (item.ColorValue.HasValue) description += "; " + surface.Title + ": " + surface.Presentation.Value(item.ColorValue.Value);
            else if (surface.Scale != null) description += "; " + surface.Presentation.NoData;
            if (node.RemainderValue > 0) description += "; " + chart.Options.Labels.Remainder + ": " + ChartNumericFormatter.FormatValue(chart.Options, node.RemainderValue);
            builder.AddRegion(new VisualSemanticRegion(id, "sunburst-segment", bounds, description));
        }
        DrawHierarchyScale(chart, context, builder, areas.Scale, surface, colors);
    }
}
