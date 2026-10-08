using System;
using System.Globalization;

namespace ChartForgeX.Core;

/// <summary>An immutable numeric display policy shared by axes, marks, totals and compact visuals.</summary>
/// <remarks>Culture data is copied at construction. A custom callback remains responsible for its own captured state.</remarks>
public sealed class ChartValueFormat {
    private enum Notation { Number, Compact, ExistingValue }
    private readonly Notation _notation;
    private readonly string _format;
    private readonly Func<double, string>? _formatter;

    private ChartValueFormat(Notation notation, string format, CultureInfo? culture, Func<double, string>? formatter = null) {
        _notation = notation; _format = format; _formatter = formatter;
        Culture = CultureInfo.ReadOnly((CultureInfo)(culture ?? CultureInfo.InvariantCulture).Clone());
    }

    /// <summary>Gets the detached, read-only display culture.</summary>
    public CultureInfo Culture { get; }

    /// <summary>Uses a .NET numeric format and an explicit culture, invariant by default.</summary>
    public static ChartValueFormat Number(string format = "0.##", CultureInfo? culture = null) {
        if (format == null) throw new ArgumentNullException(nameof(format));
        _ = 0d.ToString(format, culture ?? CultureInfo.InvariantCulture);
        return new ChartValueFormat(Notation.Number, format, culture);
    }

    /// <summary>Uses compact k, M and B suffixes, invariant by default.</summary>
    public static ChartValueFormat Compact(CultureInfo? culture = null) => new(Notation.Compact, "0.##", culture);

    /// <summary>Uses a custom formatter. A null result is displayed as empty text.</summary>
    public static ChartValueFormat Custom(Func<double, string> formatter) =>
        new(Notation.Number, string.Empty, null, formatter ?? throw new ArgumentNullException(nameof(formatter)));

    /// <summary>Formats a finite numeric observation.</summary>
    public string Format(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Display values must be finite.");
        if (_formatter != null) return _formatter(value) ?? string.Empty;
        if (_notation == Notation.Number) return value.ToString(_format, Culture);
        var magnitude = Math.Abs(value);
        if (_notation == Notation.ExistingValue && magnitude < 10000) return value.ToString("#,0.##", Culture);
        if (magnitude >= 1000000000) return (value / 1000000000).ToString("0.#", Culture) + "B";
        if (magnitude >= 1000000) return (value / 1000000).ToString("0.#", Culture) + "M";
        if (magnitude >= 1000) return (value / 1000).ToString("0.#", Culture) + "k";
        return value.ToString("0.##", Culture);
    }

    internal static ChartValueFormat ExistingValue { get; } = new(Notation.ExistingValue, "0.##", null);
    internal static ChartValueFormat InvariantCompact { get; } = Compact();
    internal Func<double, string> Callback => _formatter ?? Format;
}
