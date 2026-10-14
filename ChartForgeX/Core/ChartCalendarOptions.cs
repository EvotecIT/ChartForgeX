using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Controls the week layout, localized names, and cell sizing of calendar heatmaps in a chart.</summary>
public sealed class ChartCalendarOptions {
    private DayOfWeek _firstDayOfWeek;
    private string[]? _dayNames;
    private string[]? _monthNames;
    private double? _cellSize;
    private double? _maximumCellSize;
    private double? _gap;

    /// <summary>Gets or sets the weekday in the top row. Weeks begin on this day. Default: Sunday.</summary>
    public DayOfWeek FirstDayOfWeek {
        get => _firstDayOfWeek;
        set {
            if (!Enum.IsDefined(typeof(DayOfWeek), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown weekday.");
            _firstDayOfWeek = value;
        }
    }

    /// <summary>
    /// Gets or sets seven non-empty weekday names indexed by <see cref="DayOfWeek"/>, Sunday first.
    /// Null uses invariant abbreviations. Both assignment and retrieval copy the names.
    /// </summary>
    public IReadOnlyList<string>? DayNames {
        get => Copy(_dayNames);
        set => _dayNames = Names(value, 7, nameof(value), "Day names must contain seven non-empty entries indexed by DayOfWeek.");
    }

    /// <summary>
    /// Gets or sets twelve non-empty month names, January first. Null uses invariant abbreviations.
    /// Both assignment and retrieval copy the names.
    /// </summary>
    public IReadOnlyList<string>? MonthNames {
        get => Copy(_monthNames);
        set => _monthNames = Names(value, 12, nameof(value), "Month names must contain twelve non-empty entries, January first.");
    }

    /// <summary>
    /// Gets or sets a preferred cell size in pixels, between two and two hundred. Cells shrink to fit the plot.
    /// Null fills the plot; a preferred size takes precedence over <see cref="MaximumCellSize"/>.
    /// </summary>
    public double? CellSize {
        get => _cellSize;
        set { Range(value, 2, 200, "Calendar cell size must be between 2 and 200 pixels."); _cellSize = value; }
    }

    /// <summary>Gets or sets the largest size, in pixels, cells grow to when filling the plot. Null leaves them unlimited.</summary>
    public double? MaximumCellSize {
        get => _maximumCellSize;
        set { Range(value, 2, 200, "Calendar maximum cell size must be between 2 and 200 pixels."); _maximumCellSize = value; }
    }

    /// <summary>Gets or sets a cell gap from zero to twenty-four pixels. Null chooses 3.5 px, or 2.5 px above 32 weeks.</summary>
    public double? Gap {
        get => _gap;
        set { Range(value, 0, 24, "Calendar cell gap must be between zero and twenty-four pixels."); _gap = value; }
    }

    /// <summary>Copies the configured state, including localized names, for isolated chart configuration.</summary>
    internal ChartCalendarOptions Clone() {
        var copy = (ChartCalendarOptions)MemberwiseClone();
        copy._dayNames = Copy(_dayNames);
        copy._monthNames = Copy(_monthNames);
        return copy;
    }

    private static string[]? Copy(string[]? names) => names == null ? null : (string[])names.Clone();

    private static string[]? Names(IReadOnlyList<string>? names, int count, string parameter, string message) {
        if (names == null) return null;
        if (names.Count != count) throw new ArgumentException(message, parameter);
        var copy = new string[count];
        for (var i = 0; i < count; i++) {
            if (string.IsNullOrWhiteSpace(names[i])) throw new ArgumentException(message, parameter);
            copy[i] = names[i];
        }
        return copy;
    }

    private static void Range(double? value, double minimum, double maximum, string message) {
        if (!value.HasValue) return;
        ChartGuards.Finite(value.Value, nameof(value));
        if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(nameof(value), value, message);
    }
}
