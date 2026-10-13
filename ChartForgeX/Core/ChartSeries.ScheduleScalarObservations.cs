using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartGanttItem> _ganttItems = Array.Empty<ChartGanttItem>();
    /// <summary>Gets the task, milestone or marker authored for this classic Gantt series. Empty means no data.</summary>
    public IReadOnlyList<ChartGanttItem> GanttItems => _ganttItems;

    /// <summary>Replaces the single Gantt item or clears it with an empty sequence. Clears per-observation keys and overrides.</summary>
    public ChartSeries ReplaceGanttItems(IEnumerable<ChartGanttItem> values) {
        RequireObservationKind(ChartSeriesKind.Gantt);
        ReplaceObservations(ref _ganttItems, SingleObservation(values), ValidateGanttItem);
        return this;
    }

    /// <summary>Updates the Gantt item while preserving its explicit key and presentation overrides.</summary>
    public ChartSeries UpdateGanttItem(int index, ChartGanttItem value) {
        RequireObservationKind(ChartSeriesKind.Gantt);
        UpdateObservation(ref _ganttItems, index, value, ValidateGanttItem);
        return this;
    }

    private static ChartGanttItem ValidateGanttItem(ChartGanttItem value) => new(value.Start, value.End, value.Progress, value.DependsOn, value.IsMilestone, value.IsVerticalMarker);

    private IReadOnlyList<ChartGaugeValue> _gaugeValues = Array.Empty<ChartGaugeValue>();
    /// <summary>Gets the measured value and scale authored for this gauge or circle series. Empty means no data.</summary>
    public IReadOnlyList<ChartGaugeValue> GaugeValues => _gaugeValues;

    /// <summary>Replaces the single measured value or clears it with an empty sequence. Clears per-observation keys and overrides.</summary>
    public ChartSeries ReplaceGaugeValues(IEnumerable<ChartGaugeValue> values) {
        RequireObservationKind(ChartSeriesKind.Gauge, ChartSeriesKind.Circle);
        ReplaceObservations(ref _gaugeValues, SingleObservation(values), ValidateGaugeValue);
        return this;
    }

    /// <summary>Updates the measured value and scale while preserving its explicit key and presentation overrides.</summary>
    public ChartSeries UpdateGaugeValue(int index, ChartGaugeValue value) {
        RequireObservationKind(ChartSeriesKind.Gauge, ChartSeriesKind.Circle);
        UpdateObservation(ref _gaugeValues, index, value, ValidateGaugeValue);
        return this;
    }

    private static ChartGaugeValue ValidateGaugeValue(ChartGaugeValue value) => new(value.Value, value.Minimum, value.Maximum);

    private IReadOnlyList<ChartBulletValue> _bulletValues = Array.Empty<ChartBulletValue>();
    /// <summary>Gets the measured value, target, scale and qualitative boundaries authored for this bullet series.</summary>
    public IReadOnlyList<ChartBulletValue> BulletValues => _bulletValues;

    /// <summary>Replaces the single bullet observation or clears it with an empty sequence. Clears per-observation keys and overrides.</summary>
    public ChartSeries ReplaceBulletValues(IEnumerable<ChartBulletValue> values) {
        RequireObservationKind(ChartSeriesKind.Bullet);
        ReplaceObservations(ref _bulletValues, SingleObservation(values), ValidateBulletValue);
        return this;
    }

    /// <summary>Updates the bullet observation while preserving its explicit key and presentation overrides.</summary>
    public ChartSeries UpdateBulletValue(int index, ChartBulletValue value) {
        RequireObservationKind(ChartSeriesKind.Bullet);
        UpdateObservation(ref _bulletValues, index, value, ValidateBulletValue);
        return this;
    }

    private static ChartBulletValue ValidateBulletValue(ChartBulletValue value) => new(value.Value, value.Target, value.Minimum, value.Maximum, value.RangeEnds);

    private static T[] SingleObservation<T>(IEnumerable<T> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var snapshot = values.ToArray();
        if (snapshot.Length > 1) throw new ArgumentException("This series holds one observation. Add another series for another row or scalar value.", nameof(values));
        return snapshot;
    }
}
