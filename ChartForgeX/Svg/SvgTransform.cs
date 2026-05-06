using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Svg;

internal readonly struct SvgTransform {
    private readonly double[] _values;

    public SvgTransform(string name, IEnumerable<double>? values) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (!SvgTransformList.IsTransformName(name)) throw new ArgumentException("SVG transform name is not supported.", nameof(name));
        Name = name;
        _values = values?.ToArray() ?? Array.Empty<double>();
        if (!SvgTransformList.IsValidParameterCount(name, _values.Length)) {
            throw new ArgumentException("SVG transform has an invalid parameter count.", nameof(values));
        }
    }

    public string Name { get; }

    public IReadOnlyList<double> Values => _values;

    public SvgTransform WithValues(params double[] values) =>
        new(Name, values);

    public string ToMarkup() {
        if (_values.Length == 0) return Name + "()";

        var values = new string[_values.Length];
        for (var i = 0; i < _values.Length; i++) values[i] = SvgMarkupWriter.FormatNumber(_values[i]);
        return Name + "(" + string.Join(" ", values) + ")";
    }

    public override string ToString() => ToMarkup();
}
