using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private readonly List<ChartAdditionalValueAxis> _additionalValueAxes = new();
    private IReadOnlyList<ChartAdditionalValueAxis>? _additionalValueAxesView;

    /// <summary>Gets the additional value axes in creation order. Axes on one side stack outward in this order.</summary>
    public IReadOnlyList<ChartAdditionalValueAxis> AdditionalValueAxes =>
        _additionalValueAxesView ??= new ReadOnlyCollection<ChartAdditionalValueAxis>(_additionalValueAxes);

    /// <summary>
    /// Adds a value axis that stacks outward beside the primary (start) or secondary (end) axis of a vertical Cartesian chart.
    /// </summary>
    /// <param name="placement">The side that holds the axis.</param>
    /// <param name="title">The axis title shown above the plot.</param>
    /// <param name="configure">An optional axis configuration.</param>
    /// <returns>The new axis, which series bind to with <see cref="ChartSeries.UseValueAxis"/>.</returns>
    /// <remarks>An axis without bound series draws nothing. Line, step line, area, step area, scatter and unstacked bar series can bind to it.</remarks>
    public ChartAdditionalValueAxis AddValueAxis(ChartAxisPlacement placement, string title, Action<ChartAxis>? configure = null) {
        var axis = new ChartAdditionalValueAxis(this, placement, title);
        if (configure != null) axis.Configure(configure);
        _additionalValueAxes.Add(axis);
        return axis;
    }

    // A series bound to another chart's axis counts too, so validation reports it rather than drawing it off-scale.
    internal bool UsesAdditionalValueAxes => Series.Any(series => series.AdditionalValueAxis != null);

    internal void ValidateAdditionalValueAxes() {
        if (!UsesAdditionalValueAxes) return;
        if (Options.IsSparkline || ChartCartesianOrientation.IsHorizontal(this))
            throw new NotSupportedException("Additional value axes require a vertical Cartesian chart.");
        foreach (var series in Series) {
            if (series.AdditionalValueAxis is not { } axis) continue;
            axis.Validate(this);
            if (series.Kind is not (ChartSeriesKind.Line or ChartSeriesKind.StepLine or ChartSeriesKind.Area or ChartSeriesKind.StepArea
                or ChartSeriesKind.Scatter or ChartSeriesKind.Bar) || series.HistogramBinLayout != null)
                throw new NotSupportedException("Additional value axes support line, step line, area, step area, scatter and bar series, not " + series.Kind + ".");
            if (ChartStackLayout.Participates(this, series))
                throw new NotSupportedException("A series on an additional value axis cannot join a stack.");
        }
    }
}
