using System;
using System.Globalization;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Sets the shared numeric display policy used by value labels, totals and numeric vertical axes.</summary>
    public Chart WithValueFormat(ChartValueFormat format) {
        Options.ValueFormat = format ?? throw new ArgumentNullException(nameof(format));
        return this;
    }

    /// <summary>
    /// Localises the chart's dates and numbers: time-axis labels, tooltips and accessible names use the culture's date and
    /// time patterns, and the automatic value format uses its separators.
    /// </summary>
    /// <remarks>
    /// Charts default to invariant ISO dates and invariant numbers. An explicit <see cref="WithValueFormat"/> policy keeps its
    /// own culture; call this before or after it, the authored format wins.
    /// </remarks>
    public Chart WithCulture(CultureInfo culture) {
        if (culture == null) throw new ArgumentNullException(nameof(culture));
        var copy = CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        foreach (var axis in new[] { Options.XAxis, Options.YAxis, Options.SecondaryXAxis, Options.SecondaryYAxis }) axis.Culture = copy;
        if (ReferenceEquals(Options.ValueFormat, ChartValueFormat.ExistingValue) || Options.ValueFormat.IsAutomatic)
            Options.ValueFormat = ChartValueFormat.Automatic(copy);
        return this;
    }

    /// <summary>
    /// Sets the unit shown after values in tooltips, readouts and accessible names, for example "ms" or "%".
    /// </summary>
    /// <remarks>Axis tick labels stay unit-free; put the unit in the axis title instead. An empty or null unit removes it.</remarks>
    public Chart WithValueUnit(string? unit) {
        Options.ValueUnit = string.IsNullOrWhiteSpace(unit) ? null : unit!.Trim();
        return this;
    }
}

public sealed partial class ChartOptions {
    private ChartValueFormat _valueFormat = ChartValueFormat.ExistingValue;

    /// <summary>Gets or sets the shared numeric display policy. Existing chart defaults retain grouped values below ten thousand.</summary>
    public ChartValueFormat ValueFormat {
        get => _valueFormat;
        set => _valueFormat = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the unit appended to values in tooltips, readouts and accessible names, or null for none.</summary>
    public string? ValueUnit { get; set; }

    /// <summary>Appends <see cref="ValueUnit"/> to a formatted value: "%" and other symbols attach directly, words follow a space.</summary>
    internal string WithUnit(string formatted) {
        if (ValueUnit == null || formatted.Length == 0) return formatted;
        return formatted + (ValueUnit.Length == 1 && !char.IsLetterOrDigit(ValueUnit[0]) ? ValueUnit : " " + ValueUnit);
    }
}
