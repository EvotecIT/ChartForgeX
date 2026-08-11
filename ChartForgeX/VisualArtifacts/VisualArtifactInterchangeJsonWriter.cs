using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.VisualArtifacts;

internal sealed class VisualArtifactInterchangeJsonWriter {
    private sealed class Context {
        public Context(bool isObject) { IsObject = isObject; }
        public bool IsObject { get; }
        public bool First { get; set; } = true;
        public bool AwaitingValue { get; set; }
    }

    private readonly StringBuilder _buffer = new(4096);
    private readonly Stack<Context> _contexts = new();
    private bool _hasRoot;
    private int _valuesWritten;

    public void StartObject() {
        BeforeValue();
        _buffer.Append('{');
        EnsureWithinLimit();
        _contexts.Push(new Context(isObject: true));
    }

    public void EndObject() {
        Context context = RequireContext(isObject: true);
        if (context.AwaitingValue) throw new InvalidOperationException("A JSON property is missing its value.");
        _contexts.Pop();
        _buffer.Append('}');
        EnsureWithinLimit();
    }

    public void StartArray() {
        BeforeValue();
        _buffer.Append('[');
        EnsureWithinLimit();
        _contexts.Push(new Context(isObject: false));
    }

    public void EndArray() {
        RequireContext(isObject: false);
        _contexts.Pop();
        _buffer.Append(']');
        EnsureWithinLimit();
    }

    public void Property(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        Context context = RequireContext(isObject: true);
        if (context.AwaitingValue) throw new InvalidOperationException("The prior JSON property is missing its value.");
        Separator(context);
        WriteEscapedString(name);
        _buffer.Append(':');
        EnsureWithinLimit();
        context.AwaitingValue = true;
    }

    public void String(string? value) {
        BeforeValue();
        if (value == null) _buffer.Append("null");
        else WriteEscapedString(value);
        EnsureWithinLimit();
    }

    public void Number(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value), value, "JSON numbers must be finite.");
        BeforeValue();
        _buffer.Append(value.ToString("G17", CultureInfo.InvariantCulture));
        EnsureWithinLimit();
    }

    public void Number(int value) {
        BeforeValue();
        _buffer.Append(value.ToString(CultureInfo.InvariantCulture));
        EnsureWithinLimit();
    }

    public void Boolean(bool value) {
        BeforeValue();
        _buffer.Append(value ? "true" : "false");
        EnsureWithinLimit();
    }

    public override string ToString() {
        if (_contexts.Count != 0) throw new InvalidOperationException("The JSON document is incomplete.");
        return _buffer.ToString();
    }

    private void BeforeValue() {
        _valuesWritten++;
        if (_valuesWritten > VisualArtifactInterchangeValidation.MaximumJsonValues) {
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
        if (!context.First) _buffer.Append(',');
        context.First = false;
    }

    private Context RequireContext(bool isObject) {
        if (_contexts.Count == 0 || _contexts.Peek().IsObject != isObject) throw new InvalidOperationException(isObject ? "A JSON object context is required." : "A JSON array context is required.");
        return _contexts.Peek();
    }

    private void WriteEscapedString(string value) {
        _buffer.Append('"');
        for (var index = 0; index < value.Length; index++) {
            char c = value[index];
            if (char.IsHighSurrogate(c)) {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1])) {
                    throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(value));
                }
                _buffer.Append(c).Append(value[++index]);
                continue;
            }
            if (char.IsLowSurrogate(c)) {
                throw new ArgumentException("Interchange JSON strings cannot contain unpaired UTF-16 surrogate characters.", nameof(value));
            }
            switch (c) {
                case '"': _buffer.Append("\\\""); break;
                case '\\': _buffer.Append("\\\\"); break;
                case '\b': _buffer.Append("\\b"); break;
                case '\f': _buffer.Append("\\f"); break;
                case '\n': _buffer.Append("\\n"); break;
                case '\r': _buffer.Append("\\r"); break;
                case '\t': _buffer.Append("\\t"); break;
                default:
                    if (c < 0x20) _buffer.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else _buffer.Append(c);
                    break;
            }
        }
        _buffer.Append('"');
        EnsureWithinLimit();
    }

    private void EnsureWithinLimit() {
        if (_buffer.Length > VisualArtifactInterchangeValidation.MaximumJsonCharacters) {
            throw new InvalidOperationException("The interchange JSON exceeds the maximum supported size.");
        }
    }
}
