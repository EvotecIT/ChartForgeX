using System;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>
/// A detached layout snapshot shared by SVG, PNG, diagnostics, and native-document interchange.
/// Changes to the source chart or options do not change this snapshot. External artwork files
/// and system fonts remain host resources and must remain available when rendering.
/// </summary>
public sealed class PreparedTopology {
    private readonly TopologyChart _chart;
    private readonly TopologyRenderOptions _options;
    private readonly double _requestedWidth;
    private readonly double _requestedHeight;

    internal PreparedTopology(TopologyChart chart, TopologyRenderOptions options, double requestedWidth, double requestedHeight) {
        _chart = chart;
        _options = options;
        _requestedWidth = requestedWidth;
        _requestedHeight = requestedHeight;
    }

    /// <summary>Gets the prepared geometry width in pixels, before optional output fitting.</summary>
    public double Width => _chart.Viewport.Width;
    /// <summary>Gets the prepared geometry height in pixels, before optional output fitting.</summary>
    public double Height => _chart.Viewport.Height;
    /// <summary>Gets the number of nodes retained by the selected view.</summary>
    public int NodeCount => _chart.Nodes.Count;
    /// <summary>Gets the number of retained relationships.</summary>
    public int EdgeCount => _chart.Edges.Count;
    /// <summary>Gets the source title without requiring interchange serialization.</summary>
    public string? Title => _chart.Title;

    /// <summary>Renders the prepared geometry without running layout again.</summary>
    public string ToSvg() => new TopologySvgRenderer().RenderPrepared(_chart, _options, _requestedWidth, _requestedHeight);

    /// <summary>Renders the same prepared geometry through the dependency-free raster renderer.</summary>
    public byte[] ToPng() => PngWriter.WriteRgba(new TopologyPngRenderer().RenderPreparedImage(
        _chart, _options, (int)Math.Ceiling(_requestedWidth), (int)Math.Ceiling(_requestedHeight)));

    /// <summary>Returns a detached semantic envelope with the positions used by the renderers.</summary>
    public VisualArtifactInterchangeEnvelope ToInterchangeEnvelope() => VisualArtifactInterchangeMapping.FromPreparedTopology(_chart, _options);

    /// <summary>Measures the prepared geometry without running layout again. The returned report is detached.</summary>
    public TopologyLayoutDiagnosticReport Analyze() => TopologyLayoutDiagnostics.AnalyzePrepared(_chart, _options);

    /// <summary>Evaluates collisions, viewport expansion, and readability at a target display size.</summary>
    public TopologyReadabilityReport AssessReadability(double targetWidth, double targetHeight, double minimumScale = 0.65) =>
        TopologyReadabilityReport.Create(Analyze(), targetWidth, targetHeight, minimumScale);
}

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Validates and prepares a detached layout once for multiple exports and diagnostics.
    /// Use <see cref="PreparedTopology.AssessReadability"/> before fitting a dense layout into a small output.
    /// </summary>
    public static PreparedTopology Prepare(this TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var effective = (options ?? new TopologyRenderOptions()).CloneForRendering();
        var validator = new TopologyChartValidator();
        var sourceValidation = validator.ValidateScenarioReferences(chart);
        if (!sourceValidation.IsValid) throw new TopologyValidationException(sourceValidation);
        var prepared = TopologyLayoutEngine.Prepare(chart, effective.View, effective);
        // A filtered view may omit its original group while retaining an explicitly selected node.
        var groupIds = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var group in prepared.Groups) groupIds.Add(group.Id);
        if (effective.View != null) foreach (var node in prepared.Nodes) {
            if (node.GroupId != null && !groupIds.Contains(node.GroupId)) node.GroupId = null;
        }
        var validation = validator.Validate(prepared, validateScenarioReferences: false, effective);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        return new PreparedTopology(prepared, effective, chart.Viewport.Width, chart.Viewport.Height);
    }
}
