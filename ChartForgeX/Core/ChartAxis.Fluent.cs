using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using PowerForge.Fluent;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ChartForgeX.Core;

/// <summary>Hand-written fluent methods of <see cref="ChartAxis"/> that do more than set one property.</summary>
public static partial class ChartAxisFluent {
    /// <summary>Sets the culture for this axis's tick labels and tooltips, or null for the invariant formatting.</summary>
    /// <param name="chartAxis">The instance to configure.</param>
    /// <param name="culture">The culture; it is stored read-only.</param>
    /// <returns>The current axis.</returns>
    public static global::ChartForgeX.Core.ChartAxis Culture(this global::ChartForgeX.Core.ChartAxis chartAxis, global::System.Globalization.CultureInfo? culture) {
        (chartAxis ?? throw new System.ArgumentNullException(nameof(chartAxis))).Culture = culture;
        return chartAxis;
    }

    /// <summary>Selects a time scale with an explicit shared session domain; null restores continuous elapsed time.</summary>
    /// <param name="chartAxis">The instance to configure.</param>
    /// <param name="domain">The immutable included intervals, or null to remove compression.</param>
    /// <returns>The current axis. Existing display zone and formatting policies are retained.</returns>
    public static global::ChartForgeX.Core.ChartAxis TimeSessions(this global::ChartForgeX.Core.ChartAxis chartAxis, global::ChartForgeX.Core.ChartTimeSessionDomain? domain)
        => (chartAxis ?? throw new System.ArgumentNullException(nameof(chartAxis))).SetTimeSessions(domain);

    /// <summary>Selects a time scale and an explicit spacing policy; null restores elapsed-time spacing.</summary>
    /// <remarks>Existing formatting and display-zone settings are retained. Remove sessions before selecting discrete spacing.</remarks>
    public static global::ChartForgeX.Core.ChartAxis TimeSpacing(this global::ChartForgeX.Core.ChartAxis chartAxis, global::ChartForgeX.Core.ChartTimeSpacing? spacing)
        => (chartAxis ?? throw new System.ArgumentNullException(nameof(chartAxis))).SetTimeSpacing(spacing);

    /// <summary>Places generated ticks at whole multiples of a positive step in data units.</summary>
    /// <param name="chartAxis">The instance to configure.</param>
    /// <param name="step">A finite step greater than zero. On logarithmic axes, a whole number of powers.</param>
    /// <returns>The current axis. A calendar step on the same axis is replaced.</returns>
    public static global::ChartForgeX.Core.ChartAxis TickStep(this global::ChartForgeX.Core.ChartAxis chartAxis, double step)
        => (chartAxis ?? throw new System.ArgumentNullException(nameof(chartAxis))).SetTickStep(step);

    /// <summary>Places time-axis ticks on fixed calendar boundaries, such as every month or every six hours.</summary>
    /// <param name="chartAxis">The instance to configure.</param>
    /// <param name="step">The calendar interval.</param>
    /// <returns>The current axis. A numeric step on the same axis is replaced.</returns>
    public static global::ChartForgeX.Core.ChartAxis TickStep(this global::ChartForgeX.Core.ChartAxis chartAxis, global::ChartForgeX.Core.ChartTimeTickInterval step)
        => (chartAxis ?? throw new System.ArgumentNullException(nameof(chartAxis))).SetTickStep(step);

    /// <summary>Places ticks at exactly these values; captions still use the axis formatter.</summary>
    /// <param name="chartAxis">The instance to configure.</param>
    /// <param name="values">Up to <see cref="ChartAxis.MaximumTickValues"/> finite values. Duplicates are merged and order does not matter.</param>
    /// <returns>The current axis.</returns>
    public static global::ChartForgeX.Core.ChartAxis TickValues(this global::ChartForgeX.Core.ChartAxis chartAxis, params double[] values)
        => (chartAxis ?? throw new System.ArgumentNullException(nameof(chartAxis))).SetTickValues(values);

    /// <summary>Places ticks at exactly these values; captions still use the axis formatter.</summary>
    /// <param name="chartAxis">The instance to configure.</param>
    /// <param name="values">Up to <see cref="ChartAxis.MaximumTickValues"/> finite values. Duplicates are merged and order does not matter.</param>
    /// <returns>The current axis.</returns>
    public static global::ChartForgeX.Core.ChartAxis TickValues(this global::ChartForgeX.Core.ChartAxis chartAxis, global::System.Collections.Generic.IEnumerable<double> values)
        => (chartAxis ?? throw new System.ArgumentNullException(nameof(chartAxis))).SetTickValues(values);
}
