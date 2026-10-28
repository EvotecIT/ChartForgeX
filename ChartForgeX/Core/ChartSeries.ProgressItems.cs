using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private bool _isTypedProgress;
    private IReadOnlyList<ChartProgressItem> _progressItems = Array.Empty<ChartProgressItem>();
    /// <summary>Gets retained labeled progress rows in authored order. Direct numeric ProgressBar series use Points instead.</summary>
    public IReadOnlyList<ChartProgressItem> ProgressItems => _progressItems;
    internal bool IsTypedProgress => Kind == ChartSeriesKind.ProgressBar && _isTypedProgress;

    /// <summary>Creates identified progress rows with optional independent domains. Items without an explicit maximum use ChartOptions.ProgressMaximum.</summary>
    public static ChartSeries CreateProgressBars(ChartSeriesId id, string name, IEnumerable<ChartProgressItem> items, ChartColor? color = null) {
        var series = new ChartSeries(id, name, ChartSeriesKind.ProgressBar, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceProgressItems(items); return series;
    }

    internal static ChartSeries CreateProgressBars(string name, IEnumerable<ChartProgressItem> items, ChartColor? color) {
        var series = new ChartSeries(name, ChartSeriesKind.ProgressBar, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceProgressItems(items); return series;
    }

    /// <summary>Replaces all progress rows atomically, opting into typed ownership and clearing old numeric data, keys and observation overrides.</summary>
    /// <remarks>The shared chart maximum is retained. Empty typed rows support refill; Points remains empty.</remarks>
    public ChartSeries ReplaceProgressItems(IEnumerable<ChartProgressItem> items) {
        RequireObservationKind(ChartSeriesKind.ProgressBar);
        if (items == null) throw new ArgumentNullException(nameof(items));
        var snapshot = items.Select(ValidateProgressItem).ToArray();
        _progressItems = Array.AsReadOnly(snapshot); _isTypedProgress = true; _points.Clear();
        ClearObservationMetadata(); ObservationDataChanged(snapshot.Length); return this;
    }

    /// <summary>Updates a complete progress row while preserving its index, explicit identity and presentation overrides.</summary>
    public ChartSeries UpdateProgressItem(int index, ChartProgressItem item) {
        RequireObservationKind(ChartSeriesKind.ProgressBar);
        if (!IsTypedProgress) throw new InvalidOperationException("Use ReplaceProgressItems before updating typed progress rows.");
        UpdateObservation(ref _progressItems, index, item, ValidateProgressItem); return this;
    }

    private static ChartProgressItem ValidateProgressItem(ChartProgressItem item) => item.Maximum.HasValue
        ? new ChartProgressItem(item.Label, item.Value, item.Minimum, item.Maximum.Value, item.Color)
        : new ChartProgressItem(item.Label, item.Value, item.Color);
}
