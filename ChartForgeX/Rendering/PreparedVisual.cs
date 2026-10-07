using System;
using System.Collections.Generic;
using ChartForgeX.Accessibility;
using ChartForgeX.Diagnostics;
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
    public string Role { get; }
    /// <summary>Gets the descriptive bounds in logical units.</summary>
    public ChartRect Bounds { get; }
    /// <summary>Gets the text alternative.</summary>
    public string? Label { get; }
}

/// <summary>Bounded raster export configuration; logical layout remains unchanged.</summary>
public sealed class VisualRenderOptions {
    /// <summary>Creates raster options. Pixel budget includes supersampled working pixels.</summary>
    public VisualRenderOptions(int scale = 1, int supersampling = 2, long pixelBudget = 64000000) {
        if (scale < 1 || scale > 16) throw new ArgumentOutOfRangeException(nameof(scale));
        if (supersampling < 1 || supersampling > 8) throw new ArgumentOutOfRangeException(nameof(supersampling));
        if (pixelBudget < 1 || pixelBudget > 128000000) throw new ArgumentOutOfRangeException(nameof(pixelBudget));
        Scale = scale; Supersampling = supersampling; PixelBudget = pixelBudget;
    }
    /// <summary>Gets the output pixels per logical unit.</summary>
    public int Scale { get; }
    /// <summary>Gets the working coverage multiplier.</summary>
    public int Supersampling { get; }
    /// <summary>Gets the maximum working pixel count.</summary>
    public long PixelBudget { get; }
}

/// <summary>A detached immutable scene. SVG and native raster export consume the same prepared decisions.</summary>
public sealed class PreparedVisual {
    private readonly VisualScene _scene;
    private readonly VisualAccessibility _accessibility;
    private readonly Lazy<string> _svgIdPrefix;
    private readonly string? _semanticInterchange;
    internal PreparedVisual(VisualScene scene, VisualAccessibility? accessibility = null, VisualArtifactInterchangeEnvelope? semanticInterchange = null) {
        _scene = scene; _accessibility = accessibility?.Clone() ?? new VisualAccessibility();
        _semanticInterchange = semanticInterchange?.ToJson();
        _svgIdPrefix = new Lazy<string>(() => VisualSceneSvgRenderer.Identity(_scene, _accessibility.Name,
            _accessibility.Description, _accessibility.Language, _accessibility.IsDecorative));
    }
    internal VisualArtifactInterchangeEnvelope? SemanticInterchange => _semanticInterchange == null ? null : VisualArtifactInterchangeEnvelope.FromJson(_semanticInterchange);
    internal VisualScene Scene => _scene;
    /// <summary>Gets the logical viewport.</summary>
    public VisualSize Size => _scene.Size;
    /// <summary>Gets preparation diagnostics.</summary>
    public IReadOnlyList<VisualDiagnostic> Diagnostics => _scene.Diagnostics;
    /// <summary>Gets descriptive source regions.</summary>
    public IReadOnlyList<VisualSemanticRegion> Regions => _scene.Regions;
    /// <summary>Gets an independent copy of the text alternative.</summary>
    public VisualAccessibility Accessibility => _accessibility.Clone();
    /// <summary>Exports script-free SVG without repeating layout.</summary>
    public string ToSvg() => ExportSvg(_svgIdPrefix.Value);
    /// <summary>Exports SVG with a host-owned ID namespace, allowing identical prepared visuals in one document.</summary>
    /// <param name="idPrefix">A unique host prefix beginning with an ASCII letter and containing only letters, digits, hyphens, underscores and periods.</param>
    /// <remarks>DOM IDs are scoped; source identities in semantic data attributes and regions remain unchanged.</remarks>
    public string ToSvg(string idPrefix) => ExportSvg(VisualSceneSvgRenderer.ValidatePrefix(idPrefix));
    private string ExportSvg(string idPrefix) => VisualSceneSvgRenderer.Render(_scene, _accessibility.Name,
        _accessibility.Description, _accessibility.Language, _accessibility.IsDecorative, idPrefix);
    /// <summary>Exports an owning artifact's metadata without altering the prepared snapshot.</summary>
    internal string ToSvg(VisualAccessibility accessibility, string? idPrefix = null) {
        if (accessibility == null) throw new ArgumentNullException(nameof(accessibility));
        return VisualSceneSvgRenderer.Render(_scene, accessibility.Name, accessibility.Description,
            accessibility.Language, accessibility.IsDecorative, idPrefix);
    }
    /// <summary>Exports native RGBA pixels without repeating layout or parsing SVG.</summary>
    public RgbaImage ToRgba(VisualRenderOptions? options = null) {
        options ??= new VisualRenderOptions();
        return VisualSceneRasterRenderer.Render(_scene, options.Scale, options.Supersampling, options.PixelBudget);
    }
    /// <summary>Exports PNG from the native raster backend.</summary>
    public byte[] ToPng(VisualRenderOptions? options = null) => PngWriter.WriteRgba(ToRgba(options));
}
