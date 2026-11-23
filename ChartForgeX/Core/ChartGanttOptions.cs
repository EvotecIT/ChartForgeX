using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Gantt markers, ticks and open ends. Configure it through <see cref="ChartOptions.Gantt"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartGanttOptions {
    internal ChartGanttOptions Clone() => (ChartGanttOptions)MemberwiseClone();
    private double? _ganttToday;


    /// <summary>
    /// Gets or sets the optional Gantt current-date marker as an OLE Automation date or numeric schedule value.
    /// </summary>
    public double? Today {
        get => _ganttToday;
        set {
            ChartOptions.ValidateNullableFinite(value, nameof(value));
            _ganttToday = value;
        }
    }


    /// <summary>Gets or sets fixed calendar ticks for classic Gantt time axes. Null uses automatic ticks.
    /// Explicit axis labels take precedence; windows without a boundary have no tick labels.</summary>
    public ChartTimeTickInterval? TickInterval { get; set; }

    private ChartGanttOpenEndStyle _ganttOpenEndStyle;

    /// <summary>Gets or sets the continuation style for open classic tasks and Gantt-lane items.</summary>
    /// <remarks>Classic open tasks require an explicit X-axis maximum. Lane items retain their existing
    /// current-time endpoint policy. Neither style reads the clock during export.</remarks>
    public ChartGanttOpenEndStyle OpenEndStyle {
        get => _ganttOpenEndStyle;
        set {
            if (!Enum.IsDefined(typeof(ChartGanttOpenEndStyle), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _ganttOpenEndStyle = value;
        }
    }

    /// <summary>Gets or sets optional stroke overrides for classic Gantt and lane current-time guides.</summary>
    public ChartStrokeStyle? TodayStyle { get; set; }
}
