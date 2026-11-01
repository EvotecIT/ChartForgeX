using System;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>An immutable numeric display policy shared by axes, marks, totals and compact visuals.</summary>
/// <remarks>Culture data is copied at construction. A custom callback remains responsible for its own captured state.</remarks>
public sealed partial class ChartValueFormat {
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
        if (_notation == Notation.ExistingValue && magnitude < 10000) {
            var caption = value.ToString("#,0.##", Culture);
            return value != 0 && (caption == "0" || caption == "-0") ? FormatSignificant(value) : caption;
        }
        var compact = magnitude >= 1000000000 ? (value / 1000000000).ToString("0.#", Culture) + "B"
            : magnitude >= 1000000 ? (value / 1000000).ToString("0.#", Culture) + "M"
            : magnitude >= 1000 ? (value / 1000).ToString("0.#", Culture) + "k"
            : value.ToString("0.##", Culture);
        // Automatic captions stay bounded; explicit Number, Compact and callback policies remain authoritative.
        return _notation == Notation.ExistingValue && compact.Length > 24 ? FormatSignificant(value) : compact;
    }

    /// <summary>Captures only built-in numeric policies and unmodified named cultures, without invoking custom callbacks.</summary>
    internal (string Notation, string Format, string Culture) PortablePolicy() {
        if (_formatter != null) throw new NotSupportedException("Custom numeric callbacks cannot be serialized as editable waterfall data.");
        if (Culture.GetType() != typeof(CultureInfo)) throw new NotSupportedException("Captured numeric policies require standard named CultureInfo providers.");
        var actual = Culture.NumberFormat; var standard = CultureInfo.GetCultureInfo(Culture.Name).NumberFormat;
        if (actual.CurrencyDecimalDigits != standard.CurrencyDecimalDigits
            || actual.CurrencyDecimalSeparator != standard.CurrencyDecimalSeparator
            || actual.CurrencyGroupSeparator != standard.CurrencyGroupSeparator
            || actual.CurrencyNegativePattern != standard.CurrencyNegativePattern
            || actual.CurrencyPositivePattern != standard.CurrencyPositivePattern
            || actual.CurrencySymbol != standard.CurrencySymbol
            || actual.DigitSubstitution != standard.DigitSubstitution
            || actual.NaNSymbol != standard.NaNSymbol
            || actual.NegativeInfinitySymbol != standard.NegativeInfinitySymbol
            || actual.NegativeSign != standard.NegativeSign
            || actual.NumberDecimalDigits != standard.NumberDecimalDigits
            || actual.NumberDecimalSeparator != standard.NumberDecimalSeparator
            || actual.NumberGroupSeparator != standard.NumberGroupSeparator
            || actual.NumberNegativePattern != standard.NumberNegativePattern
            || actual.PercentDecimalDigits != standard.PercentDecimalDigits
            || actual.PercentDecimalSeparator != standard.PercentDecimalSeparator
            || actual.PercentGroupSeparator != standard.PercentGroupSeparator
            || actual.PercentNegativePattern != standard.PercentNegativePattern
            || actual.PercentPositivePattern != standard.PercentPositivePattern
            || actual.PercentSymbol != standard.PercentSymbol
            || actual.PerMilleSymbol != standard.PerMilleSymbol
            || actual.PositiveInfinitySymbol != standard.PositiveInfinitySymbol
            || actual.PositiveSign != standard.PositiveSign
            || !actual.CurrencyGroupSizes.SequenceEqual(standard.CurrencyGroupSizes)
            || !actual.NumberGroupSizes.SequenceEqual(standard.NumberGroupSizes)
            || !actual.PercentGroupSizes.SequenceEqual(standard.PercentGroupSizes)
            || !actual.NativeDigits.SequenceEqual(standard.NativeDigits))
            throw new NotSupportedException("Modified number cultures cannot be serialized as editable waterfall data.");
        return (_notation.ToString(), _format, Culture.Name);
    }

    /// <summary>Restores the existing immutable built-in policy after the interchange reader validates its named fields.</summary>
    internal static ChartValueFormat FromPortablePolicy(string notation, string format, string culture) {
        var named = CultureInfo.GetCultureInfo(culture);
        if (notation == "Number") return Number(format, named);
        if (notation == "Compact") {
            if (format != "0.##") throw new ArgumentException("Compact policies require their canonical numeric format.");
            return Compact(named);
        }
        if (notation == "ExistingValue" && format == "0.##" && culture.Length == 0) return ExistingValue;
        throw new ArgumentException("Unknown or contradictory portable numeric policy.");
    }

    internal static ChartValueFormat ExistingValue { get; } = new(Notation.ExistingValue, "0.##", null);
    internal static ChartValueFormat InvariantCompact { get; } = Compact();
    /// <summary>Uses one precision for a chart's ratios; an authored percentage policy remains authoritative.</summary>
    internal static ChartValueFormat Percentages(ChartValueFormat authored, System.Collections.Generic.IEnumerable<double> fractions) {
        // Raw-value policies (counts, currencies and callbacks) do not change the unit
        // of derived ratios. An explicit percentage policy owns its precision/culture.
        if (authored._formatter == null && authored._notation == Notation.Number && authored.UsesPercentageNotation()) return authored;
        var decimals = fractions.Any(value => Math.Round(value * 100, 1) != Math.Round(value * 100));
        return Number(decimals ? "0.0%" : "0%");
    }
    /// <summary>Preserves an overflowing derived ratio's description without admitting nonfinite source values.</summary>
    internal string FormatFraction(double fraction) => double.IsInfinity(fraction) ? fraction.ToString(_format, Culture) : Format(fraction);
    private bool UsesPercentageNotation() {
        if (_format.Length > 0 && char.ToUpperInvariant(_format[0]) == 'P' && _format.Skip(1).All(char.IsDigit)) return true;
        var quote = '\0';
        for (var index = 0; index < _format.Length; index++) {
            // Numeric custom formats may quote or escape a percent sign as a raw-value unit.
            var token = _format[index];
            if (token == '\\') { index++; continue; }
            if (quote != '\0') { if (token == quote) quote = '\0'; continue; }
            if (token is '\'' or '"') { quote = token; continue; }
            if (token == '%') return true;
        }
        return false;
    }
    // Automatic value and axis policies share this bounded fallback; authored formats remain exact policies.
    internal static string FormatSignificant(double value) => value.ToString("G6", CultureInfo.InvariantCulture);
    // Native transition plans retain built-in immutable policy, never a caller-owned mutable closure.
    internal ChartValueFormat? CapturedValuePolicy => _formatter == null ? this : null;
    internal Func<double, string> Callback => _formatter ?? Format;
}
