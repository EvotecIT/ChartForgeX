using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void AddBoxPlotDistributionMetadata(Chart chart, IDictionary<string, string> facts, ChartBoxPlotDistribution distribution) {
        facts["data-cfx-derived"] = "five-number-summary";
        facts["data-cfx-group-index"] = Number(distribution.Index);
        facts["data-cfx-source-count"] = Number(distribution.Count);
        facts["data-cfx-source-minimum"] = Number(distribution.SourceMinimum);
        facts["data-cfx-source-maximum"] = Number(distribution.SourceMaximum);
        facts["data-cfx-outlier-count"] = Number(distribution.OutlierSourceIndices.Count);
        facts["data-cfx-quartile-estimator"] = distribution.Estimator.ToString();
        facts["data-cfx-whisker-policy"] = distribution.WhiskerPolicy.ToString();
        facts["data-cfx-iqr-multiplier"] = Number(distribution.IqrMultiplier);
        if (distribution.InlierMinimum.HasValue) facts["data-cfx-inlier-minimum"] = Number(distribution.InlierMinimum.Value);
        if (distribution.InlierMaximum.HasValue) facts["data-cfx-inlier-maximum"] = Number(distribution.InlierMaximum.Value);
        facts["data-cfx-label-lower-whisker"] = chart.Options.Labels.LowerWhisker;
        facts["data-cfx-label-upper-whisker"] = chart.Options.Labels.UpperWhisker;
        facts["data-cfx-label-sample-count"] = chart.Options.Labels.SampleCount;
        facts["data-cfx-label-source-minimum"] = chart.Options.Labels.SourceMinimum;
        facts["data-cfx-label-source-maximum"] = chart.Options.Labels.SourceMaximum;
        facts["data-cfx-label-inlier-minimum"] = chart.Options.Labels.InlierMinimum;
        facts["data-cfx-label-inlier-maximum"] = chart.Options.Labels.InlierMaximum;
        facts["data-cfx-label-outlier-count"] = chart.Options.Labels.OutlierCount;
        facts["data-cfx-label-quartile-estimator"] = chart.Options.Labels.QuartileEstimator;
        facts["data-cfx-label-whisker-policy"] = chart.Options.Labels.WhiskerPolicy;
        facts["data-cfx-label-iqr-multiplier"] = chart.Options.Labels.IqrMultiplier;
        facts["data-cfx-label-no-inliers"] = chart.Options.Labels.NoInliers;
    }

    private static void DrawBoxPlotOutliers(Chart chart, VisualSceneBuilder builder, ChartSeries series, int seriesIndex, int groupIndex,
        string category, Func<double, ChartPoint> project, ChartColor color, SvgPaint paint, List<LabelObstacle> obstacles) {
        if (groupIndex >= series.BoxPlotDistributions.Count) return;
        var distribution = series.BoxPlotDistributions[groupIndex];
        foreach (var source in distribution.OutlierSourceIndices) {
            var value = series.BoxPlotSourceSamples[source]; var center = project(value);
            var point = series.BoxPlots.Count + source; var id = PointId(seriesIndex, point);
            var bounds = series.Markers.CustomPath == null
                ? VisualMarkerScene.Bounds(center.X, center.Y, VisualMarkerScene.Extent(series, 4.5))
                : VisualMarkerScene.Bounds(series, center.X, center.Y, 4.5);
            var formatted = Value(chart, value);
            var description = series.Name + ": " + category + " " + formatted;
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = Number(seriesIndex), ["data-cfx-point"] = Number(point),
                ["data-cfx-source-point"] = Number(source), ["data-cfx-source-count"] = "1",
                ["data-cfx-group-index"] = Number(groupIndex), ["data-cfx-kind"] = "BoxPlotOutlier",
                ["data-cfx-x"] = Number(distribution.Summary.X), ["data-cfx-y"] = Number(value),
                ["data-cfx-value"] = Number(value), ["data-cfx-label"] = formatted, ["data-cfx-formatted-value"] = formatted,
                ["data-cfx-label-outlier"] = chart.Options.Labels.Outlier,
                ["data-cfx-category-label"] = category, ["data-cfx-label-category"] = chart.Options.Labels.Category,
                ["data-cfx-axis"] = series.ValueAxis.ToString().ToLowerInvariant(), ["aria-label"] = description
            };
            builder.AddRegion(new VisualSemanticRegion(id, "point", bounds, description));
            using (builder.PushGroup(id, "point", metadata))
                VisualMarkerScene.Draw(builder, series, groupIndex, center.X, center.Y, 4.5, color, paint, "boxplot-outlier");
            obstacles.Add(new LabelObstacle(id, bounds));
        }
    }
}
