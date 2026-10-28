using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

/// <summary>Immutable captured absolute move, line and cubic geometry in topology coordinates.</summary>
/// <remarks>Captures 2 through 4096 commands, at most 262144 source characters and finite coordinates
/// within one billion units. PathData preserves captured double precision. No live SVG is retained.</remarks>
public sealed class TopologyPathGeometry {
    internal const double MaximumCoordinate = 1000000000;
    private readonly SvgPathCommand[] _commands;
    /// <summary>Captures supported absolute M/L/C geometry. Every subpath must contain a segment.</summary>
    public TopologyPathGeometry(string pathData) : this(Parse(pathData)) { }
    internal TopologyPathGeometry(SvgPathData path) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (path.Commands.Count < 2 || path.Commands.Count > TopologyPathDecoration.MaximumCommands) throw new ArgumentException("Topology paths require 2 through 4096 commands.", nameof(path));
        _commands = path.Commands.Select(command => new SvgPathCommand(command.Command, command.Values)).ToArray();
        Capture();
    }
    /// <summary>Gets normalized SVG path text with lossless round-trip double precision.</summary>
    public string PathData => new SvgPathData(_commands).ToRoundTripMarkup();
    internal IReadOnlyList<SvgPathCommand> Commands => Array.AsReadOnly(_commands);
    internal ChartPath Capture() {
        var commands = new List<ChartPathCommand>(_commands.Length); var subpathHasSegment = false;
        foreach (var command in _commands) {
            if (command.Command is not ('M' or 'L' or 'C')) throw new ArgumentException("Topology paths support only absolute M, L and C commands.");
            if (commands.Count == 0 && command.Command != 'M') throw new ArgumentException("Topology paths must start with M.");
            if (command.Values.Count != (command.Command == 'C' ? 6 : 2)) throw new ArgumentException("Topology paths require exact M/L arity 2 or C arity 6.");
            foreach (var value in command.Values) {
                TopologyModelGuards.Finite(value, nameof(PathData));
                if (Math.Abs(value) > MaximumCoordinate) throw new ArgumentOutOfRangeException(nameof(PathData), "Path coordinates cannot exceed one billion units.");
            }
            if (command.Command == 'M') {
                if (commands.Count > 0 && !subpathHasSegment) throw new ArgumentException("Topology subpaths require a line or cubic segment.");
                subpathHasSegment = false;
            } else subpathHasSegment = true;
            var v = command.Values;
            commands.Add(command.Command == 'M' ? ChartPathCommand.MoveTo(v[0], v[1]) : command.Command == 'L' ? ChartPathCommand.LineTo(v[0], v[1])
                : ChartPathCommand.CubicTo(v[0], v[1], v[2], v[3], v[4], v[5]));
        }
        if (!subpathHasSegment) throw new ArgumentException("Topology subpaths require a line or cubic segment.");
        return new ChartPath(commands);
    }
    internal TopologyPathGeometry Transform(Func<ChartPoint, ChartPoint> point) {
        var path = new SvgPathData();
        foreach (var command in _commands) {
            var values = new double[command.Values.Count];
            for (var i = 0; i < values.Length; i += 2) { var p = point(new ChartPoint(command.Values[i], command.Values[i + 1])); values[i] = p.X; values[i + 1] = p.Y; }
            path.Add(new SvgPathCommand(command.Command, values));
        }
        return new TopologyPathGeometry(path);
    }
    internal void ValidateEdge() {
        if (_commands.Skip(1).Any(command => command.Command == 'M')) throw new ArgumentException("An authored edge path requires one continuous subpath.");
    }
    internal List<ChartPoint> RoutePoints() { ValidateEdge(); return Capture().Flatten(maximumPoints: 10000); }
    internal ChartPoint AdjacentToEndpoint(bool start) => Capture().AdjacentToEndpoint(start);
    internal static TopologyPathGeometry FromNativePath(ChartPath source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.Commands.Count < 2 || source.Commands.Count > TopologyPathDecoration.MaximumCommands) throw new ArgumentException("Topology paths require 2 through 4096 commands.", nameof(source));
        var path = new SvgPathData();
        foreach (var command in source.Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) path.Add(new SvgPathCommand('M', new[] { command.X, command.Y }));
            else if (command.Kind == ChartPathCommandKind.LineTo) path.Add(new SvgPathCommand('L', new[] { command.X, command.Y }));
            else if (command.Kind == ChartPathCommandKind.CubicTo) path.Add(new SvgPathCommand('C', new[] { command.Control1X, command.Control1Y, command.Control2X, command.Control2Y, command.X, command.Y }));
            else throw new ArgumentException("Topology paths support only absolute move, line and cubic commands.", nameof(source));
        }
        return new TopologyPathGeometry(path);
    }
    private static SvgPathData Parse(string value) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (value.Length > 262144) throw new ArgumentException("A topology path exceeds 262144 characters.", nameof(value));
        return SvgPathData.Parse(value);
    }
}

public sealed partial class TopologyEdge {
    /// <summary>Gets or sets an immutable complete authored route, including its endpoints.</summary>
    /// <remarks>Null retains automatic routing. A path uses Manual layout, one continuous subpath,
    /// no waypoints, route-lane or port overrides. Routing remains a fallback for null geometry.
    /// The path participates in fitting, markers, route observations and version-2 interchange.</remarks>
    public TopologyPathGeometry? PathGeometry { get; set; }
}
