using System;

namespace ChartForgeX.Core;

/// <summary>An immutable spacing policy for a time axis. Null on an axis retains elapsed-time spacing.</summary>
public sealed class ChartTimeSpacing {
    private ChartTimeSpacing(ChartTimeTickInterval? unit) { CalendarUnit = unit; }

    /// <summary>Spaces the distinct supplied instants equally, collapsing omitted observations.</summary>
    /// <remarks>Source instants remain unchanged. Intermediate coordinates interpolate between adjacent supplied instants.</remarks>
    public static ChartTimeSpacing Ordinal { get; } = new(null);

    /// <summary>Creates equally wide calendar bands, including units without observations.</summary>
    /// <param name="unit">The existing calendar boundary schedule. Each series may supply at most one observation per band.</param>
    /// <returns>A discrete unit policy without aggregation or inferred data.</returns>
    /// <remarks>Every source instant in a band paints at its center. Inverse coordinates select its canonical calendar boundary;
    /// arbitrary instants within the same band do not have distinct projected coordinates.</remarks>
    public static ChartTimeSpacing Unit(ChartTimeTickInterval unit) => new(unit ?? throw new ArgumentNullException(nameof(unit)));

    /// <summary>Gets the calendar schedule, or null for ordinal spacing.</summary>
    public ChartTimeTickInterval? CalendarUnit { get; }
}
