using System;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Decorates static artifact presentation while preserving its neutral semantic model.</summary>
public static class VisualWatermarkDecoration {
    /// <summary>Appends watermark layers to trusted, already-rendered SVG without resolving the artifact's model.</summary>
    /// <param name="artifact">The unchanged host envelope; its natural size is a fallback when SVG dimensions are absent.</param>
    /// <param name="renderedSvg">A complete trusted SVG document supplied by a static or animated renderer.</param>
    /// <param name="watermarks">Watermarks captured in declaration order.</param>
    /// <returns>The supplied markup with watermark layers inserted before its closing SVG element.</returns>
    /// <remarks>Existing animation, identifiers, and semantic markup are retained. This method does not sanitize
    /// untrusted markup, freeze a producer, or change the artifact. An empty watermark array returns the supplied SVG.</remarks>
    public static string ToWatermarkedSvg(this VisualArtifact artifact, string renderedSvg, params VisualWatermark[] watermarks) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        if (renderedSvg == null) throw new ArgumentNullException(nameof(renderedSvg));
        if (string.IsNullOrWhiteSpace(renderedSvg)) throw new ArgumentException("Rendered SVG is required.", nameof(renderedSvg));
        var snapshots = SnapshotWatermarks(watermarks);
        var size = artifact.NaturalSize;
        if (size.HasValue) size = new VisualArtifactSize(size.Value.Width, size.Value.Height);
        return VisualWatermarkRendering.ApplyToSvg(renderedSvg, new VisualArtifact { NaturalSize = size }, snapshots);
    }

    /// <summary>Wraps trusted, already-rendered SVG with watermarks in a standalone HTML page.</summary>
    /// <param name="artifact">The unchanged host envelope supplying the title, language, and natural-size fallback.</param>
    /// <param name="renderedSvg">A complete trusted SVG document supplied by a static or animated renderer.</param>
    /// <param name="watermarks">Watermarks captured in declaration order.</param>
    /// <returns>A standalone page containing the decorated SVG and the artifact's host title and language.</returns>
    /// <remarks>Markup ownership and preservation follow <see cref="ToWatermarkedSvg"/>. The page clips watermark
    /// overflow to the SVG viewport while retaining the renderer's animation and semantic content.</remarks>
    public static string ToWatermarkedHtmlPage(this VisualArtifact artifact, string renderedSvg, params VisualWatermark[] watermarks) {
        var svg = ToWatermarkedSvg(artifact, renderedSvg, watermarks);
        return VisualArtifactRendering.WrapSvgPage(artifact.Title.Length == 0 ? artifact.Id : artifact.Title,
            svg, artifact.Accessibility.Language, clipSvgViewport: true);
    }

    /// <summary>Creates a detached artifact envelope with watermark presentation in declaration order.</summary>
    /// <remarks>The source artifact is preserved. The returned envelope borrows the semantic model and existing
    /// static source, with the ownership and lifetime requirements described by <see cref="VisualArtifact.Clone"/>.</remarks>
    public static VisualArtifact ToWatermarkedArtifact(this VisualArtifact artifact, params VisualWatermark[] watermarks) =>
        ToWatermarkedArtifact(artifact, null, watermarks);

    /// <summary>Creates a detached watermarked artifact with static presentation resolved using the supplied core options.</summary>
    /// <remarks>Watermarks and rendering options are captured before presentation is attached to the copied envelope.
    /// An empty watermark array still returns an independent envelope. Semantic model changes retain ordinary producer
    /// behavior; this operation does not freeze or take ownership of a model or borrowed image buffers.</remarks>
    public static VisualArtifact ToWatermarkedArtifact(this VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions,
        params VisualWatermark[] watermarks) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        var snapshots = SnapshotWatermarks(watermarks);
        var copy = artifact.Clone();
        if (snapshots.Length == 0) return copy;
        var source = VisualArtifactRendering.GetRenderSource(copy, renderOptions);
        copy.RenderSource = new WatermarkedStaticSource(source, copy.NaturalSize, snapshots);
        return copy;
    }

    /// <summary>Appends a snapshot of the supplied watermarks in declaration order.</summary>
    /// <remarks>The artifact retains its semantic model. Repeated calls append presentation layers.</remarks>
    public static VisualArtifact WithWatermarks(this VisualArtifact artifact, params VisualWatermark[] watermarks) =>
        WithWatermarks(artifact, null, watermarks);

    /// <summary>Appends watermark presentation over a static source resolved with the supplied core options.</summary>
    public static VisualArtifact WithWatermarks(this VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions,
        params VisualWatermark[] watermarks) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        var snapshots = SnapshotWatermarks(watermarks);
        if (snapshots.Length == 0) return artifact;
        var source = VisualArtifactRendering.GetRenderSource(artifact, renderOptions);
        artifact.RenderSource = new WatermarkedStaticSource(source, artifact.NaturalSize, snapshots);
        return artifact;
    }

    private static VisualWatermark[] SnapshotWatermarks(VisualWatermark[] watermarks) {
        if (watermarks == null) throw new ArgumentNullException(nameof(watermarks));
        for (var index = 0; index < watermarks.Length; index++) {
            if (watermarks[index] == null) throw new ArgumentException("Watermarks cannot contain null entries.", nameof(watermarks));
        }
        var snapshots = new VisualWatermark[watermarks.Length];
        for (var index = 0; index < watermarks.Length; index++) snapshots[index] = watermarks[index].Snapshot();
        return snapshots;
    }

    private sealed class WatermarkedStaticSource : IStaticVisualSource {
        private readonly IStaticVisualSource _source;
        private readonly VisualArtifact _frame;
        private readonly VisualWatermark[] _watermarks;

        internal WatermarkedStaticSource(IStaticVisualSource source, VisualArtifactSize? size, VisualWatermark[] watermarks) {
            _source = source;
            _frame = new VisualArtifact { NaturalSize = size };
            _watermarks = watermarks;
        }

        public string RenderSvg(string idScope) =>
            VisualWatermarkRendering.ApplyToSvg(_source.RenderSvg(idScope), _frame, _watermarks);

        public RgbaImage RenderRgba() {
            var image = _source.RenderRgba();
            return VisualWatermarkRendering.ApplyToImage(image, _frame, _source.RenderSvg(string.Empty), _watermarks);
        }
    }
}
