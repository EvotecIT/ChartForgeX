using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static TextStyle CrossingTickStyle(Chart chart, VisualRenderContext context, VisualThemeColors colors) => chart.Options.TickLabelStyle.Resolve(
        new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground });

    /// <summary>Moves a complete horizontal track's measured caption/parent/title bands to the retained physical Y coordinate.</summary>
    private static void DrawCrossingHorizontalText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect viewport,
        ChartAxis axis, IReadOnlyList<double> ticks, Func<double, double> project, double minimum, double maximum, double track, bool secondary,
        bool category, string title, VisualThemeColors colors, AxisLabelCache labels, List<LabelObstacle> obstacles, Func<double, string>? fallback, string role) {
        var spacing = context.Theme.Spacing; var style = CrossingTickStyle(chart, context, colors);
        var metrics = TickMetrics(builder, axis, ticks, style, fallback, labels);
        var available = Math.Max(0, secondary ? track - viewport.Top - spacing : viewport.Bottom - track - spacing);
        var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisTitleSize, Color = colors.Foreground });
        var desiredTitle = title.Length == 0 ? 0 : builder.MeasureText(title, titleStyle).Height + spacing;
        var groupHeight = category ? CategoryGroupBandSizes(builder, labels, style, spacing, minimum, maximum, plot.Width,
            Math.Max(0, available - metrics.Height - desiredTitle)).Sum() : 0;
        var titleHeight = FitAxisTitleReservation(metrics.Height + groupHeight, desiredTitle, available);
        var height = Math.Max(0, Math.Min(metrics.Height, available - titleHeight - groupHeight));
        var bounds = new ChartRect(viewport.Left, secondary ? track - spacing - height : track + spacing, viewport.Width, height);
        using (builder.PushClip(viewport)) {
            DrawAxisLabels(builder, chart.Options, axis, ticks, project, true, secondary, bounds, style, spacing, fallback, labels, plot.Width, obstacles);
            if (category) DrawCategoryGroups(chart, context, builder, labels, project, minimum, maximum,
                new ChartRect(plot.Left, bounds.Bottom, plot.Width, groupHeight), colors, obstacles: obstacles);
            if (title.Length > 0) DrawAxisTitle(chart, context, builder, title,
                new ChartRect(plot.Left, secondary ? Math.Max(viewport.Top, bounds.Top - titleHeight) : bounds.Bottom + groupHeight + spacing,
                    plot.Width, Math.Max(0, titleHeight - spacing)), colors, TextAlignment.Center, role + "-title", obstacles);
        }
    }

    /// <summary>Moves a vertical track's side gutter and endpoint title, using the existing category-parent and measured text owners.</summary>
    private static void DrawCrossingVerticalText(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect viewport,
        ChartAxis axis, IReadOnlyList<double> ticks, Func<double, double> project, double minimum, double maximum, double track, bool secondary,
        bool category, string title, VisualThemeColors colors, AxisLabelCache labels, List<LabelObstacle> obstacles, Func<double, string>? fallback, string role) {
        var spacing = context.Theme.Spacing; var style = CrossingTickStyle(chart, context, colors);
        var available = Math.Max(0, secondary ? viewport.Right - track - spacing : track - viewport.Left - spacing);
        var metrics = TickMetrics(builder, axis, ticks, style, fallback, labels);
        var tickWidth = Math.Min(metrics.Width, Math.Min(available, viewport.Width * .35));
        var sizes = category ? CategoryGroupBandSizes(builder, labels, style, spacing, minimum, maximum, plot.Height,
            Math.Max(0, available - tickWidth)) : Array.Empty<double>();
        var groupWidth = sizes.Sum();
        var bounds = new ChartRect(secondary ? track + spacing : track - spacing - tickWidth, viewport.Top, tickWidth, viewport.Height);
        using (builder.PushClip(viewport)) {
            DrawAxisLabels(builder, chart.Options, axis, ticks, project, false, secondary, bounds, style, spacing, fallback, labels, plot.Height, obstacles);
            if (category) DrawCategoryGroups(chart, context, builder, labels, project, minimum, maximum,
                new ChartRect(bounds.Left - groupWidth, plot.Top, groupWidth, plot.Height), colors, sizes, obstacles);
            if (title.Length > 0) DrawAxisTitle(chart, context, builder, title,
                new ChartRect(secondary ? viewport.Left : track, viewport.Top,
                    Math.Max(0, secondary ? track - viewport.Left : viewport.Right - track), Math.Max(0, plot.Top - viewport.Top - spacing)),
                colors, secondary ? TextAlignment.Right : TextAlignment.Left, role + "-title", obstacles);
        }
    }
}
