using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Typography;

/// <summary>Immutable, case-sensitive OpenType axis coordinates in the font's design units.
/// Unknown axes are ignored by a face; supported values are clamped to its declared range.</summary>
public sealed class FontVariationSettings : IReadOnlyDictionary<string, double> {
    private readonly SortedDictionary<string, double> _axes;
    private readonly string _css;
    private readonly string _key;
    /// <summary>The font's default instance, without explicit axis coordinates.</summary>
    public static FontVariationSettings Default { get; } = new();
    /// <summary>Creates the default instance.</summary>
    public FontVariationSettings() { _axes = new(StringComparer.Ordinal); _css = "normal"; _key = ""; }
    /// <summary>Copies axis coordinates. Tags contain exactly four ASCII letters or digits;
    /// values must be finite. A specification can contain at most 32 axes.</summary>
    public FontVariationSettings(IEnumerable<KeyValuePair<string, double>> axes) : this() {
        if (axes == null) throw new ArgumentNullException(nameof(axes));
        foreach (var axis in axes) {
            Validate(axis.Key, axis.Value); _axes[axis.Key] = axis.Value;
            if (_axes.Count > 32) throw new ArgumentException("At most 32 variation axes can be selected.", nameof(axes));
        }
        _css = Count == 0 ? "normal" : string.Join(",", _axes.Select(a => "'" + a.Key + "' " + a.Value.ToString("R", CultureInfo.InvariantCulture)));
        _key = string.Join(";", _axes.Select(a => a.Key + "=" + a.Value.ToString("R", CultureInfo.InvariantCulture)));
    }
    /// <summary>Returns an independent selection with one axis set or replaced.</summary>
    public FontVariationSettings WithAxis(string tag, double value) {
        Validate(tag, value);
        var axes = new SortedDictionary<string, double>(_axes, StringComparer.Ordinal) { [tag] = value };
        return new(axes);
    }
    /// <summary>The number of explicitly selected axes.</summary>
    public int Count => _axes.Count;
    /// <summary>The selected axis tags.</summary>
    public IEnumerable<string> Keys => _axes.Keys;
    /// <summary>The selected design coordinates.</summary>
    public IEnumerable<double> Values => _axes.Values;
    /// <summary>Gets a selected design coordinate.</summary>
    public double this[string key] => _axes[key];
    /// <summary>Tests whether an axis is explicitly selected.</summary>
    public bool ContainsKey(string key) => _axes.ContainsKey(key);
    /// <summary>Reads an explicitly selected design coordinate.</summary>
    public bool TryGetValue(string key, out double value) => _axes.TryGetValue(key, out value);
    /// <summary>Enumerates selected coordinates in tag order.</summary>
    public IEnumerator<KeyValuePair<string, double>> GetEnumerator() => _axes.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    internal string Css => _css;
    internal string Key => _key;
    private static void Validate(string tag, double value) {
        if (tag == null || tag.Length != 4 || tag.Any(c => !(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9')))
            throw new ArgumentException("An axis tag must contain four ASCII letters or digits.", nameof(tag));
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), "Axis coordinates must be finite.");
    }
}
