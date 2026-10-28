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
    /// Localises the chart's dates and times: time-axis labels, tooltips and accessible names use the culture's date and
    /// time patterns. The invariant culture restores ISO dates.
    /// </summary>
    /// <remarks>
    /// Numbers keep their value format; pass the culture to it as well, for example
    /// <c>WithValueFormat(ChartValueFormat.Number("0.#", culture))</c>.
    /// </remarks>
    public Chart WithCulture(CultureInfo culture) {
        if (culture == null) throw new ArgumentNullException(nameof(culture));
        var copy = culture.Name.Length == 0 ? null : CultureInfo.ReadOnly((CultureInfo)culture.Clone());
        foreach (var axis in new[] { Options.XAxis, Options.YAxis, Options.SecondaryXAxis, Options.SecondaryYAxis }) axis.Culture = copy;
        return this;
    }

    /// <summary>
    /// Sets the unit shown after values in tooltips, readouts and accessible names, for example "ms" or "%".
    /// </summary>
    /// <remarks>Applies to cartesian, schedule and heatmap charts. A value format that already writes the unit keeps a single copy.
    /// Axis tick labels stay unit-free; put the unit in the axis title instead. An empty or null unit removes it.</remarks>
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

    /// <summary>Appends <see cref="ValueUnit"/> to a formatted value: "%" and other symbols attach directly, words follow a space; a value that already ends with the unit is unchanged.</summary>
    internal string WithUnit(string formatted) => AppendUnit(formatted, ValueUnit);

    internal static string AppendUnit(string formatted, string? unit) {
        // A value format that already writes the unit ("12.5 ms") keeps a single copy.
        if (unit == null || formatted.Length == 0 || formatted.TrimEnd().EndsWith(unit, StringComparison.Ordinal)) return formatted;
        return formatted + (unit.Length == 1 && !char.IsLetterOrDigit(unit[0]) ? unit : " " + unit);
    }
}
