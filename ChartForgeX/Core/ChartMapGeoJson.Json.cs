using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Core;

internal sealed class GeoJsonFeature {
    public string? Id { get; private set; }
    public Dictionary<string, GeoJsonValue> Properties { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, GeoJsonValue>? Geometry { get; private set; }
    public string? GeometryType => Geometry?.OptionalString("type");

    public static GeoJsonFeature FromValue(GeoJsonValue value) => FromObject(value.AsObject("feature"));

    public static GeoJsonFeature FromObject(Dictionary<string, GeoJsonValue> feature) {
        if (!string.Equals(feature.OptionalString("type"), "Feature", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("GeoJSON feature entries must have type Feature.");
        var item = new GeoJsonFeature {
            Id = feature.OptionalString("id"),
            Properties = feature.OptionalObject("properties") ?? new Dictionary<string, GeoJsonValue>(StringComparer.OrdinalIgnoreCase),
            Geometry = feature.OptionalObject("geometry")
        };
        return item;
    }

    public string? PropertyString(string name) => Properties.TryGetValue(name, out var value) ? value.AsOptionalString() : null;

    public void AddPropertyAliases(string name, List<string> aliases) {
        if (!Properties.TryGetValue(name, out var value)) return;
        if (value.TryAsArray(out var values)) {
            foreach (var item in values) {
                var text = item.AsOptionalString();
                if (!string.IsNullOrWhiteSpace(text)) aliases.Add(text!.Trim());
            }
        } else {
            var text = value.AsOptionalString();
            if (!string.IsNullOrWhiteSpace(text)) aliases.Add(text!.Trim());
        }
    }
}

internal sealed class GeoJsonValue {
    private readonly object? _value;

    private GeoJsonValue(object? value) {
        _value = value;
    }

    public static GeoJsonValue Parse(string json) {
        var reader = new GeoJsonReader(json, StringComparer.OrdinalIgnoreCase);
        return reader.Parse();
    }

    public static GeoJsonValue Parse(string json, IEqualityComparer<string> propertyComparer) {
        var reader = new GeoJsonReader(json, propertyComparer);
        return reader.Parse();
    }

    public static GeoJsonValue Parse(string json, IEqualityComparer<string> propertyComparer, GeoJsonReadLimits limits) {
        var reader = new GeoJsonReader(json, propertyComparer, limits);
        return reader.Parse();
    }

    public Dictionary<string, GeoJsonValue> AsObject(string context) =>
        _value as Dictionary<string, GeoJsonValue> ?? throw new ArgumentException("Expected JSON object for " + context + ".");

    public List<GeoJsonValue> AsArray(string context) =>
        _value as List<GeoJsonValue> ?? throw new ArgumentException("Expected JSON array for " + context + ".");

    public bool TryAsArray(out List<GeoJsonValue> values) {
        values = _value as List<GeoJsonValue> ?? null!;
        return values != null;
    }

    public double AsNumber(string context) {
        if (_value is double value) return value;
        if (_value is GeoJsonNumber number) return number.Value;
        throw new ArgumentException("Expected JSON number for " + context + ".");
    }

    public string AsString(string context) =>
        _value as string ?? throw new ArgumentException("Expected JSON string for " + context + ".");

    public bool AsBoolean(string context) =>
        _value is bool value ? value : throw new ArgumentException("Expected JSON boolean for " + context + ".");

    public string? AsOptionalString() {
        if (_value == null) return null;
        if (_value is string value) return value;
        if (_value is GeoJsonNumber geoJsonNumber) return geoJsonNumber.ToLookupString();
        if (_value is double numericValue) return numericValue.ToString("0.###############", CultureInfo.InvariantCulture);
        if (_value is bool flag) return flag ? "true" : "false";
        return null;
    }

    public bool IsNull => _value == null;

    public static GeoJsonValue Object(Dictionary<string, GeoJsonValue> values) => new(values);
    public static GeoJsonValue Array(List<GeoJsonValue> values) => new(values);
    public static GeoJsonValue String(string value) => new(value);
    public static GeoJsonValue Number(double value, string? text = null) => new(text == null ? value : new GeoJsonNumber(value, text));
    public static GeoJsonValue Bool(bool value) => new(value);
    public static GeoJsonValue Null() => new(null);
}

internal sealed class GeoJsonReadLimits {
    private readonly Dictionary<string, int> _arrayItemsByProperty = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _objectPropertiesByProperty = new(StringComparer.Ordinal);

    public GeoJsonReadLimits(int maximumValues, int maximumArrayItems, int maximumObjectProperties) {
        if (maximumValues <= 0) throw new ArgumentOutOfRangeException(nameof(maximumValues));
        if (maximumArrayItems <= 0) throw new ArgumentOutOfRangeException(nameof(maximumArrayItems));
        if (maximumObjectProperties <= 0) throw new ArgumentOutOfRangeException(nameof(maximumObjectProperties));
        MaximumValues = maximumValues;
        MaximumArrayItems = maximumArrayItems;
        MaximumObjectProperties = maximumObjectProperties;
    }

    public int MaximumValues { get; }
    public int MaximumArrayItems { get; }
    public int MaximumObjectProperties { get; }
    public bool RejectDuplicateProperties { get; private set; }

    public GeoJsonReadLimits RejectDuplicates() {
        RejectDuplicateProperties = true;
        return this;
    }

    public GeoJsonReadLimits LimitArray(string propertyName, int maximumItems) {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("A property name is required.", nameof(propertyName));
        if (maximumItems <= 0) throw new ArgumentOutOfRangeException(nameof(maximumItems));
        _arrayItemsByProperty[propertyName] = maximumItems;
        return this;
    }

    public GeoJsonReadLimits LimitObject(string propertyName, int maximumProperties) {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("A property name is required.", nameof(propertyName));
        if (maximumProperties <= 0) throw new ArgumentOutOfRangeException(nameof(maximumProperties));
        _objectPropertiesByProperty[propertyName] = maximumProperties;
        return this;
    }

    internal int ArrayItems(string? propertyName) =>
        propertyName != null && _arrayItemsByProperty.TryGetValue(propertyName, out int maximum) ? maximum : MaximumArrayItems;

    internal int ObjectProperties(string? propertyName) =>
        propertyName != null && _objectPropertiesByProperty.TryGetValue(propertyName, out int maximum) ? maximum : MaximumObjectProperties;
}

internal sealed class GeoJsonNumber {
    public GeoJsonNumber(double value, string losslessIntegerText) {
        Value = value;
        LosslessIntegerText = losslessIntegerText;
    }

    public double Value { get; }
    public string LosslessIntegerText { get; }

    public string ToLookupString() => LosslessIntegerText;
}

internal static class GeoJsonValueExtensions {
    public static string? OptionalString(this Dictionary<string, GeoJsonValue> values, string name) =>
        values.TryGetValue(name, out var value) ? value.AsOptionalString() : null;

    public static Dictionary<string, GeoJsonValue>? OptionalObject(this Dictionary<string, GeoJsonValue> values, string name) =>
        values.TryGetValue(name, out var value) && !value.IsNull ? value.AsObject(name) : null;

    public static List<GeoJsonValue> RequiredArray(this Dictionary<string, GeoJsonValue> values, string name) {
        if (!values.TryGetValue(name, out var value)) throw new ArgumentException("Missing required JSON array: " + name + ".");
        return value.AsArray(name);
    }
}

internal sealed class GeoJsonReader {
    private readonly string _json;
    private readonly IEqualityComparer<string> _propertyComparer;
    private readonly GeoJsonReadLimits? _limits;
    private int _position;
    private int _valuesRead;

    public GeoJsonReader(string json, IEqualityComparer<string> propertyComparer) {
        _json = json ?? throw new ArgumentNullException(nameof(json));
        _propertyComparer = propertyComparer ?? throw new ArgumentNullException(nameof(propertyComparer));
    }

    public GeoJsonReader(string json, IEqualityComparer<string> propertyComparer, GeoJsonReadLimits limits) {
        _json = json ?? throw new ArgumentNullException(nameof(json));
        _propertyComparer = propertyComparer ?? throw new ArgumentNullException(nameof(propertyComparer));
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
    }

    public GeoJsonValue Parse() {
        var value = ReadValue(preserveNumberText: false, propertyName: null);
        SkipWhiteSpace();
        if (_position != _json.Length) throw Error("Unexpected trailing JSON content.");
        return value;
    }

    private GeoJsonValue ReadValue(bool preserveNumberText, string? propertyName) {
        _valuesRead++;
        if (_limits != null && _valuesRead > _limits.MaximumValues) throw Error("JSON contains too many values.");
        SkipWhiteSpace();
        if (_position >= _json.Length) throw Error("Unexpected end of JSON.");
        var c = _json[_position];
        if (c == '{') return ReadObject(propertyName);
        if (c == '[') return ReadArray(preserveNumberText, propertyName);
        if (c == '"') return GeoJsonValue.String(ReadString());
        if (c == '-' || char.IsDigit(c)) return ReadNumber(preserveNumberText);
        if (Match("true")) return GeoJsonValue.Bool(true);
        if (Match("false")) return GeoJsonValue.Bool(false);
        if (Match("null")) return GeoJsonValue.Null();
        throw Error("Unexpected JSON token.");
    }

    private GeoJsonValue ReadObject(string? propertyName) {
        Expect('{');
        var values = new Dictionary<string, GeoJsonValue>(_propertyComparer);
        int propertyCount = 0;
        int maximumProperties = _limits?.ObjectProperties(propertyName) ?? int.MaxValue;
        SkipWhiteSpace();
        if (TryRead('}')) return GeoJsonValue.Object(values);
        while (true) {
            propertyCount++;
            if (propertyCount > maximumProperties) throw Error("JSON object contains too many properties.");
            SkipWhiteSpace();
            var key = ReadString();
            if (_limits?.RejectDuplicateProperties == true && values.ContainsKey(key)) throw Error("JSON object contains a duplicate property named '" + key + "'.");
            SkipWhiteSpace();
            Expect(':');
            GeoJsonValue value = ReadValue(!string.Equals(key, "coordinates", StringComparison.OrdinalIgnoreCase), key);
            values[key] = value;
            SkipWhiteSpace();
            if (TryRead('}')) break;
            Expect(',');
        }

        return GeoJsonValue.Object(values);
    }

    private GeoJsonValue ReadArray(bool preserveNumberText, string? propertyName) {
        Expect('[');
        var values = new List<GeoJsonValue>();
        int maximumItems = _limits?.ArrayItems(propertyName) ?? int.MaxValue;
        SkipWhiteSpace();
        if (TryRead(']')) return GeoJsonValue.Array(values);
        while (true) {
            if (values.Count >= maximumItems) throw Error("JSON array contains too many items.");
            values.Add(ReadValue(preserveNumberText, propertyName: null));
            SkipWhiteSpace();
            if (TryRead(']')) break;
            Expect(',');
        }

        return GeoJsonValue.Array(values);
    }

    private GeoJsonValue ReadNumber(bool preserveText) {
        var start = _position;
        if (_json[_position] == '-') _position++;
        ReadDigits();
        if (_position < _json.Length && _json[_position] == '.') {
            _position++;
            ReadDigits();
        }

        if (_position < _json.Length && (_json[_position] == 'e' || _json[_position] == 'E')) {
            _position++;
            if (_position < _json.Length && (_json[_position] == '+' || _json[_position] == '-')) _position++;
            ReadDigits();
        }

        var text = _json.Substring(start, _position - start);
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) throw Error("Invalid JSON number.");
        return GeoJsonValue.Number(value, preserveText && ShouldPreserveNumberText(text, value) ? text : null);
    }

    private static bool ShouldPreserveNumberText(string text, double value) {
        if (text.IndexOf('.') >= 0 || text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0) return false;
        return !string.Equals(value.ToString("0", CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
    }

    private void ReadDigits() {
        var start = _position;
        while (_position < _json.Length && char.IsDigit(_json[_position])) _position++;
        if (_position == start) throw Error("Expected JSON number digit.");
    }

    private string ReadString() {
        Expect('"');
        var buffer = new System.Text.StringBuilder();
        while (_position < _json.Length) {
            var c = _json[_position++];
            if (c == '"') return buffer.ToString();
            if (c != '\\') {
                buffer.Append(c);
                continue;
            }

            if (_position >= _json.Length) throw Error("Unexpected end of JSON string escape.");
            var escaped = _json[_position++];
            if (escaped == '"' || escaped == '\\' || escaped == '/') buffer.Append(escaped);
            else if (escaped == 'b') buffer.Append('\b');
            else if (escaped == 'f') buffer.Append('\f');
            else if (escaped == 'n') buffer.Append('\n');
            else if (escaped == 'r') buffer.Append('\r');
            else if (escaped == 't') buffer.Append('\t');
            else if (escaped == 'u') buffer.Append(ReadUnicodeEscape());
            else throw Error("Invalid JSON string escape.");
        }

        throw Error("Unterminated JSON string.");
    }

    private char ReadUnicodeEscape() {
        if (_position + 4 > _json.Length) throw Error("Invalid unicode escape.");
        var value = _json.Substring(_position, 4);
        _position += 4;
        if (!ushort.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code)) throw Error("Invalid unicode escape.");
        return (char)code;
    }

    private bool Match(string text) {
        if (_position + text.Length > _json.Length) return false;
        for (var i = 0; i < text.Length; i++) if (_json[_position + i] != text[i]) return false;
        _position += text.Length;
        return true;
    }

    private void SkipWhiteSpace() {
        while (_position < _json.Length && char.IsWhiteSpace(_json[_position])) _position++;
    }

    private bool TryRead(char c) {
        if (_position >= _json.Length || _json[_position] != c) return false;
        _position++;
        return true;
    }

    private void Expect(char c) {
        SkipWhiteSpace();
        if (!TryRead(c)) throw Error("Expected '" + c + "'.");
    }

    private ArgumentException Error(string message) => new(message + " Position " + _position.ToString(CultureInfo.InvariantCulture) + ".");
}
