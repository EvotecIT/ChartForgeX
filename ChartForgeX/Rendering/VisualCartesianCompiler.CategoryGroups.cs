using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Owns one detached full-source membership and cell snapshot across refits and native windows.</summary>
    private sealed class CategoryGroupSource {
        private CategoryGroupSource(ChartCategoryGroupPlan plan, IReadOnlyList<ChartCategoryGroupSpan> spans) {
            Plan = plan; Spans = spans; Depth = plan.Runs.Max(run => run.Depth) + 1;
        }
        internal ChartCategoryGroupPlan Plan { get; }
        internal IReadOnlyList<ChartCategoryGroupSpan> Spans { get; }
        internal int Depth { get; }

        internal static CategoryGroupSource? Create(Chart chart, ChartRange range) {
            var plan = ChartCategoryGroupPlan.TryCreate(chart);
            // Cell membership describes the source domain, not optional marker-only pixel padding.
            return plan == null ? null : new CategoryGroupSource(plan,
                plan.CreateSpans(plan.Horizontal ? range.MinY : range.MinX, plan.Horizontal ? range.MaxY : range.MaxX));
        }
    }

    private static double[] CategoryGroupBandSizes(VisualSceneBuilder builder, AxisLabelCache cache, TextStyle style,
        double spacing, double minimum, double maximum, double trackLength, double budget) {
        var groups = cache.CategoryGroups;
        return groups == null ? Array.Empty<double>() : VisualCategoryGroupScene.Measure(builder, groups.Plan, groups.Spans,
            style, spacing, minimum, maximum, trackLength, budget);
    }

    private static void DrawCategoryGroups(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        AxisLabelCache cache, Func<double, double> coordinate, double minimum, double maximum, ChartRect band,
        VisualThemeColors colors, double[]? measuredBandSizes = null, List<LabelObstacle>? obstacles = null) {
        var groups = cache.CategoryGroups;
        if (groups != null) VisualCategoryGroupScene.Draw(chart, context, builder, groups.Plan, groups.Spans,
            coordinate, minimum, maximum, band, colors, measuredBandSizes, obstacles);
    }
}
