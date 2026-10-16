using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>
/// Measures the label band and density under matrix heatmaps. The compiler can choose a measured automatic angle;
/// authored <see cref="ChartOptions.XAxisLabelAngle"/> values retain the same geometry in both static painters.
/// </summary>
internal static class ChartHeatmapColumnLabels {
    /// <summary>Limits both rotated reaches to the resolved content viewport, retaining room for cells at shallow angles.</summary>
    internal static double MaximumRotatedLength(Chart chart, ChartRect viewport, double textHeight, double? angle = null) {
        var radians = Math.Abs(angle ?? Angle(chart)) * Math.PI / 180;
        var sine = Math.Sin(radians);
        var cosine = Math.Cos(radians);
        var verticalRoom = viewport.Height * 0.4 - RotatedOffset - 8 - cosine * textHeight;
        var horizontalRoom = viewport.Width * 0.4 - EdgeInset - sine * textHeight / 2;
        return Math.Max(0, Math.Min(verticalRoom / Math.Max(0.000001, sine), horizontalRoom / Math.Max(0.000001, cosine)));
    }

    /// <summary>Baseline offset of unrotated labels below the plot.</summary>
    public const double LabelOffset = 22;

    /// <summary>Offset of the anchor of rotated labels below the plot.</summary>
    public const double RotatedOffset = 10;

    /// <summary>Returns the label angle in degrees, limited to ±80 as x-axis labels are.</summary>
    public static double Angle(Chart chart) {
        var angle = chart.Options.XAxisLabelAngle;
        if (double.IsNaN(angle)) return 0;
        return Math.Max(-80, Math.Min(80, angle));
    }

    /// <summary>Returns true when column labels are rotated.</summary>
    public static bool IsRotated(Chart chart, double? angle = null) => Math.Abs(angle ?? Angle(chart)) >= 0.001;

    /// <summary>
    /// Returns the height below the plot that the column labels take, up to the top of what follows them (an axis
    /// title or the heatmap scale).
    /// </summary>
    /// <param name="chart">The chart.</param>
    /// <param name="viewport">The resolved content viewport after frame layout.</param>
    /// <param name="widestLabel">Width of the widest label at the tick font size.</param>
    /// <param name="textHeight">Height of a line of tick text.</param>
    /// <param name="angle">Measured automatic angle, or null to use the authored chart angle.</param>
    public static double Reserve(Chart chart, ChartRect viewport, double widestLabel, double textHeight, double? angle = null) {
        if (!IsRotated(chart, angle)) return textHeight + 24;
        var radians = Math.Abs(angle ?? Angle(chart)) * Math.PI / 180;
        return RotatedOffset + Math.Sin(radians) * Math.Min(MaximumRotatedLength(chart, viewport, textHeight, angle), widestLabel) + Math.Cos(radians) * textHeight + 8;
    }

    /// <summary>
    /// Returns true when a rotated label ends at its column (negative angles, the label runs down to the left) rather
    /// than starting there. Labels never flip at the plot edge, which would cross their neighbours; the plot moves in
    /// to leave them room (see <see cref="SideReserve"/>) and anything longer is shortened (see <see cref="MaximumRotatedLength"/>).
    /// </summary>
    public static bool EndsAtColumn(Chart chart, double? angle = null) => (angle ?? Angle(chart)) < 0;

    /// <summary>
    /// Returns the width the plot must give up on the side the labels slant towards (left for negative angles, right
    /// for positive ones) so the label of the outermost column fits inside the chart; zero when they are not rotated.
    /// </summary>
    /// <param name="chart">The chart.</param>
    /// <param name="viewport">The resolved content viewport after frame layout.</param>
    /// <param name="widestLabel">Width of the widest label at the tick font size.</param>
    /// <param name="textHeight">Height of a line of tick text.</param>
    /// <param name="spaceBeside">Space between the plot and the chart edge on that side before the reserve.</param>
    /// <param name="angle">Measured automatic angle, or null to use the authored chart angle.</param>
    public static double SideReserve(Chart chart, ChartRect viewport, double widestLabel, double textHeight, double spaceBeside, double? angle = null) {
        if (!IsRotated(chart, angle)) return 0;
        return Math.Max(0, Reach(chart, Math.Min(MaximumRotatedLength(chart, viewport, textHeight, angle), widestLabel), textHeight, angle) - spaceBeside);
    }

    /// <summary>Returns how far a rotated label of <paramref name="length"/> reaches sideways from its column, edge inset included.</summary>
    private static double Reach(Chart chart, double length, double textHeight, double? angle = null) {
        var radians = Math.Abs(angle ?? Angle(chart)) * Math.PI / 180;
        return length * Math.Cos(radians) + Math.Sin(radians) * textHeight / 2 + EdgeInset;
    }

    private const double EdgeInset = 4;
    private const double MinimumLineSeparation = .9;

    /// <summary>Returns the smallest column pitch that can show every measured caption without thinning.</summary>
    internal static double MinimumPitch(Chart chart, ChartRect viewport, double textHeight, double widestLabel, double? angle = null) {
        var radians = Math.Abs(angle ?? Angle(chart)) * Math.PI / 180;
        var length = Math.Min(MaximumRotatedLength(chart, viewport, textHeight, angle), widestLabel);
        return Math.Min(length / Math.Max(.000001, Math.Cos(radians)),
            textHeight * MinimumLineSeparation / Math.Max(.000001, Math.Sin(radians)));
    }

    /// <summary>
    /// Returns how many columns apart rotated labels are drawn so neighbouring labels do not overlap: every column when
    /// the labels are shorter than the horizontal distance between columns or the slant leaves room for a line of text
    /// between them, otherwise every n-th column.
    /// </summary>
    /// <param name="chart">The chart.</param>
    /// <param name="viewport">The resolved content viewport after frame layout.</param>
    /// <param name="columnPitch">Distance between the centres of neighbouring columns.</param>
    /// <param name="textHeight">Height of a line of tick text.</param>
    /// <param name="widestLabel">Width of the widest label at the tick font size.</param>
    /// <param name="angle">Measured automatic angle, or null to use the authored chart angle.</param>
    public static int Step(Chart chart, ChartRect viewport, double columnPitch, double textHeight, double widestLabel, double? angle = null) {
        var radians = Math.Abs(angle ?? Angle(chart)) * Math.PI / 180;
        if (columnPitch >= MinimumPitch(chart, viewport, textHeight, widestLabel, angle) - .000001) return 1;
        var spacing = columnPitch * Math.Sin(radians);
        if (spacing <= 0) return 1;
        return Math.Max(1, (int)Math.Ceiling(textHeight * MinimumLineSeparation / spacing - 0.000001));
    }
}
