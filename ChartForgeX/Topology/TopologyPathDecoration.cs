using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

/// <summary>A detached path painted behind topology relationships and nodes without introducing graph facts.</summary>
/// <remarks>Coordinates use the chart's authored coordinate domain and participate in native fitting and clipping.
/// Only absolute M, L and C commands are accepted; use <see cref="Close"/> to close every subpath.
/// Paths are bounded to 4096 commands, 262144 source characters and coordinates within one billion units.
/// Native SVG, HTML, PNG and version-2 portable interchange share the same immutable numeric capture.</remarks>
public sealed class TopologyPathDecoration {
    internal const int MaximumCommands = 4096;
    internal const int MaximumPaths = 1024;
    internal const int MaximumTotalCommands = 65536;
    private readonly TopologyPathGeometry _geometry;
    private readonly double[]? _dash;

    /// <summary>Captures a path and its optional native fill, stroke and dash pattern.</summary>
    public TopologyPathDecoration(string id, string pathData, ChartColor? fill = null, ChartColor? stroke = null,
        double strokeWidth = 1, bool close = false, IEnumerable<double>? dashPattern = null)
        : this(id, new TopologyPathGeometry(pathData), fill, stroke, strokeWidth, close, dashPattern) { }

    /// <summary>Captures existing typed SVG path commands without reducing their numeric precision.</summary>
    internal TopologyPathDecoration(string id, SvgPathData path, ChartColor? fill = null, ChartColor? stroke = null,
        double strokeWidth = 1, bool close = false, IEnumerable<double>? dashPattern = null)
        : this(id, new TopologyPathGeometry(path), fill, stroke, strokeWidth, close, dashPattern) { }

    private TopologyPathDecoration(string id, TopologyPathGeometry geometry, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, bool close, IEnumerable<double>? dashPattern) {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 256) throw new ArgumentException("Path decoration ids require 1 through 256 characters.", nameof(id));
        _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        TopologyModelGuards.NonNegativeFinite(strokeWidth, nameof(strokeWidth));
        if (strokeWidth > 4096) throw new ArgumentOutOfRangeException(nameof(strokeWidth));
        _dash = dashPattern?.Take(33).ToArray();
        if (_dash != null) {
            if (_dash.Length == 0 || _dash.Length > 32) throw new ArgumentException("A dash pattern requires 1 through 32 lengths.", nameof(dashPattern));
            foreach (var length in _dash) {
                TopologyModelGuards.PositiveFinite(length, nameof(dashPattern));
                if (length > 4096) throw new ArgumentOutOfRangeException(nameof(dashPattern));
            }
        }
        Id = id; Fill = fill; Stroke = stroke; StrokeWidth = strokeWidth; Close = close;
        Capture();
    }

    /// <summary>Gets the stable decoration identity, independent of graph nodes and edges.</summary>
    public string Id { get; }
    /// <summary>Gets normalized SVG path text with round-trip double precision. Legacy SVG markup formatting is independent.</summary>
    public string PathData => _geometry.PathData;
    internal IReadOnlyList<SvgPathCommand> Commands => _geometry.Commands;
    /// <summary>Gets the optional fill. Null paints no fill.</summary>
    public ChartColor? Fill { get; }
    /// <summary>Gets the optional stroke. Null paints no stroke.</summary>
    public ChartColor? Stroke { get; }
    /// <summary>Gets the logical stroke width, scaled with the topology.</summary>
    public double StrokeWidth { get; }
    /// <summary>Gets whether each captured subpath is closed.</summary>
    public bool Close { get; }
    /// <summary>Gets a detached immutable dash pattern, or null for a solid stroke.</summary>
    public IReadOnlyList<double>? DashPattern => _dash == null ? null : Array.AsReadOnly(_dash);

    internal ChartPath Capture() => _geometry.Capture();

    /// <summary>Uses the compiler's prepared point transform for every endpoint and cubic control.</summary>
    internal TopologyPathDecoration Transform(Func<ChartPoint, ChartPoint> point, double scale) {
        return new TopologyPathDecoration(Id, _geometry.Transform(point), Fill, Stroke, StrokeWidth * scale, Close, _dash?.Select(value => value * scale));
    }

}

public sealed partial class TopologyChart {
    /// <summary>Gets optional captured paths painted behind graph entities in the authored coordinate domain.</summary>
    /// <remarks>The empty collection preserves ordinary topology output. Preparation accepts at most 1024 paths
    /// and 65536 total commands. Automatic graph layout does not reposition these authored paths.</remarks>
    public List<TopologyPathDecoration> PathDecorations { get; } = new();
}
