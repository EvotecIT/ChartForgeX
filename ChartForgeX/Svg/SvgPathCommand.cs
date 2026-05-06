using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Svg;

internal readonly struct SvgPathCommand {
    private readonly double[] _values;

    public SvgPathCommand(char command, IEnumerable<double>? values) {
        if (!SvgPathData.IsCommand(command)) throw new ArgumentException("SVG path command is not supported.", nameof(command));
        Command = command;
        _values = values?.ToArray() ?? Array.Empty<double>();
        var expected = SvgPathData.GetParameterCount(command);
        if (expected != _values.Length) {
            throw new ArgumentException("SVG path command has an invalid parameter count.", nameof(values));
        }
    }

    public char Command { get; }

    public IReadOnlyList<double> Values => _values;

    public SvgPathCommand WithValues(params double[] values) =>
        new(Command, values);

    public string ToMarkup() {
        if (_values.Length == 0) return Command.ToString();

        var parts = new string[_values.Length + 1];
        parts[0] = Command.ToString();
        for (var i = 0; i < _values.Length; i++) parts[i + 1] = SvgMarkupWriter.FormatNumber(_values[i]);
        return string.Join(" ", parts);
    }

    public override string ToString() => ToMarkup();
}
