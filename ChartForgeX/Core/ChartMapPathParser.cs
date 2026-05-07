using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

internal static class ChartMapPathParser {
    public static List<List<ChartPoint>> ParseRings(string path) {
        if (path == null) throw new ArgumentNullException(nameof(path));

        var rings = new List<List<ChartPoint>>();
        List<ChartPoint>? currentRing = null;
        var index = 0;
        var command = '\0';
        var current = new ChartPoint(0, 0);
        var subpathStart = new ChartPoint(0, 0);

        while (true) {
            SkipSeparators(path, ref index);
            if (index >= path.Length) break;

            var next = path[index];
            if (IsCommand(next)) {
                command = next;
                index++;
                if (command == 'Z' || command == 'z') {
                    if (currentRing != null && currentRing.Count > 0) {
                        current = subpathStart;
                    }

                    currentRing = null;
                    command = '\0';
                }

                continue;
            }

            if (!IsNumberStart(next) || command == '\0') {
                throw new InvalidOperationException("Unsupported map path command.");
            }

            var x = ReadNumber(path, ref index);
            var y = ReadNumber(path, ref index);
            switch (command) {
                case 'M':
                case 'm': {
                    var point = command == 'm' ? new ChartPoint(current.X + x, current.Y + y) : new ChartPoint(x, y);
                    currentRing = new List<ChartPoint>();
                    rings.Add(currentRing);
                    currentRing.Add(point);
                    current = point;
                    subpathStart = point;
                    command = command == 'm' ? 'l' : 'L';
                    break;
                }
                case 'L':
                case 'l': {
                    if (currentRing == null) throw new InvalidOperationException("Map path lineto commands require an active subpath.");
                    var point = command == 'l' ? new ChartPoint(current.X + x, current.Y + y) : new ChartPoint(x, y);
                    currentRing.Add(point);
                    current = point;
                    break;
                }
                default:
                    throw new InvalidOperationException("Unsupported map path command.");
            }
        }

        return rings;
    }

    private static bool IsCommand(char value) {
        return value == 'M' || value == 'm' || value == 'L' || value == 'l' || value == 'Z' || value == 'z';
    }

    private static bool IsNumberStart(char value) {
        return char.IsDigit(value) || value == '-' || value == '+' || value == '.';
    }

    private static void SkipSeparators(string path, ref int index) {
        while (index < path.Length && (char.IsWhiteSpace(path[index]) || path[index] == ',')) index++;
    }

    private static double ReadNumber(string path, ref int index) {
        SkipSeparators(path, ref index);
        var start = index;
        if (index < path.Length && (path[index] == '-' || path[index] == '+')) index++;
        var hasDigit = false;
        while (index < path.Length && char.IsDigit(path[index])) { index++; hasDigit = true; }
        if (index < path.Length && path[index] == '.') {
            index++;
            while (index < path.Length && char.IsDigit(path[index])) { index++; hasDigit = true; }
        }

        if (!hasDigit) throw new InvalidOperationException("Invalid map path number.");
        if (index < path.Length && (path[index] == 'e' || path[index] == 'E')) {
            var exponent = index;
            index++;
            if (index < path.Length && (path[index] == '-' || path[index] == '+')) index++;
            var hasExponentDigit = false;
            while (index < path.Length && char.IsDigit(path[index])) { index++; hasExponentDigit = true; }
            if (!hasExponentDigit) index = exponent;
        }

        return double.Parse(path.Substring(start, index - start), CultureInfo.InvariantCulture);
    }
}
