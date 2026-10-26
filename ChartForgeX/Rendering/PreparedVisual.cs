using System;
using System.Collections.Generic;
using ChartForgeX.Accessibility;
using ChartForgeX.Diagnostics;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

/// <summary>An immutable preparation observation with a stable machine-readable code.</summary>
public sealed class VisualDiagnostic {
    /// <summary>Creates a diagnostic.</summary>
    public VisualDiagnostic(string code, string message, VisualDiagnosticSeverity severity = VisualDiagnosticSeverity.Warning) {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("A diagnostic code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("A diagnostic message is required.", nameof(message));
        if (!Enum.IsDefined(typeof(VisualDiagnosticSeverity), severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        Code = code; Message = message; Severity = severity;
    }
    /// <summary>Gets the stable code.</summary>
    public string Code { get; }
    /// <summary>Gets the human-readable message.</summary>
    public string Message { get; }
    /// <summary>Gets the severity.</summary>
    public VisualDiagnosticSeverity Severity { get; }
}

/// <summary>A descriptive semantic extent; bounds are not an exact hit-test shape.</summary>
public sealed class VisualSemanticRegion {
    /// <summary>Creates an immutable semantic region.</summary>
    public VisualSemanticRegion(string id, string role, ChartRect bounds, string? label = null) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A region ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("A region role is required.", nameof(role));
        Id = id; Role = role; Bounds = bounds; Label = label;
    }
    /// <summary>Gets the stable source identity.</summary>
    public string Id { get; }
    /// <summary>Gets the semantic role.</summary>
    /// <remarks>A prepared Cartesian chart with usable layout exposes one <c>cartesian-plot</c> region:
    /// the final mapping rectangle in logical SVG units, independent of mark clipping. It describes
    /// layout for host guides, not a data domain or exact hit shape. Composed panels retain this role
    /// with translated bounds and panel-prefixed IDs; empty and non-Cartesian charts omit it.</remarks>
    public string Role { get; }
    /// <summary>Gets the descriptive bounds in logical units.</summary>
    public ChartRect Bounds { get; }
    /// <summary>Gets the text alternative.</summary>
    public string? Label { get; }
}

/// <summary>Bounded raster export configuration; logical layout remains unchanged.</summary>
public sealed class VisualRenderOptions {
    /// <summary>Creates raster options. Pixel budget includes supersampled working pixels.</summary>
    public VisualRenderOptions(int scale = 1, int supersampling = 2, long pixelBudget = 64000000, ChartForgeX.Typography.TextHinting? textHinting = null) {
        if (scale < 1 || scale > 16) throw new ArgumentOutOfRangeException(nameof(scale));
        if (supersampling < 1 || supersampling > 8) throw new ArgumentOutOfRangeException(nameof(supersampling));
        if (pixelBudget < 1 || pixelBudget > 128000000) throw new ArgumentOutOfRangeException(nameof(pixelBudget));
        if (textHinting.HasValue && !Enum.IsDefined(typeof(ChartForgeX.Typography.TextHinting), textHinting.Value)) throw new ArgumentOutOfRangeException(nameof(textHinting));
        Scale = scale; Supersampling = supersampling; PixelBudget = pixelBudget; TextHinting = textHinting;
    }
    /// <summary>Gets the output pixels per logical unit.</summary>
    public int Scale { get; }
    /// <summary>Gets the working coverage multiplier.</summary>
    public int Supersampling { get; }
    /// <summary>Gets the maximum working pixel count.</summary>
    public long PixelBudget { get; }
    /// <summary>Gets an optional raster hinting override. Null retains each prepared text style's choice.</summary>
    public ChartForgeX.Typography.TextHinting? TextHinting { get; }
}

/// <summary>A detached immutable scene. SVG and native raster export consume the same prepared decisions.</summary>
public sealed partial class PreparedVisual {
    private readonly VisualScene _scene;
    private readonly VisualAccessibility _accessibility;
    private readonly Lazy<string> _svgIdPrefix;
    private readonly Lazy<string>? _semanticInterchange;
    private readonly VisualArtifactInterchangeEnvelope? _semanticSnapshot;
    private readonly VisualSvgOptions? _defaultSvgOptions;
    private readonly bool _capturePngText;
    internal VisualSceneTextCapture? NativeTextCapture { get; private set; }
    internal PreparedVisual(VisualScene scene, VisualAccessibility? accessibility = null, VisualArtifactInterchangeEnvelope? semanticInterchange = null,
        VisualSvgOptions? svgOptions = null, ChartAxisProjection? xProjection = null, ChartAxisProjection? yProjection = null,
        ChartAxisProjection? secondaryXProjection = null, ChartAxisProjection? secondaryYProjection = null,
        ChartCartesianViewport? viewport = null, ChartOrientation? cartesianOrientation = null, ChartHierarchyPreparation? hierarchyPreparation = null, ChartPolarProjection? polarProjection = null, ChartPolarViewport? polarViewport = null, ChartForgeX.Topology.TopologyHierarchyPreparation? topologyHierarchyPreparation = null) {
        _scene = scene; _accessibility = accessibility?.Clone() ?? new VisualAccessibility();
        // Portable-envelope budgets apply when semantics cross that boundary, not to static rendering.
        // Capture all mutable DTO collections now; serialize only if a host requests interchange.
        var semanticSnapshot = semanticInterchange == null ? null : VisualArtifactInterchangeSnapshot.Capture(semanticInterchange);
        _semanticSnapshot = semanticSnapshot;
        _semanticInterchange = semanticSnapshot == null ? null : new Lazy<string>(semanticSnapshot.ToJson);
        _defaultSvgOptions = svgOptions;
        XProjection = xProjection; YProjection = yProjection;
        SecondaryXProjection = secondaryXProjection; SecondaryYProjection = secondaryYProjection;
        CartesianViewport = viewport; CartesianOrientation = cartesianOrientation;
        HierarchyPreparation = hierarchyPreparation; TopologyHierarchyPreparation = topologyHierarchyPreparation; PolarProjection = polarProjection; PolarViewport = polarViewport;
        _svgIdPrefix = new Lazy<string>(() => _defaultSvgOptions?.IdPrefix ?? VisualSceneSvgRenderer.Identity(_scene, _accessibility.Name,
            _accessibility.Description, _accessibility.Language, _accessibility.IsDecorative, _defaultSvgOptions));
    }
    internal VisualArtifactInterchangeEnvelope? CapturedSemanticSnapshot => _semanticSnapshot == null ? null : VisualArtifactInterchangeSnapshot.Capture(_semanticSnapshot);
    internal VisualArtifactInterchangeEnvelope? SemanticInterchange => _semanticInterchange == null ? null : VisualArtifactInterchangeEnvelope.FromJson(_semanticInterchange.Value);
    private PreparedVisual(VisualScene scene, PreparedVisual basis, VisualSvgOptions? svgOptions, bool? capturePngText = null) {
        _scene = scene; _accessibility = basis._accessibility.Clone(); NativeTextCapture = basis.NativeTextCapture;
        _turbulenceSourceScene = basis._turbulenceSourceScene;
        _capturePngText = capturePngText ?? basis._capturePngText;
        _semanticInterchange = basis._semanticInterchange; _semanticSnapshot = basis._semanticSnapshot; _defaultSvgOptions = svgOptions;
        XProjection = basis.XProjection; YProjection = basis.YProjection;
        SecondaryXProjection = basis.SecondaryXProjection; SecondaryYProjection = basis.SecondaryYProjection;
        CartesianViewport = basis.CartesianViewport; CartesianOrientation = basis.CartesianOrientation;
        HierarchyPreparation = basis.HierarchyPreparation; TopologyHierarchyPreparation = basis.TopologyHierarchyPreparation; PolarProjection = basis.PolarProjection; PolarViewport = basis.PolarViewport;
        _svgIdPrefix = new Lazy<string>(() => _defaultSvgOptions?.IdPrefix ?? VisualSceneSvgRenderer.Identity(_scene, _accessibility.Name,
            _accessibility.Description, _accessibility.Language, _accessibility.IsDecorative, _defaultSvgOptions));
    }
    internal PreparedVisual WithScene(VisualScene scene, VisualSvgOptions? svgOptions) => new PreparedVisual(scene, this, svgOptions);
    // Native frame capture retains the accepted namespace/paint and PNG-text policy.
    internal PreparedVisual WithNativeFrame(VisualScene scene) => new PreparedVisual(scene, this, _defaultSvgOptions);
    /// <summary>Decorates only an adapter-owned serialization snapshot; native scene and ordinary exports remain detached.</summary>
    internal PreparedVisual ForInteractivePngExport() => _capturePngText ? this : new PreparedVisual(_scene, this, _defaultSvgOptions, true);
    internal PreparedVisual WithCapturedText(VisualSceneTextCapture? capture) {
        if (capture == null) return this;
        var copy = new PreparedVisual(_scene, this, _defaultSvgOptions); copy.NativeTextCapture = capture; return copy;
    }
    /// <summary>A newly compiled complete frame retains its own semantics, projections and regions; only serialization policy comes from the accepted basis.</summary>
    internal PreparedVisual WithCapturedExportPolicy(PreparedVisual basis) => new PreparedVisual(_scene, this, basis._defaultSvgOptions, basis._capturePngText);
    internal VisualScene Scene => _scene;
    internal ChartHierarchyPreparation? HierarchyPreparation { get; }
    internal ChartForgeX.Topology.TopologyHierarchyPreparation? TopologyHierarchyPreparation { get; }
    /// <summary>Gets the immutable organization hierarchy request, independently of numeric chart hierarchy views.</summary>
    public ChartForgeX.Topology.TopologyHierarchyView? TopologyHierarchyView => TopologyHierarchyPreparation?.View;
    /// <summary>Gets the immutable subtree request, or null for the complete source hierarchy.</summary>
    public ChartHierarchyView? HierarchyView => HierarchyPreparation?.Request;
    /// <summary>Gets the immutable whole-series view captured by native visibility preparation, or null for ordinary preparation.</summary>
    public ChartVisibilityView? VisibilityView => _scene.VisibilityCapture?.View;
    /// <summary>Gets the logical viewport.</summary>
    public VisualSize Size => _scene.Size;
    /// <summary>Gets preparation diagnostics.</summary>
    public IReadOnlyList<VisualDiagnostic> Diagnostics => _scene.Diagnostics;
    /// <summary>Gets descriptive source regions.</summary>
    public IReadOnlyList<VisualSemanticRegion> Regions => _scene.Regions;
    /// <summary>Gets the detached primary horizontal projection for a supported usable Cartesian scene, or null otherwise.</summary>
    /// <remarks>Ordinary preparation captures the full resolved window. This fact does not change static paint or SVG metadata.
    /// It is absent for empty, unsupported, sparkline or composite scenes that have no single supported X projection.</remarks>
    public ChartAxisProjection? XProjection { get; }
    /// <summary>Gets the detached primary Y projection for a supported usable Cartesian scene.</summary>
    /// <remarks>It is absent for empty, unsupported, sparkline or composite scenes.</remarks>
    public ChartAxisProjection? YProjection { get; }
    /// <summary>Gets the detached secondary horizontal value projection on a supported horizontal chart.</summary>
    public ChartAxisProjection? SecondaryXProjection { get; }
    /// <summary>Gets the detached secondary vertical value projection on a supported vertical chart.</summary>
    public ChartAxisProjection? SecondaryYProjection { get; }
    /// <summary>Gets the captured immutable Cartesian request, or null for full-domain preparation.</summary>
    public ChartCartesianViewport? CartesianViewport { get; }
    /// <summary>Gets the captured Cartesian orientation, or null for a non-Cartesian or unusable scene.</summary>
    public ChartOrientation? CartesianOrientation { get; }
    /// <summary>Gets the detached directional/radius projection for a usable continuous Polar scene.</summary>
    public ChartPolarProjection? PolarProjection { get; }
    /// <summary>Gets the captured polar request, or null for its full view.</summary>
    public ChartPolarViewport? PolarViewport { get; }
    /// <summary>Gets an independent copy of the text alternative.</summary>
    public VisualAccessibility Accessibility => _accessibility.Clone();
    /// <summary>Exports script-free SVG without repeating layout.</summary>
    public string ToSvg() => ExportSvg(_svgIdPrefix.Value);
    /// <summary>Exports SVG with a host-owned ID namespace, allowing identical prepared visuals in one document.</summary>
    /// <param name="idPrefix">A unique host prefix beginning with an ASCII letter and containing only letters, digits, hyphens, underscores and periods.</param>
    /// <remarks>DOM IDs are scoped; source identities in semantic data attributes and regions remain unchanged.</remarks>
    public string ToSvg(string idPrefix) => ExportSvg(VisualSceneSvgRenderer.ValidatePrefix(idPrefix));
    /// <summary>Exports SVG using detached host namespace, color and safe link policies.</summary>
    public string ToSvg(VisualSvgOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return VisualSceneSvgRenderer.Render(_scene, _accessibility.Name, _accessibility.Description,
            _accessibility.Language, _accessibility.IsDecorative, options.IdPrefix, options, _capturePngText);
    }
    private string ExportSvg(string idPrefix) => VisualSceneSvgRenderer.Render(_scene, _accessibility.Name,
        _accessibility.Description, _accessibility.Language, _accessibility.IsDecorative, idPrefix, _defaultSvgOptions, _capturePngText);
    /// <summary>Exports an owning artifact's metadata without altering the prepared snapshot.</summary>
    internal string ToSvg(VisualAccessibility accessibility, string? idPrefix = null, int? maximumCharacters = null) {
        if (accessibility == null) throw new ArgumentNullException(nameof(accessibility));
        return VisualSceneSvgRenderer.Render(_scene, accessibility.Name, accessibility.Description,
            accessibility.Language, accessibility.IsDecorative, idPrefix ?? _defaultSvgOptions?.IdPrefix, _defaultSvgOptions, _capturePngText, maximumCharacters);
    }
    /// <summary>Exports native RGBA pixels without repeating layout or parsing SVG.</summary>
    public RgbaImage ToRgba(VisualRenderOptions? options = null) {
        options ??= new VisualRenderOptions();
        return VisualSceneRasterRenderer.Render(_scene, options.Scale, options.Supersampling, options.PixelBudget, options.TextHinting);
    }
    /// <summary>Exports PNG from the native raster backend.</summary>
    public byte[] ToPng(VisualRenderOptions? options = null) => PngWriter.WriteRgba(ToRgba(options));
}
