using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Represents a renderer-independent topology diagram.
/// </summary>
public sealed class TopologyChart {
    private TopologyLayoutMode _layoutMode = TopologyLayoutMode.Manual;
    private TopologyLayoutDirection _layoutDirection = TopologyLayoutDirection.TopToBottom;
    private TopologyViewport _viewport = new();

    /// <summary>
    /// Creates a new topology chart.
    /// </summary>
    /// <returns>A new topology chart.</returns>
    public static TopologyChart Create() => new();

    /// <summary>Gets or sets a stable chart identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the chart title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the chart subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the deterministic layout mode.</summary>
    public TopologyLayoutMode LayoutMode {
        get => _layoutMode;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _layoutMode = value;
        }
    }

    /// <summary>Gets or sets the deterministic layout flow direction.</summary>
    public TopologyLayoutDirection LayoutDirection {
        get => _layoutDirection;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _layoutDirection = value;
        }
    }

    /// <summary>Gets the viewport used by renderers.</summary>
    public TopologyViewport Viewport {
        get => _viewport;
        set => _viewport = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the longitude/latitude window used by geographic topology layouts.</summary>
    public ChartMapViewport MapViewport { get; set; } = ChartMapViewport.World();

    /// <summary>Gets the topology groups.</summary>
    public List<TopologyGroup> Groups { get; } = new();

    /// <summary>Gets the topology nodes.</summary>
    public List<TopologyNode> Nodes { get; } = new();

    /// <summary>Gets the topology edges.</summary>
    public List<TopologyEdge> Edges { get; } = new();

    /// <summary>Gets or sets an optional legend.</summary>
    public TopologyLegend? Legend { get; set; }

    /// <summary>Gets or sets an optional topology theme.</summary>
    public TopologyTheme? Theme { get; set; }
}

/// <summary>
/// Defines the topology rendering viewport.
/// </summary>
public sealed class TopologyViewport {
    private double _width = 1200;
    private double _height = 700;
    private double _padding = 24;

    /// <summary>Gets or sets the viewport width.</summary>
    public double Width {
        get => _width;
        set {
            TopologyModelGuards.PositiveFinite(value, nameof(value));
            _width = value;
        }
    }

    /// <summary>Gets or sets the viewport height.</summary>
    public double Height {
        get => _height;
        set {
            TopologyModelGuards.PositiveFinite(value, nameof(value));
            _height = value;
        }
    }

    /// <summary>Gets or sets the viewport padding.</summary>
    public double Padding {
        get => _padding;
        set {
            TopologyModelGuards.NonNegativeFinite(value, nameof(value));
            _padding = value;
        }
    }
}

/// <summary>
/// Represents a logical topology region or cluster.
/// </summary>
public sealed class TopologyGroup {
    private string _id = string.Empty;
    private string _label = string.Empty;
    private TopologyHealthStatus _status = TopologyHealthStatus.Unknown;
    private double _x;
    private double _y;
    private double? _longitude;
    private double? _latitude;
    private double _width;
    private double _height;
    private TopologyGroupLayoutPolicy _layoutPolicy = TopologyGroupLayoutPolicy.Auto;
    private TopologyGroupLayoutPolicy _appliedLayoutPolicy = TopologyGroupLayoutPolicy.Auto;

    /// <summary>Gets or sets the stable group identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the group label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional group subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the group status.</summary>
    public TopologyHealthStatus Status {
        get => _status;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets the group x-coordinate.</summary>
    public double X {
        get => _x;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            _x = value;
        }
    }

    /// <summary>Gets or sets the group y-coordinate.</summary>
    public double Y {
        get => _y;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            _y = value;
        }
    }

    /// <summary>Gets or sets the optional group longitude in degrees.</summary>
    public double? Longitude {
        get => _longitude;
        set {
            if (value.HasValue) TopologyModelGuards.Longitude(value.Value, nameof(value));
            _longitude = value;
        }
    }

    /// <summary>Gets or sets the optional group latitude in degrees.</summary>
    public double? Latitude {
        get => _latitude;
        set {
            if (value.HasValue) TopologyModelGuards.Latitude(value.Value, nameof(value));
            _latitude = value;
        }
    }

    /// <summary>Gets or sets the group width.</summary>
    public double Width {
        get => _width;
        set {
            TopologyModelGuards.NonNegativeFinite(value, nameof(value));
            _width = value;
        }
    }

    /// <summary>Gets or sets the group height.</summary>
    public double Height {
        get => _height;
        set {
            TopologyModelGuards.NonNegativeFinite(value, nameof(value));
            _height = value;
        }
    }

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets or sets optional caller-provided CSS class tokens for SVG/HTML hosts.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets or sets an optional short group symbol used by renderers.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets an optional reusable icon id such as <c>microsoft-ad:site</c>.</summary>
    public string? IconId { get; set; }

    /// <summary>Gets or sets an optional group accent color. When set, this colors the group shell independently from health status.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the preferred node arrangement policy for dense grouped layouts.</summary>
    public TopologyGroupLayoutPolicy LayoutPolicy {
        get => _layoutPolicy;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _layoutPolicy = value;
        }
    }

    /// <summary>Gets or sets the resolved node arrangement policy applied by layout preparation.</summary>
    public TopologyGroupLayoutPolicy AppliedLayoutPolicy {
        get => _appliedLayoutPolicy;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _appliedLayoutPolicy = value;
        }
    }

    internal bool HasPositionOverride { get; set; }

    /// <summary>Gets arbitrary metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Represents a topology node.
