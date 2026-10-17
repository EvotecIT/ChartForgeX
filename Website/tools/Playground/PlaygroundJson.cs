using System;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Playground;

/// <summary>Minimal reflection-free JSON writer so the engine needs neither System.Text.Json nor trimming annotations.</summary>
internal sealed class PlaygroundJson {
    private readonly StringBuilder _builder = new();
    private bool _needsComma;

    public PlaygroundJson BeginObject() => Open('{');
    public PlaygroundJson EndObject() => Close('}');
    public PlaygroundJson BeginArray() => Open('[');
    public PlaygroundJson EndArray() => Close(']');

    public PlaygroundJson BeginObject(string name) {
        Name(name);
        return Open('{');
    }

    public PlaygroundJson BeginArray(string name) {
        Name(name);
        return Open('[');
    }

    public PlaygroundJson Property(string name, string? value) {
        Name(name);
        if (value == null) _builder.Append("null");
        else WriteString(value);
        _needsComma = true;
        return this;
    }

    public PlaygroundJson Property(string name, bool value) {
        Name(name);
        _builder.Append(value ? "true" : "false");
        _needsComma = true;
        return this;
    }

    public PlaygroundJson Property(string name, int? value) {
        Name(name);
        _builder.Append(value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null");
        _needsComma = true;
        return this;
    }

    public PlaygroundJson Number(string name, double value) {
        Name(name);
        _builder.Append(double.IsFinite(value) ? Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture) : "null");
        _needsComma = true;
        return this;
    }

    public PlaygroundJson Value(string value) {
        Comma();
        WriteString(value);
        _needsComma = true;
        return this;
    }

    public override string ToString() => _builder.ToString();

    private PlaygroundJson Open(char token) {
        Comma();
        _builder.Append(token);
        _needsComma = false;
        return this;
    }

    private PlaygroundJson Close(char token) {
        _builder.Append(token);
        _needsComma = true;
        return this;
    }

    private void Name(string name) {
        Comma();
        WriteString(name);
        _builder.Append(':');
        _needsComma = false;
    }

    private void Comma() {
        if (_needsComma) _builder.Append(',');
    }

    private void WriteString(string value) {
        _builder.Append('"');
        foreach (var c in value) {
            switch (c) {
                case '"': _builder.Append("\\\""); break;
                case '\\': _builder.Append("\\\\"); break;
                case '\n': _builder.Append("\\n"); break;
                case '\r': _builder.Append("\\r"); break;
                case '\t': _builder.Append("\\t"); break;
                default:
                    if (c < ' ' || c == (char)0x2028 || c == (char)0x2029) _builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else _builder.Append(c);
                    break;
            }
        }
        _builder.Append('"');
    }
}
