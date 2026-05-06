using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Svg;

internal sealed class SvgPointList {
    private readonly List<SvgPoint> _points = new();

    public SvgPointList() { }

    public SvgPointList(IEnumerable<SvgPoint>? points) {
        if (points == null) return;
        _points.AddRange(points);
    }

    public IReadOnlyList<SvgPoint> Points => _points;

    public bool IsEmpty => _points.Count == 0;

    public static SvgPointList Parse(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var numbers = ReadNumbers(value).ToArray();
        if (numbers.Length % 2 != 0) throw new FormatException("SVG point lists must contain x/y pairs.");

        var points = new SvgPointList();
        for (var i = 0; i < numbers.Length; i += 2) {
            points.Add(numbers[i], numbers[i + 1]);
        }

        return points;
    }

    public SvgPointList Add(double x, double y) =>
        Add(new SvgPoint(x, y));

    public SvgPointList Add(SvgPoint point) {
        _points.Add(point);
        return this;
    }

    public SvgPointList Insert(int index, SvgPoint point) {
        if (index < 0 || index > _points.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _points.Insert(index, point);
        return this;
    }

    public bool RemoveAt(int index) {
        if (index < 0 || index >= _points.Count) return false;
        _points.RemoveAt(index);
        return true;
    }

    public SvgPointList Replace(int index, SvgPoint point) {
        if (index < 0 || index >= _points.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _points[index] = point;
        return this;
    }

    public string ToMarkup() =>
        string.Join(" ", _points.Select(point => point.ToMarkup()));

    public override string ToString() => ToMarkup();

    private static IEnumerable<double> ReadNumbers(string value) {
        var index = 0;
        while (true) {
            SkipSeparators(value, ref index);
            if (index >= value.Length) yield break;

            var start = index;
            if (value[index] == '+' || value[index] == '-') index++;

            var hasDigit = false;
            while (index < value.Length && char.IsDigit(value[index])) {
                hasDigit = true;
                index++;
            }

            if (index < value.Length && value[index] == '.') {
                index++;
                while (index < value.Length && char.IsDigit(value[index])) {
                    hasDigit = true;
                    index++;
                }
            }

            if (!hasDigit) throw new FormatException("SVG point list contains an invalid number.");

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E')) {
                var exponentStart = index;
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-')) index++;

                var exponentDigits = false;
                while (index < value.Length && char.IsDigit(value[index])) {
                    exponentDigits = true;
                    index++;
                }

                if (!exponentDigits) index = exponentStart;
            }

            var text = value.Substring(start, index - start);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) {
                throw new FormatException("SVG point list contains an invalid number.");
            }

            yield return number;
        }
    }

    private static void SkipSeparators(string value, ref int index) {
        while (index < value.Length) {
            var ch = value[index];
            if (!char.IsWhiteSpace(ch) && ch != ',') break;
            index++;
        }
    }
}
