using System;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactRendering {
    /// <summary>Wraps an immutable prepared visual for static export and portable host handoff.</summary>
    /// <param name="prepared">The detached static output.</param>
    /// <param name="id">The stable artifact ID; it must match a supplied semantic envelope.</param>
    /// <param name="kind">The declared artifact kind; it must match a supplied semantic envelope.</param>
    /// <param name="semanticInterchange">Optional source-model semantics, captured independently of the display scene.</param>
    /// <param name="title">Optional artifact title; defaults to the semantic title or prepared accessible name.</param>
    /// <param name="subtitle">Optional artifact subtitle; defaults to the semantic subtitle.</param>
    /// <returns>A mutable host envelope whose render model and captured semantic data are detached snapshots.</returns>
    /// <remarks>
    /// Native diagram data is never inferred from scene commands. Supplied semantic dimensions must match the
    /// prepared viewport when present. Artifact metadata can be edited without changing prepared pixels or geometry.
    /// </remarks>
    public static VisualArtifact ToArtifact(this PreparedVisual prepared, string id, VisualArtifactKind kind,
        VisualArtifactInterchangeEnvelope? semanticInterchange = null, string? title = null, string? subtitle = null) {
        if (prepared == null) throw new ArgumentNullException(nameof(prepared));
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable artifact ID is required.", nameof(id));
        if (!Enum.IsDefined(typeof(VisualArtifactKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        new VisualArtifactInterchangeEnvelope { Id = id, Kind = kind }.Validate();
        VisualArtifactInterchangeEnvelope? semantics = null;
        if (semanticInterchange == null) {
            semanticInterchange = prepared.SemanticInterchange;
            if (semanticInterchange != null) semanticInterchange.Id = id;
        }
        if (semanticInterchange != null) {
            // The versioned writer/reader captures every supported typed field and validates the portable boundary.
            semantics = VisualArtifactInterchangeEnvelope.FromUtf8Json(semanticInterchange.ToUtf8Json());
            if (!string.Equals(semantics.Id, id, StringComparison.Ordinal) || semantics.Kind != kind)
                throw new ArgumentException("Semantic artifact ID and kind must match the prepared artifact.", nameof(semanticInterchange));
            if (semantics.Width.HasValue && semantics.Width.Value != prepared.Size.Width ||
                semantics.Height.HasValue && semantics.Height.Value != prepared.Size.Height)
                throw new ArgumentException("Semantic dimensions must match the prepared viewport.", nameof(semanticInterchange));
        }
        var accessibility = prepared.Accessibility;
        var artifact = VisualArtifact.Create(id, kind, prepared);
        artifact.Title = title ?? semantics?.Title ?? accessibility.Name ?? string.Empty;
        artifact.Subtitle = subtitle ?? semantics?.Subtitle ?? string.Empty;
        artifact.SourceLanguage = semantics?.SourceLanguage ?? VisualArtifactSourceLanguage.Native;
        artifact.NaturalSize = new VisualArtifactSize(prepared.Size.Width, prepared.Size.Height);
        artifact.PreserveNaturalSize = true;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png |
            VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Office;
        artifact.Accessibility.Name = semantics?.AccessibleName ?? accessibility.Name;
        artifact.Accessibility.Description = semantics?.AccessibleDescription ?? accessibility.Description;
        artifact.Accessibility.Language = semantics?.Language ?? accessibility.Language;
        artifact.Accessibility.IsDecorative = semantics?.IsDecorative ?? accessibility.IsDecorative;
        if (semantics != null) {
            artifact.ExportFormats |= VisualArtifactExportFormat.Json;
            artifact.PreparedInterchangeSnapshot = semantics.ToJson();
            foreach (var pair in semantics.Extensions) artifact.Metadata.Add(pair.Key, pair.Value);
        }
        foreach (var region in prepared.Regions) {
            artifact.Regions.Add(new VisualArtifactRegion {
                Id = region.Id, Kind = region.Role, Label = region.Label ?? string.Empty,
                Bounds = region.Bounds, AlternativeText = region.Label
            });
        }
        return artifact;
    }

    internal static PreparedVisual PreparedModel(VisualArtifact artifact, PreparedVisual prepared) {
        if (!artifact.NaturalSize.HasValue || artifact.NaturalSize.Value.Width != prepared.Size.Width ||
            artifact.NaturalSize.Value.Height != prepared.Size.Height)
            throw new InvalidOperationException("A prepared artifact's natural size must match its immutable viewport. Prepare again to change size.");
        return prepared;
    }

    private static string RenderPreparedSvg(VisualArtifact artifact, PreparedVisual prepared) {
        var accessibility = artifact.Accessibility.Clone();
        accessibility.Name ??= artifact.Title.Length == 0 ? artifact.Id : artifact.Title;
        return PreparedModel(artifact, prepared).ToSvg(accessibility,
            SvgRenderedIdentity.CreateProvisionalId("artifact", artifact.Id));
    }

    private static string RenderPreparedHtml(VisualArtifact artifact, PreparedVisual prepared, VisualArtifactRenderOptions? options) {
        PreparedModel(artifact, prepared);
        var language = string.IsNullOrWhiteSpace(artifact.Accessibility.Language) ? "en" : artifact.Accessibility.Language!;
        var title = artifact.Title.Length == 0 ? artifact.Id : artifact.Title;
        return "<!doctype html><html lang=\"" + EscapeHtml(language) + "\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + EscapeHtml(title) +
            "</title><style>body{margin:0;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}" +
            "svg{display:block;max-width:100%;height:auto;overflow:visible}" +
            "@media print{body{margin:0;background:transparent}svg{max-width:none}}</style></head><body>" +
            artifact.ToSvg(options) + "</body></html>";
    }
}
