using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class ChartAxis {
    /// <summary>The most explicit tick values one axis accepts.</summary>
    public const int MaximumTickValues = 1000;
    /// <summary>The most minor ticks between two adjacent major ticks.</summary>
    public const int MaximumMinorTickCount = 20;

    private double? _tickStep;
    private ChartTimeTickInterval? _timeTickStep;
    private double[] _tickValues = Array.Empty<double>();
    private double? _minimumTickSpacing;
    private double? _maximumTickSpacing;
    private int _minorTickCount;
    private ChartAxisGridStyle? _grid;

    /// <summary>Gets the fixed distance between generated ticks, or null for automatic ticks.</summary>
    /// <remarks>Linear axes place ticks at whole multiples of the step, so a step of 25 gives 0, 25, 50 and so on.
    /// Logarithmic axes count whole powers of the base: 1 marks every power, 2 every second one. Time axes use
    /// <see cref="TimeTickStep"/>; symmetric logarithmic axes reject a fixed step during preparation.</remarks>
    public double? TickStep => _tickStep;

    /// <summary>Gets the fixed calendar step of a time axis, or null for automatic calendar ticks.</summary>
    public ChartTimeTickInterval? TimeTickStep => _timeTickStep;

    /// <summary>Gets the explicit tick positions, in ascending order. Empty uses generated ticks.</summary>
    /// <remarks>Values outside the visible domain are skipped. Captions use the axis formatter, unlike <see cref="Labels"/>,
    /// which pairs each position with authored text.</remarks>
    public IReadOnlyList<double> TickValues => _tickValues;

    /// <summary>Gets the smallest distance in drawing units the automatic tick search accepts between adjacent ticks, or null.</summary>
    public double? MinimumTickSpacing => _minimumTickSpacing;

    /// <summary>Gets the largest distance in drawing units the automatic tick search aims for between adjacent ticks, or null.</summary>
    /// <remarks>Captions keep their room first: a crowded axis can still choose a wider spacing.</remarks>
    public double? MaximumTickSpacing => _maximumTickSpacing;

    /// <summary>Gets the number of minor ticks between adjacent major ticks. Zero, the default, draws none.</summary>
    /// <remarks>Axes draw no tick marks, so minor ticks show as lighter grid lines wherever the axis draws grid lines.
    /// They divide each major interval evenly in data units.</remarks>
    public int MinorTickCount => _minorTickCount;

    /// <summary>Gets or sets this axis's grid styling, or null to use the chart grid style.</summary>
    public ChartAxisGridStyle? Grid { get => _grid; set => _grid = value; }

    internal bool HasFixedTicks => _tickStep.HasValue || _timeTickStep != null || _tickValues.Length > 0;
    internal bool HasTickSpacing => _minimumTickSpacing.HasValue || _maximumTickSpacing.HasValue;
    internal bool HasTickPolicy => HasFixedTicks || HasTickSpacing || _minorTickCount > 0;

    /// <summary>Places generated ticks at whole multiples of a positive step in data units.</summary>
    /// <param name="step">A finite step greater than zero. On logarithmic axes, a whole number of powers.</param>
    /// <returns>The current axis. A calendar step on the same axis is replaced.</returns>
    public ChartAxis WithTickStep(double step) {
        if (!ChartMath.IsFinite(step) || step <= 0) throw new ArgumentOutOfRangeException(nameof(step), step, "Tick step must be finite and greater than zero.");
        RejectFixedConflict(values: false);
        _tickStep = step; _timeTickStep = null; return this;
    }

    /// <summary>Places time-axis ticks on fixed calendar boundaries, such as every month or every six hours.</summary>
    /// <param name="step">The calendar interval.</param>
    /// <returns>The current axis. A numeric step on the same axis is replaced.</returns>
    public ChartAxis WithTickStep(ChartTimeTickInterval step) {
        if (step == null) throw new ArgumentNullException(nameof(step));
        RejectFixedConflict(values: false);
        _timeTickStep = step; _tickStep = null; return this;
    }

    /// <summary>Places ticks at exactly these values; captions still use the axis formatter.</summary>
    /// <param name="values">Up to <see cref="MaximumTickValues"/> finite values. Duplicates are merged and order does not matter.</param>
    /// <returns>The current axis.</returns>
    public ChartAxis WithTickValues(params double[] values) => WithTickValues((IEnumerable<double>)values);

    /// <summary>Places ticks at exactly these values; captions still use the axis formatter.</summary>
    /// <param name="values">Up to <see cref="MaximumTickValues"/> finite values. Duplicates are merged and order does not matter.</param>
    /// <returns>The current axis.</returns>
    public ChartAxis WithTickValues(IEnumerable<double> values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var sorted = new SortedSet<double>();
        foreach (var value in values) {
            if (!ChartMath.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Tick values must be finite.");
            sorted.Add(value == 0 ? 0 : value);
            if (sorted.Count > MaximumTickValues)
                throw new ArgumentOutOfRangeException(nameof(values), "An axis accepts at most " + MaximumTickValues + " explicit tick values.");
        }
        if (sorted.Count == 0) throw new ArgumentException("Provide at least one tick value, or call WithAutomaticTicks to clear them.", nameof(values));
        RejectFixedConflict(values: true);
        _tickValues = sorted.ToArray(); return this;
    }

    /// <summary>Bounds the distance between adjacent automatic ticks, in drawing units. Null leaves either side automatic.</summary>
    /// <param name="minimum">The smallest accepted spacing, or null.</param>
    /// <param name="maximum">The largest preferred spacing, or null. Must not be smaller than <paramref name="minimum"/>.</param>
    /// <returns>The current axis.</returns>
    /// <remarks>Applies to generated Cartesian ticks. A fixed step or explicit values already decide the ticks, so they cannot be combined.</remarks>
    public ChartAxis WithTickSpacing(double? minimum, double? maximum) {
        if (minimum.HasValue && (!ChartMath.IsFinite(minimum.Value) || minimum.Value <= 0))
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Minimum tick spacing must be finite and greater than zero.");
        if (maximum.HasValue && (!ChartMath.IsFinite(maximum.Value) || maximum.Value <= 0))
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Maximum tick spacing must be finite and greater than zero.");
        if (minimum.HasValue && maximum.HasValue && maximum.Value < minimum.Value)
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Maximum tick spacing must not be smaller than the minimum.");
        if ((minimum.HasValue || maximum.HasValue) && HasFixedTicks)
            throw new InvalidOperationException("A fixed tick step or explicit tick values already decide the ticks; clear them before bounding tick spacing.");
        _minimumTickSpacing = minimum; _maximumTickSpacing = maximum; return this;
    }

    /// <summary>Divides every major interval with evenly spaced minor ticks, shown as lighter grid lines.</summary>
    /// <param name="count">Minor ticks between adjacent major ticks, from zero (none) through <see cref="MaximumMinorTickCount"/>.</param>
    /// <returns>The current axis.</returns>
    public ChartAxis WithMinorTicks(int count) {
        if (count < 0 || count > MaximumMinorTickCount)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Minor tick count must be from zero through " + MaximumMinorTickCount + ".");
        _minorTickCount = count; return this;
    }

    /// <summary>Clears the fixed step, explicit values and spacing bounds so the axis chooses its ticks again.</summary>
    /// <returns>The current axis. Minor ticks and grid styling are kept.</returns>
    public ChartAxis WithAutomaticTicks() {
        _tickStep = null; _timeTickStep = null; _tickValues = Array.Empty<double>();
        _minimumTickSpacing = null; _maximumTickSpacing = null; return this;
    }

    /// <summary>Sets this axis's grid styling; null restores the chart grid style.</summary>
    public ChartAxis WithGrid(ChartAxisGridStyle? style) { Grid = style; return this; }

    /// <summary>Configures this axis's grid styling, creating it on first use.</summary>
    public ChartAxis ConfigureGrid(Action<ChartAxisGridStyle> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Grid ??= new ChartAxisGridStyle()); return this;
    }

    private void RejectFixedConflict(bool values) {
        if (HasTickSpacing) throw new InvalidOperationException("Tick spacing bounds steer automatic ticks; clear them before fixing the ticks.");
        if (values && (_tickStep.HasValue || _timeTickStep != null))
            throw new InvalidOperationException("A tick step and explicit tick values cannot be combined; clear the step with WithAutomaticTicks first.");
        if (!values && _tickValues.Length > 0)
            throw new InvalidOperationException("Explicit tick values and a tick step cannot be combined; clear the values with WithAutomaticTicks first.");
    }

    /// <summary>Rejects a fixed step the selected scale cannot honour, before any layout runs.</summary>
    internal void ValidateTickPolicy(string axisName) {
        if (_tickStep.HasValue) {
            if (Scale == ChartScaleKind.Time)
                throw new InvalidOperationException(axisName + " is a time axis; set a calendar step with WithTickStep(ChartTimeTickInterval).");
            if (Scale == ChartScaleKind.SymmetricLogarithmic)
                throw new InvalidOperationException(axisName + " uses a symmetric logarithmic scale, which has no uniform tick step; use explicit tick values.");
            if (Scale == ChartScaleKind.Logarithmic && Math.Abs(_tickStep.Value - Math.Round(_tickStep.Value)) > 1e-9)
                throw new InvalidOperationException(axisName + " is logarithmic; its tick step counts whole powers of the base.");
        }
        if (_timeTickStep != null && Scale != ChartScaleKind.Time)
            throw new InvalidOperationException(axisName + " is not a time axis; use a numeric WithTickStep.");
    }
}
