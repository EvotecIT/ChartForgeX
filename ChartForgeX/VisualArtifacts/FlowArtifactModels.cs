using System;
using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Defines deterministic layout modes for process and workflow artifacts.
/// </summary>
public enum FlowArtifactLayoutMode {
    /// <summary>Place steps in ordered layers.</summary>
    Layered,
    /// <summary>Place lanes and steps in dense grouped panels.</summary>
    Dense,
    /// <summary>Place connected steps with a deterministic force-directed layout.</summary>
    Force
}

/// <summary>
/// Defines the preferred direction for ordered flow layouts.
/// </summary>
public enum FlowArtifactDirection {
    /// <summary>Place steps from left to right.</summary>
    LeftToRight,
    /// <summary>Place steps from top to bottom.</summary>
    TopToBottom,
    /// <summary>Place steps from right to left.</summary>
    RightToLeft,
    /// <summary>Place steps from bottom to top.</summary>
    BottomToTop
}

/// <summary>
/// Defines semantic step kinds for workflow and process artifacts.
/// </summary>
public enum FlowArtifactStepKind {
    /// <summary>A generic flow step.</summary>
    Step,
    /// <summary>A process or activity step.</summary>
    Process,
    /// <summary>A decision or branching point.</summary>
    Decision,
    /// <summary>A flow start point.</summary>
    Start,
    /// <summary>A flow end point.</summary>
    End,
    /// <summary>An input step.</summary>
    Input,
    /// <summary>An output step.</summary>
    Output,
    /// <summary>A data store or data-oriented step.</summary>
    Data,
    /// <summary>An external system or actor.</summary>
    External,
    /// <summary>A document or artifact step.</summary>
    Document,
    /// <summary>A manual or human-owned step.</summary>
    Manual,
    /// <summary>A wait or delay step.</summary>
    Delay,
    /// <summary>An event step.</summary>
    Event
}

/// <summary>
/// Defines semantic connector kinds for workflow and process artifacts.
/// </summary>
public enum FlowArtifactConnectorKind {
    /// <summary>A normal flow connector.</summary>
    Flow,
    /// <summary>A dependency connector.</summary>
    Dependency,
    /// <summary>A data movement connector.</summary>
    Data,
    /// <summary>A rejection or negative branch connector.</summary>
    Rejection,
    /// <summary>A retry connector.</summary>
    Retry,
    /// <summary>An error-handling connector.</summary>
    Error,
    /// <summary>An asynchronous connector.</summary>
    Async
}

/// <summary>
/// Represents a product-neutral workflow or process flow artifact.
/// </summary>
public sealed class FlowArtifact {
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private readonly List<FlowArtifactLane> _lanes = new();
    private readonly List<FlowArtifactStep> _steps = new();
    private readonly List<FlowArtifactConnector> _connectors = new();
    private double _width = 1200;
    private double _height = 700;
    private double _padding = 24;
    private VisualArtifactExportFormat _exportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json;

    /// <summary>Gets or sets a stable flow identifier.</summary>
    public string Id { get => _id; set => _id = RequireToken(value, nameof(value)); }

    /// <summary>Gets or sets the flow title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the flow subtitle.</summary>
    public string Subtitle { get => _subtitle; set => _subtitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the deterministic layout mode.</summary>
    public FlowArtifactLayoutMode LayoutMode { get; set; } = FlowArtifactLayoutMode.Layered;

    /// <summary>Gets or sets the deterministic layout direction.</summary>
    public FlowArtifactDirection Direction { get; set; } = FlowArtifactDirection.LeftToRight;

    /// <summary>Gets or sets the static preview width.</summary>
    public double Width { get => _width; set { ValidatePositive(value, nameof(value)); _width = value; } }

    /// <summary>Gets or sets the static preview height.</summary>
    public double Height { get => _height; set { ValidatePositive(value, nameof(value)); _height = value; } }

    /// <summary>Gets or sets the static preview padding.</summary>
    public double Padding { get => _padding; set { ValidateNonNegative(value, nameof(value)); _padding = value; } }

    /// <summary>Gets declared flow lanes.</summary>
    public IReadOnlyList<FlowArtifactLane> Lanes => _lanes;

    /// <summary>Gets declared flow steps.</summary>
    public IReadOnlyList<FlowArtifactStep> Steps => _steps;

    /// <summary>Gets declared flow connectors.</summary>
    public IReadOnlyList<FlowArtifactConnector> Connectors => _connectors;

    /// <summary>Gets or sets static export formats declared by this artifact.</summary>
    public VisualArtifactExportFormat ExportFormats {
        get => _exportFormats;
        set {
            VisualArtifactGuards.ExportFormatsDefined(value, nameof(value));
            _exportFormats = value;
        }
    }

    /// <summary>Gets metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

    /// <summary>Creates a new flow artifact.</summary>
    public static FlowArtifact Create(string id) => new() { Id = id };

    /// <summary>Sets the flow title.</summary>
    public FlowArtifact WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>Sets the flow subtitle.</summary>
    public FlowArtifact WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return this; }

