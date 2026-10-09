using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    private static void Treemap(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0]; var index = series.Relationships!; var options = chart.Options.Treemap;
        var surface = new ChartTreemapSurface(chart, colors);
        var areas = TreemapScaleLayout(chart, context, builder, plot, surface);
        var layout = ChartTreemapLayout.Compute(series, areas.Content, options, item => {
            var style = LabelStyle(chart, context, surface.Blend(item).Color, item);
            return series.ShowDataLabels == false ? 0 : builder.MeasureText("Mg", style).Height + 8;
        });
        var tiles = layout.ToDictionary(tile => tile.ItemIndex);
        var paintOrder = layout.Select((tile, ordinal) => (tile.ItemIndex, ordinal)).ToDictionary(item => item.ItemIndex, item => item.ordinal);
        if (tiles.Count == 0) builder.AddDiagnostic(new VisualDiagnostic("hierarchy.no-data", "The treemap has no positive leaf sizes."));
        foreach (var root in Ordered(index.Roots)) Draw(root);
        DrawTreemapScale(chart, context, builder, areas.Scale, surface, colors);

        void Draw(int itemIndex) {
            var item = series.TreemapItems[itemIndex]; var children = index.Children(itemIndex);
            var value = index.HierarchyValues[itemIndex]; var formatted = ChartNumericFormatter.FormatValue(chart.Options, value);
            var full = itemIndex < series.PointLabels.Count && series.PointLabels[itemIndex] != null ? series.PointLabels[itemIndex]! : item.Label;
            var metadata = ChartRelationshipMetadata.Node(series, item.Id, item.Label, itemIndex);
            metadata["data-cfx-full-label"] = full; metadata["data-cfx-depth"] = N(index.Depths[itemIndex]);
            metadata["data-cfx-value"] = N(value); metadata["data-cfx-formatted-value"] = formatted;
            metadata["data-cfx-leaf"] = children.Count == 0 ? "true" : "false";
            if (item.ParentId != null) metadata["data-cfx-parent"] = item.ParentId;
            if (item.Value.HasValue) metadata["data-cfx-authored-value"] = N(item.Value.Value);
            if (item.ColorValue.HasValue) metadata["data-cfx-color-value"] = N(item.ColorValue.Value);
            else if (surface.Scale != null) metadata["data-cfx-color-missing"] = "true";
            var id = ChartRelationshipMetadata.SourceId("node", item.Id);
            var visible = tiles.TryGetValue(itemIndex, out var tile);
            var role = visible ? children.Count > 0 ? "treemap-group" : "treemap-tile" : value == 0 ? "treemap-zero-value" : "treemap-unpainted-value";
            var bounds = visible ? tile.Rect : new ChartRect(areas.Content.X, areas.Content.Y, 0, 0);
            using (builder.PushGroup(id, role, metadata)) {
                if (visible) {
                    var blend = surface.Blend(itemIndex);
                    builder.Rect(bounds, blend.Color, radius: Math.Min(context.Theme.BarRadius, Math.Min(bounds.Width, bounds.Height) * .1),
                        role: children.Count > 0 ? "treemap-group-mark" : "treemap-tile-mark", paint: VisualChartPaint.Fill(blend.Paint));
                    Pattern(builder, Rectangle(bounds), series, itemIndex, blend.Color, "treemap-pattern");
                    if (series.ShowDataLabels != false) {
                        if (children.Count == 0) Label(chart, context, builder, full + "\n" + formatted, bounds, blend.Color, itemIndex, "treemap-label", fillPaint: blend.Paint);
                        else if (options.ShowGroupLabels) Label(chart, context, builder, full + " · " + formatted, tile.HeaderRect,
                            blend.Color, itemIndex, "treemap-group-label", fillPaint: blend.Paint, insetLimit: 4, maximumLines: 1);
                    }
                    if (children.Count > 0) using (builder.PushClip(tile.ContentRect)) foreach (var child in Ordered(children)) Draw(child);
                } else foreach (var child in Ordered(children)) Draw(child);
            }
            var description = full + ": " + formatted;
            if (item.ColorValue.HasValue) description += "; " + (options.ColorLegendTitle ?? chart.Options.Labels.Color) + ": " + ChartNumericFormatter.FormatValue(chart.Options, item.ColorValue.Value);
            else if (surface.Scale != null) description += "; " + chart.Options.Labels.NoData;
            builder.AddRegion(new VisualSemanticRegion(id, role, bounds, description));
        }

        IEnumerable<int> Ordered(IReadOnlyList<int> siblings) => siblings.OrderBy(item => paintOrder.TryGetValue(item, out var rank) ? rank : int.MaxValue).ThenBy(item => item);
    }
}
