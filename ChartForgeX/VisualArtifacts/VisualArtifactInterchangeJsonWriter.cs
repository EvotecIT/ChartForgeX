using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class VisualArtifactInterchangeJsonWriter {
    private sealed class Context {
        public Context(bool isObject) { IsObject = isObject; }
        public bool IsObject { get; }
        public bool First { get; set; } = true;
        public bool AwaitingValue { get; set; }
    }

    private readonly StringBuilder _buffer = new(4096);
    private readonly Stack<Context> _contexts = new();
    private readonly bool _writeText;
    private readonly bool _enforceValueLimit;
    private readonly bool _escapeMarkupUnsafeCharacters;
    private bool _hasRoot;
    private long _valuesWritten;
    private int _charactersWritten;
    private int _utf8BytesWritten;

    public VisualArtifactInterchangeJsonWriter(bool writeText = true, bool enforceValueLimit = true, bool escapeMarkupUnsafeCharacters = false) {
        _writeText = writeText;
        _enforceValueLimit = enforceValueLimit;
        _escapeMarkupUnsafeCharacters = escapeMarkupUnsafeCharacters;
    }

    public long ValuesWritten => _valuesWritten;
    internal int CharactersWritten => _charactersWritten;
    internal int Utf8BytesWritten => _utf8BytesWritten;

    public void StartObject() {
        EnsureContainerDepth();
        BeforeValue();
        Append('{');
        EnsureWithinLimit();
        _contexts.Push(new Context(isObject: true));
    }

    public void EndObject() {
        Context context = RequireContext(isObject: true);
        if (context.AwaitingValue) throw new InvalidOperationException("A JSON property is missing its value.");
        _contexts.Pop();
        Append('}');
        EnsureWithinLimit();
    }

    public void StartArray() {
        EnsureContainerDepth();
        BeforeValue();
        Append('[');
        EnsureWithinLimit();
        _contexts.Push(new Context(isObject: false));
    }

    public void EndArray() {
        RequireContext(isObject: false);
        _contexts.Pop();
        Append(']');
        EnsureWithinLimit();
    }

    public void Property(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        Context context = RequireContext(isObject: true);
        if (context.AwaitingValue) throw new InvalidOperationException("The prior JSON property is missing its value.");
        Separator(context);
        WriteEscapedString(name);
        Append(':');
        EnsureWithinLimit();
        context.AwaitingValue = true;
    }

    public void String(string? value) {
        BeforeValue();
        if (value == null) Append("null");
        else WriteEscapedString(value);
        EnsureWithinLimit();
    }

    public void Number(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), value, "JSON numbers must be finite.");
        BeforeValue();
        Append(value.ToString("G17", CultureInfo.InvariantCulture));
        EnsureWithinLimit();
    }

    public void Number(int value) {
        BeforeValue();
        Append(value.ToString(CultureInfo.InvariantCulture));
        EnsureWithinLimit();
    }

    public void Boolean(bool value) {
        BeforeValue();
        Append(value ? "true" : "false");
        EnsureWithinLimit();
    }

    /// <summary>Writes an already parsed JSON value through the same escaping, value and size bounds.</summary>
    internal void Value(GeoJsonValue value) {
        switch (value.RawValue) {
            case Dictionary<string, GeoJsonValue> properties:
                StartObject();
                foreach (var property in properties) { Property(property.Key); Value(property.Value); }
                EndObject(); break;
            case List<GeoJsonValue> items:
                StartArray(); foreach (var item in items) Value(item); EndArray(); break;
            case string text: String(text); break;
            case bool flag: Boolean(flag); break;
            case double number: Number(number); break;
            case GeoJsonNumber parsedNumber:
                // OriginalText is emitted only from the canonical finite-number parser, never from arbitrary raw text.
                BeforeValue(); Append(parsedNumber.OriginalText); EnsureWithinLimit(); break;
            case null: String(null); break;
            default: throw new ArgumentException("Unsupported parsed JSON value.", nameof(value));
        }
    }

    public override string ToString() {
        if (_contexts.Count != 0) throw new InvalidOperationException("The JSON document is incomplete.");
        if (!_writeText) throw new InvalidOperationException("This writer was created for value counting only.");
        return _buffer.ToString();
    }

    private void EnsureContainerDepth() {
        if (_contexts.Count >= VisualArtifactInterchangeJson.MaximumJsonDepth)
            throw new InvalidOperationException("The interchange JSON exceeds the maximum supported depth.");
    }

    private void BeforeValue() {
        _valuesWritten++;
        if (_enforceValueLimit && _valuesWritten > VisualArtifactInterchangeValidation.MaximumJsonValues) {
            throw new InvalidOperationException("The interchange JSON contains too many values.");
        }
        if (_contexts.Count == 0) {
            if (_hasRoot) throw new InvalidOperationException("The JSON document already has a root value.");
            _hasRoot = true;
            return;
        }

        Context context = _contexts.Peek();
        if (context.IsObject) {
            if (!context.AwaitingValue) throw new InvalidOperationException("JSON object values require a property name.");
            context.AwaitingValue = false;
        } else {
            Separator(context);
        }
    }

    private void Separator(Context context) {
        if (!context.First) Append(',');
        context.First = false;
    }

    private Context RequireContext(bool isObject) {
        if (_contexts.Count == 0 || _contexts.Peek().IsObject != isObject) throw new InvalidOperationException(isObject ? "A JSON object context is required." : "A JSON array context is required.");
        return _contexts.Peek();
    }

    private void WriteEscapedString(string value) {
        Append('"');
        for (var index = 0; index < value.Length; index++) {
            char c = value[index];
            if (char.IsHighSurrogate(c)) {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1])) {
                    throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(value));
                }
                var low = value[++index];
                if (_escapeMarkupUnsafeCharacters && !SvgMarkupWriter.IsMarkupScalar(char.ConvertToUtf32(c, low))) {
                    WriteUnicodeEscape(c); WriteUnicodeEscape(low);
                } else { AppendSurrogatePair(c, low); }
                continue;
            }
            if (char.IsLowSurrogate(c)) {
                throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(value));
            }
            switch (c) {
                case '"': Append("\\\""); break;
                case '\\': Append("\\\\"); break;
                case '\b': Append("\\b"); break;
                case '\f': Append("\\f"); break;
                case '\n': Append("\\n"); break;
                case '\r': Append("\\r"); break;
                case '\t': Append("\\t"); break;
                default:
                    if (c < 0x20 || _escapeMarkupUnsafeCharacters && !SvgMarkupWriter.IsMarkupScalar(c)) {
                        WriteUnicodeEscape(c);
                    } else {
                        Append(c);
                    }
                    break;
            }
        }
        Append('"');
        EnsureWithinLimit();
    }

    private void WriteUnicodeEscape(char value) {
        Append("\\u"); Append(((int)value).ToString("x4", CultureInfo.InvariantCulture));
    }

    private void EnsureWithinLimit() {
        if (_charactersWritten > VisualArtifactInterchangeValidation.MaximumJsonCharacters)
            throw new InvalidOperationException("The interchange JSON exceeds the maximum supported size.");
        if (_utf8BytesWritten > VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes)
            throw new ArgumentOutOfRangeException("json", _utf8BytesWritten,
                "Interchange UTF-8 JSON exceeds the existing supported byte budget.");
    }

    private void Append(char value) {
        _charactersWritten = checked(_charactersWritten + 1);
        _utf8BytesWritten = checked(_utf8BytesWritten + (value < 0x80 ? 1 : value < 0x800 ? 2 : 3));
        if (_writeText) _buffer.Append(value);
    }

    private void AppendSurrogatePair(char high, char low) {
        // WriteEscapedString already validated the scalar. Raw supplementary text has
        // two UTF16 units and four UTF8 bytes; escaped pairs use the ordinary ASCII path.
        _charactersWritten = checked(_charactersWritten + 2);
        _utf8BytesWritten = checked(_utf8BytesWritten + 4);
        if (_writeText) _buffer.Append(high).Append(low);
    }

    private void Append(string value) {
        _charactersWritten = checked(_charactersWritten + value.Length);
        _utf8BytesWritten = checked(_utf8BytesWritten + Encoding.UTF8.GetByteCount(value));
        if (_writeText) _buffer.Append(value);
    }
}
