using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double? _calendarCellSize;
    private double? _calendarMaximumCellSize;
    private double? _calendarCellGap;

    /// <summary>
    /// Gets or sets a preferred calendar heatmap cell size in pixels. It is used when it fits the plot; otherwise the cells
    /// shrink to fit. Null makes cells fill the plot (see <see cref="CalendarMaximumCellSize"/>).
    /// </summary>
    public double? CalendarCellSize {
        get => _calendarCellSize;
        set {
            ValidateNullableFinite(value, nameof(value));
            if (value < 2 || value > 200) throw new ArgumentOutOfRangeException(nameof(value), value, "Calendar cell size must be between 2 and 200 pixels.");
            _calendarCellSize = value;
        }
    }

    /// <summary>Gets or sets the largest size, in pixels, calendar cells grow to when they fill the plot. Null leaves them unlimited.</summary>
    public double? CalendarMaximumCellSize {
        get => _calendarMaximumCellSize;
        set {
            ValidateNullableFinite(value, nameof(value));
            if (value < 2 || value > 200) throw new ArgumentOutOfRangeException(nameof(value), value, "Calendar maximum cell size must be between 2 and 200 pixels.");
            _calendarMaximumCellSize = value;
        }
    }

    /// <summary>Gets or sets the gap between calendar cells in pixels. Null chooses 3.5 px, or 2.5 px above 32 weeks.</summary>
    public double? CalendarCellGap {
        get => _calendarCellGap;
        set {
            ValidateNullableFinite(value, nameof(value));
            if (value < 0 || value > 24) throw new ArgumentOutOfRangeException(nameof(value), value, "Calendar cell gap must be between zero and twenty-four pixels.");
            _calendarCellGap = value;
        }
    }
}