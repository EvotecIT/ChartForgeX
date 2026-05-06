using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Svg;

internal sealed class SvgPathData {
    private readonly List<SvgPathCommand> _commands = new();

    public SvgPathData() { }

    public SvgPathData(IEnumerable<SvgPathCommand>? commands) {
        if (commands == null) return;
        _commands.AddRange(commands);
    }

    public IReadOnlyList<SvgPathCommand> Commands => _commands;

    public bool IsEmpty => _commands.Count == 0;

    public static SvgPathData Parse(string pathData) {
        if (pathData == null) throw new ArgumentNullException(nameof(pathData));

        var parser = new Parser(pathData);
        return parser.Parse();
    }

    public SvgPathData Add(SvgPathCommand command) {
        _commands.Add(command);
        return this;
    }

    public SvgPathData Insert(int index, SvgPathCommand command) {
        if (index < 0 || index > _commands.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _commands.Insert(index, command);
        return this;
    }

    public bool RemoveAt(int index) {
        if (index < 0 || index >= _commands.Count) return false;
        _commands.RemoveAt(index);
        return true;
    }

    public SvgPathData Replace(int index, SvgPathCommand command) {
        if (index < 0 || index >= _commands.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _commands[index] = command;
        return this;
    }

    public string ToMarkup() =>
        string.Join(" ", _commands.Select(command => command.ToMarkup()));

    public override string ToString() => ToMarkup();

    internal static bool IsCommand(char value) {
        switch (value) {
            case 'M':
            case 'm':
            case 'L':
            case 'l':
            case 'H':
            case 'h':
            case 'V':
            case 'v':
            case 'C':
            case 'c':
            case 'S':
            case 's':
            case 'Q':
            case 'q':
            case 'T':
            case 't':
            case 'A':
            case 'a':
            case 'Z':
            case 'z':
                return true;
            default:
                return false;
        }
    }

    internal static int GetParameterCount(char command) {
        switch (char.ToUpperInvariant(command)) {
            case 'M':
            case 'L':
            case 'T':
                return 2;
            case 'H':
            case 'V':
                return 1;
            case 'C':
                return 6;
            case 'S':
            case 'Q':
                return 4;
            case 'A':
                return 7;
            case 'Z':
                return 0;
            default:
                throw new ArgumentException("SVG path command is not supported.", nameof(command));
        }
    }

    private sealed class Parser {
        private readonly string _value;
        private int _index;

        public Parser(string value) {
            _value = value;
        }

        public SvgPathData Parse() {
            var path = new SvgPathData();
            char command = '\0';

            while (true) {
                SkipSeparators();
                if (IsEnd) break;

                if (IsCommand(_value[_index])) {
                    command = _value[_index++];
                } else if (command == '\0') {
                    throw Error("SVG path data must start with a command.");
                }

                if (char.ToUpperInvariant(command) == 'Z') {
                    path.Add(new SvgPathCommand(command, Array.Empty<double>()));
                    command = '\0';
                    continue;
                }

                var parameterCount = GetParameterCount(command);
                var firstMove = char.ToUpperInvariant(command) == 'M';
                var emitted = false;

                while (true) {
                    SkipSeparators();
                    if (IsEnd || IsCommand(_value[_index])) break;

                    var parameters = new double[parameterCount];
                    for (var i = 0; i < parameterCount; i++) {
                        parameters[i] = ReadNumber();
                        SkipSeparators();
                    }

                    var emittedCommand = command;
                    if (firstMove && emitted) emittedCommand = char.IsLower(command) ? 'l' : 'L';
                    path.Add(new SvgPathCommand(emittedCommand, parameters));
                    emitted = true;
                }

                if (!emitted) throw Error("SVG path command is missing parameters.");
            }

            return path;
        }

        private bool IsEnd => _index >= _value.Length;

        private void SkipSeparators() {
            while (!IsEnd) {
                var ch = _value[_index];
                if (!char.IsWhiteSpace(ch) && ch != ',') break;
                _index++;
            }
        }

        private double ReadNumber() {
            SkipSeparators();
            if (IsEnd) throw Error("SVG path number is missing.");

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

            if (!hasDigit) throw Error("SVG path number is invalid.");

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
                throw Error("SVG path number is invalid.");
            }

            return number;
        }

        private FormatException Error(string message) =>
            new(message + " Position: " + _index.ToString(CultureInfo.InvariantCulture) + ".");
    }
}
