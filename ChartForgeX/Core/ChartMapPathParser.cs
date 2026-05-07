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
        ChartPoint? lastCubicControl = null;
        ChartPoint? lastQuadraticControl = null;
        var previousCommand = '\0';

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
                    lastCubicControl = null;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    command = '\0';
                }

                continue;
            }

            if (!IsNumberStart(next) || command == '\0') {
                throw new InvalidOperationException("Unsupported map path command.");
            }

            switch (command) {
                case 'M':
                case 'm': {
                    var x = ReadNumber(path, ref index);
                    var y = ReadNumber(path, ref index);
                    var point = command == 'm' ? new ChartPoint(current.X + x, current.Y + y) : new ChartPoint(x, y);
                    currentRing = new List<ChartPoint>();
                    rings.Add(currentRing);
                    currentRing.Add(point);
                    current = point;
                    subpathStart = point;
                    lastCubicControl = null;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    command = command == 'm' ? 'l' : 'L';
                    break;
                }
                case 'L':
                case 'l': {
                    var x = ReadNumber(path, ref index);
                    var y = ReadNumber(path, ref index);
                    currentRing = RequireRing(currentRing);
                    var point = command == 'l' ? new ChartPoint(current.X + x, current.Y + y) : new ChartPoint(x, y);
                    currentRing.Add(point);
                    current = point;
                    lastCubicControl = null;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    break;
                }
                case 'H':
                case 'h': {
                    var x = ReadNumber(path, ref index);
                    currentRing = RequireRing(currentRing);
                    var point = new ChartPoint(command == 'h' ? current.X + x : x, current.Y);
                    currentRing.Add(point);
                    current = point;
                    lastCubicControl = null;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    break;
                }
                case 'V':
                case 'v': {
                    var y = ReadNumber(path, ref index);
                    currentRing = RequireRing(currentRing);
                    var point = new ChartPoint(current.X, command == 'v' ? current.Y + y : y);
                    currentRing.Add(point);
                    current = point;
                    lastCubicControl = null;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    break;
                }
                case 'C':
                case 'c': {
                    currentRing = RequireRing(currentRing);
                    var control1 = ReadPoint(path, ref index, current, command == 'c');
                    var control2 = ReadPoint(path, ref index, current, command == 'c');
                    var point = ReadPoint(path, ref index, current, command == 'c');
                    AddCubic(currentRing, current, control1, control2, point);
                    current = point;
                    lastCubicControl = control2;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    break;
                }
                case 'S':
                case 's': {
                    currentRing = RequireRing(currentRing);
                    var control1 = IsSmoothCubic(previousCommand) && lastCubicControl.HasValue ? Reflect(lastCubicControl.Value, current) : current;
                    var control2 = ReadPoint(path, ref index, current, command == 's');
                    var point = ReadPoint(path, ref index, current, command == 's');
                    AddCubic(currentRing, current, control1, control2, point);
                    current = point;
                    lastCubicControl = control2;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    break;
                }
                case 'Q':
                case 'q': {
                    currentRing = RequireRing(currentRing);
                    var control = ReadPoint(path, ref index, current, command == 'q');
                    var point = ReadPoint(path, ref index, current, command == 'q');
                    AddQuadratic(currentRing, current, control, point);
                    current = point;
                    lastCubicControl = null;
                    lastQuadraticControl = control;
                    previousCommand = command;
                    break;
                }
                case 'T':
                case 't': {
                    currentRing = RequireRing(currentRing);
                    var control = IsSmoothQuadratic(previousCommand) && lastQuadraticControl.HasValue ? Reflect(lastQuadraticControl.Value, current) : current;
                    var point = ReadPoint(path, ref index, current, command == 't');
                    AddQuadratic(currentRing, current, control, point);
                    current = point;
                    lastCubicControl = null;
                    lastQuadraticControl = control;
                    previousCommand = command;
                    break;
                }
                case 'A':
                case 'a': {
                    currentRing = RequireRing(currentRing);
                    var radiusX = ReadNumber(path, ref index);
                    var radiusY = ReadNumber(path, ref index);
                    var rotation = ReadNumber(path, ref index);
                    var largeArc = Math.Abs(ReadNumber(path, ref index)) > 0.5;
                    var sweep = Math.Abs(ReadNumber(path, ref index)) > 0.5;
                    var point = ReadPoint(path, ref index, current, command == 'a');
                    AddArc(currentRing, current, Math.Abs(radiusX), Math.Abs(radiusY), rotation, largeArc, sweep, point);
                    current = point;
                    lastCubicControl = null;
                    lastQuadraticControl = null;
                    previousCommand = command;
                    break;
                }
                default:
                    throw new InvalidOperationException("Unsupported map path command.");
            }
        }

        return rings;
    }

    private static bool IsCommand(char value) {
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

    private static List<ChartPoint> RequireRing(List<ChartPoint>? ring) {
        if (ring == null) throw new InvalidOperationException("Map path drawing commands require an active subpath.");
        return ring;
    }

    private static ChartPoint ReadPoint(string path, ref int index, ChartPoint current, bool relative) {
        var x = ReadNumber(path, ref index);
        var y = ReadNumber(path, ref index);
        return relative ? new ChartPoint(current.X + x, current.Y + y) : new ChartPoint(x, y);
    }

    private static ChartPoint Reflect(ChartPoint control, ChartPoint current) {
        return new ChartPoint(current.X * 2 - control.X, current.Y * 2 - control.Y);
    }

    private static bool IsSmoothCubic(char command) {
        return command == 'C' || command == 'c' || command == 'S' || command == 's';
    }

    private static bool IsSmoothQuadratic(char command) {
        return command == 'Q' || command == 'q' || command == 'T' || command == 't';
    }

    private static void AddCubic(List<ChartPoint> ring, ChartPoint start, ChartPoint control1, ChartPoint control2, ChartPoint end) {
        const int segments = 12;
        for (var i = 1; i <= segments; i++) {
            var t = i / (double)segments;
            var mt = 1 - t;
            ring.Add(new ChartPoint(
                mt * mt * mt * start.X + 3 * mt * mt * t * control1.X + 3 * mt * t * t * control2.X + t * t * t * end.X,
                mt * mt * mt * start.Y + 3 * mt * mt * t * control1.Y + 3 * mt * t * t * control2.Y + t * t * t * end.Y));
        }
    }

    private static void AddQuadratic(List<ChartPoint> ring, ChartPoint start, ChartPoint control, ChartPoint end) {
        const int segments = 10;
        for (var i = 1; i <= segments; i++) {
            var t = i / (double)segments;
            var mt = 1 - t;
            ring.Add(new ChartPoint(
                mt * mt * start.X + 2 * mt * t * control.X + t * t * end.X,
                mt * mt * start.Y + 2 * mt * t * control.Y + t * t * end.Y));
        }
    }

    private static void AddArc(List<ChartPoint> ring, ChartPoint start, double radiusX, double radiusY, double rotation, bool largeArc, bool sweep, ChartPoint end) {
        if ((Math.Abs(start.X - end.X) < 0.000001 && Math.Abs(start.Y - end.Y) < 0.000001) || radiusX <= 0 || radiusY <= 0) {
            ring.Add(end);
            return;
        }

        var phi = rotation * Math.PI / 180.0;
        var cosPhi = Math.Cos(phi);
        var sinPhi = Math.Sin(phi);
        var dx = (start.X - end.X) / 2.0;
        var dy = (start.Y - end.Y) / 2.0;
        var x1Prime = cosPhi * dx + sinPhi * dy;
        var y1Prime = -sinPhi * dx + cosPhi * dy;
        var rx2 = radiusX * radiusX;
        var ry2 = radiusY * radiusY;
        var x1p2 = x1Prime * x1Prime;
        var y1p2 = y1Prime * y1Prime;
        var radiiScale = x1p2 / rx2 + y1p2 / ry2;
        if (radiiScale > 1) {
            var scale = Math.Sqrt(radiiScale);
            radiusX *= scale;
            radiusY *= scale;
            rx2 = radiusX * radiusX;
            ry2 = radiusY * radiusY;
        }

        var denominator = rx2 * y1p2 + ry2 * x1p2;
        if (denominator <= 0) {
            ring.Add(end);
            return;
        }

        var sign = largeArc == sweep ? -1.0 : 1.0;
        var factor = sign * Math.Sqrt(Math.Max(0, (rx2 * ry2 - denominator) / denominator));
        var cxPrime = factor * radiusX * y1Prime / radiusY;
        var cyPrime = -factor * radiusY * x1Prime / radiusX;
        var centerX = cosPhi * cxPrime - sinPhi * cyPrime + (start.X + end.X) / 2.0;
        var centerY = sinPhi * cxPrime + cosPhi * cyPrime + (start.Y + end.Y) / 2.0;
        var ux = (x1Prime - cxPrime) / radiusX;
        var uy = (y1Prime - cyPrime) / radiusY;
        var vx = (-x1Prime - cxPrime) / radiusX;
        var vy = (-y1Prime - cyPrime) / radiusY;
        var startAngle = Angle(1, 0, ux, uy);
        var delta = Angle(ux, uy, vx, vy);
        if (!sweep && delta > 0) delta -= Math.PI * 2;
        if (sweep && delta < 0) delta += Math.PI * 2;

        var segments = Math.Max(4, (int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 8.0)));
        for (var i = 1; i <= segments; i++) {
            var theta = startAngle + delta * i / segments;
            var x = centerX + cosPhi * radiusX * Math.Cos(theta) - sinPhi * radiusY * Math.Sin(theta);
            var y = centerY + sinPhi * radiusX * Math.Cos(theta) + cosPhi * radiusY * Math.Sin(theta);
            ring.Add(new ChartPoint(x, y));
        }
    }

    private static double Angle(double ux, double uy, double vx, double vy) {
        var length = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
        if (length <= 0) return 0;
        var value = Math.Max(-1, Math.Min(1, (ux * vx + uy * vy) / length));
        var angle = Math.Acos(value);
        return ux * vy - uy * vx < 0 ? -angle : angle;
    }
}
