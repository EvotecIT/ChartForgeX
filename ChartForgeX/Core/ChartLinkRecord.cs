using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Core;

/// <summary>Validates and encodes the record keys and field values that linked views exchange with a mark.</summary>
internal static class ChartLinkRecord {
    internal const int MaximumKeys = 10000;
    private static readonly IReadOnlyList<string> NoKeys = Array.AsReadOnly(Array.Empty<string>());
    private static readonly IReadOnlyDictionary<string, object> NoFields = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

    internal static IReadOnlyList<string> Keys(IEnumerable<string>? keys, string parameter) {
        if (keys == null) return NoKeys;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        foreach (var key in keys) {
            if (string.IsNullOrEmpty(key) || HasControl(key)) throw new ArgumentException("Record keys must be non-empty text without control characters.", parameter);
            if (seen.Add(key)) list.Add(key);
            if (list.Count > MaximumKeys) throw new ArgumentException("A mark represents at most 10000 record keys.", parameter);
        }
        return list.Count == 0 ? NoKeys : list.AsReadOnly();
    }

    internal static IReadOnlyDictionary<string, object> Fields(IReadOnlyDictionary<string, object>? fields, string parameter) {
        if (fields == null || fields.Count == 0) return NoFields;
        var sorted = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var entry in fields) {
            if (string.IsNullOrEmpty(entry.Key) || entry.Key.Length > 128 || HasControl(entry.Key))
                throw new ArgumentException("Field names must be 1 to 128 characters without control characters.", parameter);
            sorted.Add(entry.Key, Normalize(entry.Value, parameter));
        }
        return new ReadOnlyDictionary<string, object>(sorted);
    }

    /// <summary>Merges a series' base fields under an item's own fields.</summary>
    internal static IReadOnlyDictionary<string, object> Merge(IReadOnlyDictionary<string, object>? series, IReadOnlyDictionary<string, object> item) {
        if (series == null || series.Count == 0 || ReferenceEquals(series, item)) return item;
        if (item.Count == 0) return series;
        var merged = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var entry in series) merged[entry.Key] = entry.Value;
        foreach (var entry in item) merged[entry.Key] = entry.Value;
        return new ReadOnlyDictionary<string, object>(merged);
    }

    internal static string KeysJson(IReadOnlyList<string> keys) {
        var builder = new StringBuilder("[");
        for (var i = 0; i < keys.Count; i++) { if (i > 0) builder.Append(','); AppendString(builder, keys[i]); }
        return builder.Append(']').ToString();
    }

    internal static string FieldsJson(IReadOnlyDictionary<string, object> fields) {
        var builder = new StringBuilder("{");
        var first = true;
        foreach (var entry in fields) {
            if (!first) builder.Append(',');
            first = false;
            AppendString(builder, entry.Key); builder.Append(':'); AppendValue(builder, entry.Value);
        }
        return builder.Append('}').ToString();
    }

    internal static void AppendValue(StringBuilder builder, object value) {
        switch (value) {
            case bool flag: builder.Append(flag ? "true" : "false"); break;
            case double number: builder.Append(number.ToString("R", CultureInfo.InvariantCulture)); break;
            default: AppendString(builder, (string)value); break;
        }
    }

    internal static void AppendString(StringBuilder builder, string value) {
        builder.Append('"');
        foreach (var c in value) {
            switch (c) {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '<': builder.Append("\\u003c"); break;
                case '>': builder.Append("\\u003e"); break;
                case '&': builder.Append("\\u0026"); break;
                default:
                    if (c < ' ' || c == (char)0x2028 || c == (char)0x2029) builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else builder.Append(c);
                    break;
            }
        }
        builder.Append('"');
    }

    private static object Normalize(object? value, string parameter) {
        switch (value) {
            case string text: return text;
            case bool flag: return flag;
            case DateTimeOffset offset: return Instant(offset.UtcDateTime);
            case DateTime date:
                if (date.Kind == DateTimeKind.Unspecified) throw new ArgumentException("Date field values need a UTC or local DateTimeKind so the instant is unambiguous.", parameter);
                return Instant(date.ToUniversalTime());
            case null: throw new ArgumentException("Field values must not be null.", parameter);
        }
        double number;
        try { number = Convert.ToDouble(value, CultureInfo.InvariantCulture); }
        catch (Exception exception) when (exception is InvalidCastException || exception is FormatException || exception is OverflowException) {
            throw new ArgumentException("Field values must be text, booleans, numbers or dates.", parameter);
        }
        if (value is char || value is Enum || double.IsNaN(number) || double.IsInfinity(number))
            throw new ArgumentException("Field values must be text, booleans, finite numbers or dates.", parameter);
        return number;
    }

    private static string Instant(DateTime utc) => utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private static bool HasControl(string value) {
        foreach (var c in value) if (char.IsControl(c)) return true;
        return false;
    }
}