/// </summary>
public sealed class TopologyNode {
    private string _id = string.Empty;
    private string _label = string.Empty;
    private TopologyNodeKind _kind = TopologyNodeKind.Generic;
    private TopologyNodeDisplayMode? _displayMode;
    private TopologyHealthStatus _status = TopologyHealthStatus.Unknown;
    private double _x;
    private double _y;
    private double? _longitude;
    private double? _latitude;
    private double _width = 120;
    private double _height = 64;

    /// <summary>Gets or sets the stable node identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the node label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional node subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the node kind.</summary>
    public TopologyNodeKind Kind {
        get => _kind;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _kind = value;
        }
    }

    /// <summary>Gets or sets an optional short visual symbol used inside the node icon.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets an optional reusable icon id such as <c>network:switch</c> or <c>microsoft-ad:domain-controller</c>.</summary>
    public string? IconId { get; set; }

    /// <summary>Gets or sets an optional node-specific display mode. When null, render options decide.</summary>
    public TopologyNodeDisplayMode? DisplayMode {
        get => _displayMode;
        set {
            if (value.HasValue) TopologyModelGuards.EnumDefined(value.Value, nameof(value));
            _displayMode = value;
        }
    }

    /// <summary>Gets or sets optional short badge text such as a count, role, or collapsed-item marker.</summary>
    public string? Badge { get; set; }

    /// <summary>Gets or sets the node status.</summary>
    public TopologyHealthStatus Status {
        get => _status;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets the optional parent group id.</summary>
    public string? GroupId { get; set; }

    /// <summary>Gets or sets the node x-coordinate.</summary>
    public double X {
        get => _x;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            _x = value;
        }
    }

    /// <summary>Gets or sets the node y-coordinate.</summary>
    public double Y {
        get => _y;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            _y = value;
        }
    }

    /// <summary>Gets or sets the optional node longitude in degrees.</summary>
    public double? Longitude {
        get => _longitude;
        set {
            if (value.HasValue) TopologyModelGuards.Longitude(value.Value, nameof(value));
            _longitude = value;
        }
    }

    /// <summary>Gets or sets the optional node latitude in degrees.</summary>
    public double? Latitude {
        get => _latitude;
        set {
            if (value.HasValue) TopologyModelGuards.Latitude(value.Value, nameof(value));
            _latitude = value;
        }
    }

    /// <summary>Gets or sets the node width.</summary>
    public double Width {
        get => _width;
        set {
            TopologyModelGuards.PositiveFinite(value, nameof(value));
            _width = value;
        }
    }

    /// <summary>Gets or sets the node height.</summary>
    public double Height {
        get => _height;
        set {
            TopologyModelGuards.PositiveFinite(value, nameof(value));
            _height = value;
        }
    }

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets or sets optional caller-provided CSS class tokens for SVG/HTML hosts.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets or sets an optional node accent color. When set, this colors the node shell independently from health status.</summary>
    public string? Color { get; set; }

    /// <summary>Gets node metrics for host adapters.</summary>
    public Dictionary<string, string> Metrics { get; } = new();

    /// <summary>Gets arbitrary node metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Represents a topology edge between two nodes.
/// </summary>
public sealed class TopologyEdge {
    private string _id = string.Empty;
    private string _sourceNodeId = string.Empty;
    private string _targetNodeId = string.Empty;
    private TopologyEdgeKind _kind = TopologyEdgeKind.Generic;
    private TopologyHealthStatus _status = TopologyHealthStatus.Unknown;
    private TopologyDirection _direction = TopologyDirection.None;
    private TopologyEdgeRouting _routing = TopologyEdgeRouting.Orthogonal;
    private TopologyEdgeLineStyle _lineStyle = TopologyEdgeLineStyle.Auto;
    private TopologyEdgeEmphasis _emphasis = TopologyEdgeEmphasis.Normal;
    private TopologyEdgePort _sourcePort = TopologyEdgePort.Auto;
    private TopologyEdgePort _targetPort = TopologyEdgePort.Auto;
    private double _routeLane;
    private double _labelOffsetX;
    private double _labelOffsetY;
    private TopologyEdgeLayoutInference _layoutInference = TopologyEdgeLayoutInference.None;

