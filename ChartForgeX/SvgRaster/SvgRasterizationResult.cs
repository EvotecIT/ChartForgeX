using System;
using System.Collections.Generic;
using ChartForgeX.Diagnostics;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

/// <summary>Contains raster pixels and diagnostics for content omitted by the supported SVG subset.</summary>
public sealed class SvgRasterizationResult {
    internal SvgRasterizationResult(RgbaImage image, IReadOnlyList<SvgRasterDiagnostic> diagnostics) { Image = image; Diagnostics = diagnostics; }
    /// <summary>Gets the rendered RGBA pixels.</summary>
    public RgbaImage Image { get; }
    /// <summary>Gets bounded diagnostics for encountered unsupported elements, filters, images, and references.</summary>
    /// <remarks>An empty list is not certification of complete browser SVG support.</remarks>
    public IReadOnlyList<SvgRasterDiagnostic> Diagnostics { get; }
}

/// <summary>Identifies content omitted or approximated during SVG rasterization.</summary>
public sealed class SvgRasterDiagnostic {
    internal SvgRasterDiagnostic(string code, string message, SvgRasterElement element) { Code = code; Message = message; ElementName = element.Name; ElementId = element.Get("id"); }
    /// <summary>Gets the stable diagnostic code.</summary>
    public string Code { get; }
    /// <summary>Gets the human-readable explanation.</summary>
    public string Message { get; }
    /// <summary>Gets the affected SVG element name.</summary>
    public string ElementName { get; }
    /// <summary>Gets the affected element's id when one is authored.</summary>
    public string? ElementId { get; }
    /// <summary>Gets warning severity: best-effort raster pixels can still be returned.</summary>
    public VisualDiagnosticSeverity Severity => VisualDiagnosticSeverity.Warning;
}

internal sealed class SvgRasterDiagnostics {
    private readonly List<SvgRasterDiagnostic> _items = new();
    private readonly HashSet<(SvgRasterElement Element, string Code)> _reported = new();
    internal void Report(string code, string message, SvgRasterElement element) {
        if (_items.Count >= 256 || _reported.Contains((element, code))) return;
        if (_items.Count == 255) { _items.Add(new SvgRasterDiagnostic("SFR999", "Further SVG raster diagnostics were omitted after the 256-entry limit.", element)); return; }
        _reported.Add((element, code)); _items.Add(new SvgRasterDiagnostic(code, message, element));
    }
    internal IReadOnlyList<SvgRasterDiagnostic> Snapshot() => Array.AsReadOnly(_items.ToArray());
}