    /// <summary>Sets the static preview size.</summary>
    public FlowArtifact WithSize(double width, double height, double padding = 24) {
        ValidatePositive(width, nameof(width));
        ValidatePositive(height, nameof(height));
        ValidateNonNegative(padding, nameof(padding));
        Width = width;
        Height = height;
        Padding = padding;
        return this;
    }

    /// <summary>Adds a flow lane.</summary>
    public FlowArtifact AddLane(string id, string label, VisualStatus status = VisualStatus.None, string? color = null) {
        if (ContainsLane(id)) throw new ArgumentException("Flow lane ids must be unique.", nameof(id));
        _lanes.Add(new FlowArtifactLane(id, label) { Status = status, Color = color });
        return this;
    }

    /// <summary>Adds a flow step.</summary>
    public FlowArtifact AddStep(string id, string label, FlowArtifactStepKind kind = FlowArtifactStepKind.Step, string? laneId = null, VisualStatus status = VisualStatus.None) {
        if (ContainsStep(id)) throw new ArgumentException("Flow step ids must be unique.", nameof(id));
        _steps.Add(new FlowArtifactStep(id, label, kind) { LaneId = laneId, Status = status });
        return this;
    }

    /// <summary>Configures one existing step.</summary>
    public FlowArtifact WithStep(string id, Action<FlowArtifactStep> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(FindStep(id));
        return this;
    }

    /// <summary>Adds a flow connector.</summary>
    public FlowArtifact AddConnector(string sourceId, string targetId, string label = "", FlowArtifactConnectorKind kind = FlowArtifactConnectorKind.Flow, VisualLinkDirection direction = VisualLinkDirection.Forward, VisualStatus status = VisualStatus.None, string? color = null) {
        if (!ContainsStep(sourceId)) throw new ArgumentException("Flow connector source step does not exist: " + sourceId + ".", nameof(sourceId));
        if (!ContainsStep(targetId)) throw new ArgumentException("Flow connector target step does not exist: " + targetId + ".", nameof(targetId));
        var id = sourceId + "-" + targetId + "-" + (_connectors.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        _connectors.Add(new FlowArtifactConnector(id, sourceId, targetId) { Label = label ?? string.Empty, Kind = kind, Direction = direction, Status = status, Color = color });
        return this;
    }

    /// <summary>Configures one existing connector.</summary>
    public FlowArtifact WithConnector(int connectorIndex, Action<FlowArtifactConnector> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        if (connectorIndex < 0 || connectorIndex >= _connectors.Count) throw new ArgumentOutOfRangeException(nameof(connectorIndex), connectorIndex, "Connector index must reference an existing flow connector.");
        configure(_connectors[connectorIndex]);
        return this;
    }

    /// <summary>Returns true when the flow declares the requested static export format.</summary>
    public bool SupportsExport(VisualArtifactExportFormat format) => format != VisualArtifactExportFormat.None && (ExportFormats & format) == format;

    private bool ContainsLane(string id) {
        RequireToken(id, nameof(id));
        for (var i = 0; i < _lanes.Count; i++) if (string.Equals(_lanes[i].Id, id, StringComparison.Ordinal)) return true;
        return false;
    }

    private bool ContainsStep(string id) {
        RequireToken(id, nameof(id));
        for (var i = 0; i < _steps.Count; i++) if (string.Equals(_steps[i].Id, id, StringComparison.Ordinal)) return true;
        return false;
    }

    private FlowArtifactStep FindStep(string id) {
        RequireToken(id, nameof(id));
        for (var i = 0; i < _steps.Count; i++) if (string.Equals(_steps[i].Id, id, StringComparison.Ordinal)) return _steps[i];
        throw new ArgumentException("Flow step does not exist: " + id + ".", nameof(id));
    }

    private static void ValidatePositive(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
    }

    private static void ValidateNonNegative(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and zero or greater.");
    }

    internal static string RequireToken(string value, string parameterName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Flow artifact identifiers must not be empty.", parameterName);
        return value;
    }
}

/// <summary>
/// Describes one lane in a flow artifact.
/// </summary>
public sealed class FlowArtifactLane {
    private string _id;
    private string _label;

