using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    // This is a transient map paint layout, not another observation collection. Every contributor still has its own source group.
    internal sealed class MapCluster {
        internal MapCluster(string key, ChartPoint center, int[] sources) { Key = key; Center = center; Sources = sources; }
        internal string Key { get; } internal ChartPoint Center { get; } internal int[] Sources { get; }
    }

    private static void DrawMapClusters(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, MapDataset data,
        ChartRect plot, VisualThemeColors colors, List<LabelObstacle> obstacles, double dot) {
        var policy = data.Series.MapClustering;
        if (policy == null) return;
        var (color, paint) = DottedPaint(data.Series, null, colors);
        foreach (var cluster in data.Clusters) {
            var id = data.Prefix + "-map-cluster-" + cluster.Key;
            var count = cluster.Sources.Length; var text = count.ToString(CultureInfo.InvariantCulture);
            var full = chart.Options.Labels.SampleCount + ": " + text + "; "
                + string.Join("; ", cluster.Sources.Select(source => data.Series.MapPoints[source].Label));
            var radius = policy.Radius;
            var bounds = new ChartRect(cluster.Center.X - radius, cluster.Center.Y - radius, radius * 2, radius * 2);
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = N(data.Index), ["data-cfx-point"] = "-1", ["data-cfx-derived"] = "map-grid-cluster",
                ["data-cfx-derived-identity"] = "map-grid-cluster:" + cluster.Key,
                ["data-cfx-source-points"] = string.Join(",", cluster.Sources.Select(source => N(source))), ["data-cfx-source-count"] = N(count),
                ["data-cfx-value"] = text, ["data-cfx-formatted-value"] = text, ["data-cfx-label-value"] = chart.Options.Labels.SampleCount,
                ["data-cfx-label"] = full, ["data-cfx-visible"] = "true", ["data-cfx-cluster-grid"] = cluster.Key,
                ["data-cfx-cluster-cell-size"] = N(policy.CellSize), ["data-cfx-cluster-radius"] = N(radius),
                ["data-cfx-cluster-center-x"] = N(cluster.Center.X), ["data-cfx-cluster-center-y"] = N(cluster.Center.Y), ["aria-label"] = full
            };
            builder.AddRegion(new VisualSemanticRegion(id, "dotted-map-cluster", Intersect(bounds, plot), full));
            using (builder.PushGroup(id, "dotted-map-cluster-source", metadata)) {
                builder.Ellipse(cluster.Center.X, cluster.Center.Y, radius, radius, color, colors.Surface, Math.Max(1, dot * .55),
                    "dotted-map-cluster", paint: new VisualScenePaintBinding(paint, SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                var style = MapDataStyle(chart, context, -1, ChartColorMath.AccessibleTextOnBackground(color), data.Series);
                VisualRadialPrimitives.Text(builder, text, bounds, style, "dotted-map-cluster-count", id + "-count", SvgPaint.Literal(style.Color));
            }
            obstacles.Add(new LabelObstacle(id, bounds));
        }
    }
}
