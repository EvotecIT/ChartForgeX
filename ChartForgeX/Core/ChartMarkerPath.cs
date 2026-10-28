using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable, closed numeric marker path in the unit square from minus one through one.</summary>
/// <remarks>
/// Coordinates and cubic control points are finite and bounded by the unit square. Each contour closes at its start;
/// multiple contours use even-odd fill, including holes. The marker radius scales these unit coordinates, so radius
/// encoding bounds the painted geometry before its outline. Area-encoded bubbles instead use reference-circle area
/// and may extend beyond that reference radius. Curved fill areas remove translation and normalize the finite
/// coordinate extent before using the shared native flattener at 1024 pixels per extent, bounded to 1024 edges
/// and 32768 intersection levels. Measurement restores the original linear scale; exporters retain the original
/// typed curves. No SVG markup, script or external resource is accepted.
/// </remarks>
public sealed class ChartMarkerPath {
    private const int MaximumCommands = 128;
    private readonly IReadOnlyList<ChartPathCommand> _commands;
    private readonly Lazy<double> _areaFactor;
    private readonly Lazy<ChartRect> _bounds;

    private ChartMarkerPath(ChartPathCommand[] commands) {
        _commands = Array.AsReadOnly(commands);
        _bounds = new Lazy<ChartRect>(() => new ChartPath(_commands).Bounds(), true);
        _areaFactor = new Lazy<double>(() => ChartMarkerGeometry.CustomCircleAreaRadiusFactor(this), true);
    }

    /// <summary>Starts a closed contour at finite unit coordinates; at most 128 commands are retained.</summary>
    public static ChartMarkerPath Start(double x, double y) {
        Coordinate(x, nameof(x)); Coordinate(y, nameof(y));
        return new ChartMarkerPath(new[] { ChartPathCommand.MoveTo(x, y) });
    }

    /// <summary>Starts another closed contour, for a hole or a disconnected part, without changing this path.</summary>
    public ChartMarkerPath MoveTo(double x, double y) {
        Coordinate(x, nameof(x)); Coordinate(y, nameof(y));
        return Append(ChartPathCommand.MoveTo(x, y));
    }

    /// <summary>Appends a straight segment to finite unit coordinates, returning a detached path.</summary>
    public ChartMarkerPath LineTo(double x, double y) {
        Coordinate(x, nameof(x)); Coordinate(y, nameof(y));
        return Append(ChartPathCommand.LineTo(x, y));
    }

    /// <summary>Appends a cubic segment whose endpoint and both control points remain in the unit square.</summary>
    public ChartMarkerPath CubicTo(double firstX, double firstY, double secondX, double secondY, double x, double y) {
        Coordinate(firstX, nameof(firstX)); Coordinate(firstY, nameof(firstY));
        Coordinate(secondX, nameof(secondX)); Coordinate(secondY, nameof(secondY));
        Coordinate(x, nameof(x)); Coordinate(y, nameof(y));
        return Append(ChartPathCommand.CubicTo(firstX, firstY, secondX, secondY, x, y));
    }

    internal IReadOnlyList<ChartPathCommand> Commands => _commands;
    // Derived once by the same numeric cubic-extrema owner as exported paths.
    internal ChartRect Bounds => _bounds.Value;
    internal double CircleAreaRadiusFactor => _areaFactor.Value;

    private ChartMarkerPath Append(ChartPathCommand command) {
        if (_commands.Count >= MaximumCommands) throw new InvalidOperationException("A marker path supports at most 128 numeric commands.");
        var copy = new ChartPathCommand[_commands.Count + 1];
        for (var i = 0; i < _commands.Count; i++) copy[i] = _commands[i];
        copy[copy.Length - 1] = command;
        return new ChartMarkerPath(copy);
    }

    private static void Coordinate(double value, string parameter) {
        ChartGuards.Finite(value, parameter);
        if (value < -1 || value > 1) throw new ArgumentOutOfRangeException(parameter, value, "Marker path coordinates must be from minus one through one.");
    }
}
