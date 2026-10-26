using System;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactRendering {
    /// <summary>Wraps a prepared visual using its captured semantic identity and family when available.</summary>
    /// <param name="prepared">The detached static output.</param>
    /// <param name="id">Optional host ID; defaults to the captured ID or "prepared-visual" for a scene without semantics.</param>
    /// <remarks>A scene without captured semantics has kind Unknown; native diagram data is never inferred from pixels.</remarks>
    public static VisualArtifact ToArtifact(this PreparedVisual prepared, string? id = null) {
        if (prepared == null) throw new ArgumentNullException(nameof(prepared));
        var semantics = prepared.CapturedSemanticSnapshot;
        return prepared.ToArtifact(id ?? semantics?.Id ?? "prepared-visual", semantics?.Kind ?? VisualArtifactKind.Unknown);
    }

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
            semantics = prepared.CapturedSemanticSnapshot;
            if (semantics != null) semantics.Id = id;
        } else {
            // Explicit caller-supplied semantics retain the eager portable codec/validation boundary.
            semantics = VisualArtifactInterchangeEnvelope.FromUtf8Json(semanticInterchange.ToUtf8Json());
            var actualSource = prepared.CapturedTurbulenceSource;
            var suppliedSource = semantics.Presentation?.TurbulenceSource;
            if ((actualSource == null) != (suppliedSource == null) ||
                actualSource != null && !actualSource.SameSourcePolicy(suppliedSource!))
                throw new ArgumentException("Supplied procedural source facts must match the actual captured native producer.", nameof(semanticInterchange));
            if (semantics.Presentation?.CapturedEffects.Count > 0)
                throw new ArgumentException("Supplied source semantics must not carry captured effects; actual prepared effect scopes are captured once at export.", nameof(semanticInterchange));
        }
        if (semantics != null) {
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
            if (!prepared.HasUnsupportedInterchangePresentation) artifact.ExportFormats |= VisualArtifactExportFormat.Json;
            artifact.PreparedInterchangeSnapshot = new Lazy<string>(semantics.ToJson);
            foreach (var pair in semantics.Extensions) artifact.Metadata.Add(pair.Key, pair.Value);
        }
        if (prepared.Scene.HasRetainedEffects && !prepared.HasUnsupportedInterchangePresentation)
            artifact.ExportFormats |= VisualArtifactExportFormat.Json;
        foreach (var region in prepared.Regions) {
            artifact.Regions.Add(new VisualArtifactRegion {
                Id = region.Id, Kind = region.Role, Label = region.Label ?? string.Empty,
                Bounds = region.Bounds, AlternativeText = region.Label
            });
        }
        return artifact;
    }

    /// <summary>Only an explicit JSON capability query enters the same borrowed wire-validation boundary as export.</summary>
    internal static bool PreparedJsonExportSupported(VisualArtifact artifact) {
        if (artifact.Model is not PreparedVisual prepared) return false;
        try {
            VisualArtifactInterchangeMapping.ValidatePreparedJsonExport(artifact, prepared);
            return true;
        } catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (NotSupportedException) { return false; }
    }

    internal static PreparedVisual PreparedModel(VisualArtifact artifact, PreparedVisual prepared) {
        if (!artifact.NaturalSize.HasValue || artifact.NaturalSize.Value.Width != prepared.Size.Width ||
            artifact.NaturalSize.Value.Height != prepared.Size.Height)
            throw new InvalidOperationException("A prepared artifact's natural size must match its immutable viewport. Prepare again to change size.");
        return prepared;
    }

    private static string RenderPreparedSvg(VisualArtifact artifact, PreparedVisual prepared, string? idScope = null) {
        var accessibility = artifact.Accessibility.Clone();
        accessibility.Name ??= artifact.Title.Length == 0 ? artifact.Id : artifact.Title;
        return PreparedModel(artifact, prepared).ToSvg(accessibility,
            idScope == null ? SvgRenderedIdentity.CreateProvisionalId("artifact", artifact.Id) : VisualSvgOptions.NamespaceFromExternalId(idScope), SvgCharacterCeiling(artifact));
    }

    private static string RenderPreparedHtml(VisualArtifact artifact, PreparedVisual prepared, VisualArtifactRenderOptions? options) {
        PreparedModel(artifact, prepared);
        var title = artifact.Title.Length == 0 ? artifact.Id : artifact.Title;
        return WrapSvgPage(title, artifact.ToSvg(options), artifact.Accessibility.Language,
            sizing: HtmlSizing(artifact, options), plainScene: true);
    }
}