    /// <summary>Initializes a flow lane.</summary>
    public FlowArtifactLane(string id, string label) {
        _id = FlowArtifact.RequireToken(id, nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets or sets the lane id.</summary>
    public string Id { get => _id; set => _id = FlowArtifact.RequireToken(value, nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the lane status.</summary>
    public VisualStatus Status { get; set; }

    /// <summary>Gets or sets an optional accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets lane metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Describes one step in a flow artifact.
/// </summary>
public sealed class FlowArtifactStep {
    private string _id;
    private string _label;

    /// <summary>Initializes a flow step.</summary>
    public FlowArtifactStep(string id, string label, FlowArtifactStepKind kind = FlowArtifactStepKind.Step) {
        _id = FlowArtifact.RequireToken(id, nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Kind = kind;
    }

    /// <summary>Gets or sets the step id.</summary>
    public string Id { get => _id; set => _id = FlowArtifact.RequireToken(value, nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the semantic step kind.</summary>
    public FlowArtifactStepKind Kind { get; set; }

    /// <summary>Gets or sets the optional lane id.</summary>
    public string? LaneId { get; set; }

    /// <summary>Gets or sets the step status.</summary>
    public VisualStatus Status { get; set; }

    /// <summary>Gets or sets an optional subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets an optional icon id.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets an optional symbol.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets an optional accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets an optional badge.</summary>
    public string? Badge { get; set; }

    /// <summary>Gets or sets the static preview width.</summary>
    public double Width { get; set; } = 130;

    /// <summary>Gets or sets the static preview height.</summary>
    public double Height { get; set; } = 68;

    /// <summary>Gets step metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Describes one connector in a flow artifact.
/// </summary>
public sealed class FlowArtifactConnector {
    private string _id;
    private string _sourceId;
    private string _targetId;
    private string _label = string.Empty;

    /// <summary>Initializes a flow connector.</summary>
    public FlowArtifactConnector(string id, string sourceId, string targetId) {
        _id = FlowArtifact.RequireToken(id, nameof(id));
        _sourceId = FlowArtifact.RequireToken(sourceId, nameof(sourceId));
        _targetId = FlowArtifact.RequireToken(targetId, nameof(targetId));
    }

    /// <summary>Gets or sets the connector id.</summary>
    public string Id { get => _id; set => _id = FlowArtifact.RequireToken(value, nameof(value)); }

    /// <summary>Gets or sets the source step id.</summary>
    public string SourceId { get => _sourceId; set => _sourceId = FlowArtifact.RequireToken(value, nameof(value)); }

    /// <summary>Gets or sets the target step id.</summary>
    public string TargetId { get => _targetId; set => _targetId = FlowArtifact.RequireToken(value, nameof(value)); }

    /// <summary>Gets or sets the connector label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the semantic connector kind.</summary>
    public FlowArtifactConnectorKind Kind { get; set; }

    /// <summary>Gets or sets connector direction marker behavior.</summary>
    public VisualLinkDirection Direction { get; set; } = VisualLinkDirection.Forward;

    /// <summary>Gets or sets connector status.</summary>
    public VisualStatus Status { get; set; }

    /// <summary>Gets or sets an optional accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets connector metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}
