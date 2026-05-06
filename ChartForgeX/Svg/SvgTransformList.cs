using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Svg;

internal sealed class SvgTransformList {
    private readonly List<SvgTransform> _transforms = new();

    public SvgTransformList() { }

    public SvgTransformList(IEnumerable<SvgTransform>? transforms) {
        if (transforms == null) return;
        _transforms.AddRange(transforms);
    }

    public IReadOnlyList<SvgTransform> Transforms => _transforms;

    public bool IsEmpty => _transforms.Count == 0;

    public static SvgTransformList Parse(string transform) {
        if (transform == null) throw new ArgumentNullException(nameof(transform));

        var parser = new Parser(transform);
        return parser.Parse();
    }

    public SvgTransformList Add(SvgTransform transform) {
        _transforms.Add(transform);
        return this;
    }

    public SvgTransformList Insert(int index, SvgTransform transform) {
        if (index < 0 || index > _transforms.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _transforms.Insert(index, transform);
        return this;
    }

    public bool RemoveAt(int index) {
        if (index < 0 || index >= _transforms.Count) return false;
        _transforms.RemoveAt(index);
        return true;
    }

    public SvgTransformList Replace(int index, SvgTransform transform) {
        if (index < 0 || index >= _transforms.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _transforms[index] = transform;
        return this;
    }

    public string ToMarkup() =>
        string.Join(" ", _transforms.Select(transform => transform.ToMarkup()));

    public override string ToString() => ToMarkup();

    internal static bool IsTransformName(string name) {
        switch (name) {
            case "matrix":
            case "translate":
            case "scale":
            case "rotate":
            case "skewX":
            case "skewY":
                return true;
            default:
                return false;
        }
    }

    internal static bool IsValidParameterCount(string name, int count) {
        switch (name) {
            case "matrix":
                return count == 6;
            case "translate":
            case "scale":
                return count == 1 || count == 2;
            case "rotate":
                return count == 1 || count == 3;
            case "skewX":
            case "skewY":
                return count == 1;
            default:
                return false;
        }
    }

    private sealed class Parser {
        private readonly string _value;
        private int _index;

        public Parser(string value) {
            _value = value;
        }

        public SvgTransformList Parse() {
            var transforms = new SvgTransformList();
            while (true) {
                SkipSeparators();
                if (IsEnd) break;

                var name = ReadName();
                SkipWhitespace();
                Expect('(');
                var values = ReadValues();
                Expect(')');
                transforms.Add(new SvgTransform(name, values));
            }

            return transforms;
        }

        private bool IsEnd => _index >= _value.Length;

        private void SkipSeparators() {
            while (!IsEnd) {
                var ch = _value[_index];
                if (!char.IsWhiteSpace(ch) && ch != ',') break;
                _index++;
            }
        }

        private void SkipWhitespace() {
            while (!IsEnd && char.IsWhiteSpace(_value[_index])) _index++;
        }

        private string ReadName() {
            SkipSeparators();
            var start = _index;
            while (!IsEnd && char.IsLetter(_value[_index])) _index++;
            if (start == _index) throw Error("SVG transform name is missing.");
            return _value.Substring(start, _index - start);
        }

        private double[] ReadValues() {
            var values = new List<double>();
            while (true) {
                SkipSeparators();
                if (IsEnd || _value[_index] == ')') break;
                values.Add(ReadNumber());
            }

            return values.ToArray();
        }

        private double ReadNumber() {
            SkipSeparators();
            if (IsEnd) throw Error("SVG transform number is missing.");

            var start = _index;
            if (_value[_index] == '+' || _value[_index] == '-') _index++;

            var hasDigit = false;
            while (!IsEnd && char.IsDigit(_value[_index])) {
                hasDigit = true;
                _index++;
            }

            if (!IsEnd && _value[_index] == '.') {
                _index++;
                while (!IsEnd && char.IsDigit(_value[_index])) {
                    hasDigit = true;
                    _index++;
                }
            }

            if (!hasDigit) throw Error("SVG transform number is invalid.");

            if (!IsEnd && (_value[_index] == 'e' || _value[_index] == 'E')) {
                var exponentStart = _index;
                _index++;
                if (!IsEnd && (_value[_index] == '+' || _value[_index] == '-')) _index++;

                var exponentDigits = false;
                while (!IsEnd && char.IsDigit(_value[_index])) {
                    exponentDigits = true;
                    _index++;
                }

                if (!exponentDigits) _index = exponentStart;
            }

            var text = _value.Substring(start, _index - start);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) {
                throw Error("SVG transform number is invalid.");
            }

            return number;
        }

        private void Expect(char expected) {
            if (IsEnd || _value[_index] != expected) {
                throw Error("SVG transform expected '" + expected + "'.");
            }

            _index++;
        }

        private FormatException Error(string message) =>
            new(message + " Position: " + _index.ToString(CultureInfo.InvariantCulture) + ".");
    }
}