    /// <summary>Gets or sets the stable edge identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the source node id.</summary>
    public string SourceNodeId { get => _sourceNodeId; set => _sourceNodeId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the target node id.</summary>
    public string TargetNodeId { get => _targetNodeId; set => _targetNodeId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the edge kind.</summary>
    public TopologyEdgeKind Kind {
        get => _kind;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _kind = value;
        }
    }

    /// <summary>Gets or sets the edge status.</summary>
    public TopologyHealthStatus Status {
        get => _status;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets the edge direction.</summary>
    public TopologyDirection Direction {
        get => _direction;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _direction = value;
        }
    }

    /// <summary>Gets or sets the edge routing mode.</summary>
    public TopologyEdgeRouting Routing {
        get => _routing;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _routing = value;
        }
    }

    /// <summary>Gets or sets explicit edge line styling. Auto derives styling from health status.</summary>
    public TopologyEdgeLineStyle LineStyle {
        get => _lineStyle;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _lineStyle = value;
        }
    }

    /// <summary>Gets or sets edge rendering emphasis independent from health status and line style.</summary>
    public TopologyEdgeEmphasis Emphasis {
        get => _emphasis;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _emphasis = value;
        }
    }

    /// <summary>Gets or sets the preferred source attachment side.</summary>
    public TopologyEdgePort SourcePort {
        get => _sourcePort;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _sourcePort = value;
        }
    }

    /// <summary>Gets or sets the preferred target attachment side.</summary>
    public TopologyEdgePort TargetPort {
        get => _targetPort;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _targetPort = value;
        }
    }

    /// <summary>Gets or sets the deterministic orthogonal route lane offset in pixels.</summary>
    public double RouteLane {
        get => _routeLane;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            _routeLane = value;
        }
    }

    internal bool HasRouteLaneOverride { get; set; }

    /// <summary>Gets or sets the horizontal edge-label placement adjustment in pixels.</summary>
    public double LabelOffsetX {
        get => _labelOffsetX;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            _labelOffsetX = value;
        }
    }

    /// <summary>Gets or sets the vertical edge-label placement adjustment in pixels.</summary>
    public double LabelOffsetY {
        get => _labelOffsetY;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            _labelOffsetY = value;
        }
    }

    /// <summary>Gets or sets which edge layout values were inferred during layout preparation.</summary>
    public TopologyEdgeLayoutInference LayoutInference {
        get => _layoutInference;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _layoutInference = value;
        }
    }

    /// <summary>Gets explicit route bend points used between the source and target nodes.</summary>
    public List<ChartPoint> Waypoints { get; } = new();

    /// <summary>Gets or sets the primary edge label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the secondary edge label.</summary>
    public string? SecondaryLabel { get; set; }

    /// <summary>Gets or sets the tertiary edge label.</summary>
    public string? TertiaryLabel { get; set; }

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets or sets optional caller-provided CSS class tokens for SVG/HTML hosts.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets or sets whether the edge should render as a quiet structural relationship instead of a status route.</summary>
    public bool IsMuted { get; set; }

    /// <summary>Gets edge metrics for host adapters.</summary>
    public Dictionary<string, string> Metrics { get; } = new();

    /// <summary>Gets arbitrary edge metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

internal static class TopologyModelGuards {
    public static void EnumDefined<TEnum>(TEnum value, string parameterName) where TEnum : struct {
        if (typeof(TEnum) == typeof(TopologyEdgeLayoutInference)) {
            var mask = TopologyEdgeLayoutInference.SourcePort | TopologyEdgeLayoutInference.TargetPort | TopologyEdgeLayoutInference.RouteLane;
            var typed = (TopologyEdgeLayoutInference)(object)value;
            if ((typed & ~mask) == 0) return;
        }

        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown " + typeof(TEnum).Name + " value.");
    }

    public static void Finite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }

    public static void PositiveFinite(double value, string parameterName) {
        Finite(value, parameterName);
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
    }

    public static void NonNegativeFinite(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
    }

    public static void Longitude(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < -180 || value > 180) throw new ArgumentOutOfRangeException(parameterName, value, "Longitude must be between -180 and 180 degrees.");
    }

    public static void Latitude(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < -90 || value > 90) throw new ArgumentOutOfRangeException(parameterName, value, "Latitude must be between -90 and 90 degrees.");
    }
}
