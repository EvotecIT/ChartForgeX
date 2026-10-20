using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>A native full-domain envelope shared by several layout contexts without changing authored axes.</summary>
internal sealed class ChartCartesianFullDomain {
    private ChartCartesianFullDomain(ChartXViewport x, ChartYViewport? y) { X = x; Y = y; }
    internal ChartXViewport X { get; }
    internal ChartYViewport? Y { get; }
    internal static ChartCartesianFullDomain Enclose(IReadOnlyList<PreparedVisual> scenes) {
        var minimumX = double.PositiveInfinity; var maximumX = double.NegativeInfinity;
        var minimumY = double.PositiveInfinity; var maximumY = double.NegativeInfinity; var hasY = true;
        foreach (var scene in scenes) {
            var x = scene.XProjection ?? throw new InvalidOperationException("Aligned Cartesian preparation requires a supported X projection in every drawing.");
            minimumX = Math.Min(minimumX, x.FullMinimum); maximumX = Math.Max(maximumX, x.FullMaximum);
            if (scene.YProjection is { } y) { minimumY = Math.Min(minimumY, y.FullMinimum); maximumY = Math.Max(maximumY, y.FullMaximum); }
            else hasY = false;
        }
        return new ChartCartesianFullDomain(new ChartXViewport(minimumX, maximumX), hasY ? new ChartYViewport(minimumY, maximumY) : null);
    }
    internal bool Matches(PreparedVisual scene) => scene.XProjection is { } x && x.FullMinimum == X.Minimum && x.FullMaximum == X.Maximum
        && (Y == null || scene.YProjection is { } y && y.FullMinimum == Y.Minimum && y.FullMaximum == Y.Maximum);

    internal void Apply(Chart chart, ChartRange range, ChartRange? secondaryRange) {
        var x = Expanded(chart.Options.XAxis, range.MinX, range.MaxX, X.Minimum, X.Maximum);
        range.SetXBounds(x.Minimum, x.Maximum); secondaryRange?.SetXBounds(x.Minimum, x.Maximum);
        if (Y != null) {
            var y = Expanded(chart.Options.YAxis, range.MinY, range.MaxY, Y.Minimum, Y.Maximum);
            range.SetYBounds(y.Minimum, y.Maximum);
        }
    }
    private static (double Minimum, double Maximum) Expanded(ChartAxis axis, double minimum, double maximum, double domainMinimum, double domainMaximum) {
        if (axis.Minimum.HasValue && domainMinimum != minimum || axis.Maximum.HasValue && domainMaximum != maximum)
            throw new InvalidOperationException("Aligned preparation must preserve every authored axis bound exactly.");
        return (axis.Minimum.HasValue ? minimum : Math.Min(minimum, domainMinimum), axis.Maximum.HasValue ? maximum : Math.Max(maximum, domainMaximum));
    }
}
