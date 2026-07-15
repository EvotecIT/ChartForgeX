using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Describes the product-neutral visual artifact family represented by an artifact envelope.
/// </summary>
public enum VisualArtifactKind {
    /// <summary>The artifact kind is not specified.</summary>
    Unknown,
    /// <summary>A chart artifact.</summary>
    Chart,
    /// <summary>A topology diagram artifact.</summary>
    Topology,
    /// <summary>A flow diagram artifact.</summary>
    Flow,
    /// <summary>A sequence or interaction diagram artifact.</summary>
    Sequence,
    /// <summary>A table artifact.</summary>
    Table,
    /// <summary>A timeline or activity artifact.</summary>
    Timeline,
    /// <summary>A visual block artifact.</summary>
    VisualBlock,
    /// <summary>A Mermaid-authored diagram artifact.</summary>
    Mermaid
}

/// <summary>
/// Identifies the source language used to author a visual artifact.
/// </summary>
public enum VisualArtifactSourceLanguage {
    /// <summary>The source language is unknown or not applicable.</summary>
    Unknown,
    /// <summary>The artifact came from imperative .NET code.</summary>
    Native,
    /// <summary>The artifact came from ChartForgeX markup.</summary>
    ChartForgeX,
    /// <summary>The artifact came from Mermaid markup.</summary>
    Mermaid,
    /// <summary>The artifact came from Markdown.</summary>
    Markdown
}

/// <summary>
/// Declares static export formats an artifact can produce without host-specific UI.
/// </summary>
[Flags]
public enum VisualArtifactExportFormat {
    /// <summary>No static export format is declared.</summary>
    None = 0,
    /// <summary>Scalable vector graphics export.</summary>
    Svg = 1,
    /// <summary>Portable network graphics export.</summary>
    Png = 2,
    /// <summary>Static HTML export.</summary>
    Html = 4,
    /// <summary>Comma-separated values export.</summary>
    Csv = 8,
    /// <summary>JSON export.</summary>
    Json = 16,
    /// <summary>Markdown export.</summary>
    Markdown = 32,
    /// <summary>Document or spreadsheet handoff export.</summary>
    Office = 64
}

/// <summary>
/// Describes one product-neutral visual artifact that can be rendered, inspected, exported, or handed to a host adapter.
/// </summary>
public sealed class VisualArtifact {
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private VisualArtifactExportFormat _exportFormats;

    /// <summary>Gets or sets a stable artifact identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the artifact kind.</summary>
    public VisualArtifactKind Kind { get; set; }

    /// <summary>Gets or sets the source language used to author the artifact.</summary>
    public VisualArtifactSourceLanguage SourceLanguage { get; set; } = VisualArtifactSourceLanguage.Native;

    /// <summary>Gets or sets the artifact title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string Subtitle { get => _subtitle; set => _subtitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the renderer-independent artifact model.</summary>
    public object? Model { get; set; }

    /// <summary>Gets or sets the static export formats supported by this artifact.</summary>
    public VisualArtifactExportFormat ExportFormats {
        get => _exportFormats;
        set {
            VisualArtifactGuards.ExportFormatsDefined(value, nameof(value));
            _exportFormats = value;
        }
    }

    /// <summary>Gets or sets the artifact's natural size in pixels when known.</summary>
    public VisualArtifactSize? NaturalSize { get; set; }

    /// <summary>Gets artifact metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets host-inspectable visual regions.</summary>
    public List<VisualArtifactRegion> Regions { get; } = new();

    /// <summary>Gets legend items associated with the artifact.</summary>
    public List<VisualArtifactLegendItem> Legend { get; } = new();

    /// <summary>Creates a visual artifact envelope for a model.</summary>
    public static VisualArtifact Create(string id, VisualArtifactKind kind, object model) {
        if (model == null) throw new ArgumentNullException(nameof(model));
        return new VisualArtifact {
            Id = id ?? throw new ArgumentNullException(nameof(id)),
            Kind = kind,
            Model = model
        };
    }

    /// <summary>Returns true when the artifact declares support for the requested export format.</summary>
    public bool SupportsExport(VisualArtifactExportFormat format) => format != VisualArtifactExportFormat.None && (ExportFormats & format) == format;
}

/// <summary>
/// Describes an artifact size in pixels.
/// </summary>
public readonly struct VisualArtifactSize {
    /// <summary>Initializes a new artifact size.</summary>
    public VisualArtifactSize(double width, double height) {
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Artifact width must be finite and greater than zero.");
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Artifact height must be finite and greater than zero.");
        Width = width;
        Height = height;
    }

    /// <summary>Gets the width in pixels.</summary>
    public double Width { get; }

    /// <summary>Gets the height in pixels.</summary>
    public double Height { get; }
}

/// <summary>
/// Describes one host-inspectable region inside a rendered visual artifact.
/// </summary>
public sealed class VisualArtifactRegion {
    private string _id = string.Empty;
    private string _kind = string.Empty;
    private string _label = string.Empty;

    /// <summary>Gets or sets a stable region identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets a product-neutral region kind token.</summary>
    public string Kind { get => _kind; set => _kind = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the region bounds when known.</summary>
    public ChartRect? Bounds { get; set; }

    /// <summary>Gets region metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Describes one legend item associated with an artifact.
/// </summary>
public sealed class VisualArtifactLegendItem {
    private string _id = string.Empty;
    private string _label = string.Empty;

    /// <summary>Gets or sets the legend item identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the legend label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional color token.</summary>
    public string? Color { get; set; }
}
