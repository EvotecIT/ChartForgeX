using System;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Decorates static artifact presentation while preserving its neutral semantic model.</summary>
public static class VisualWatermarkDecoration {
    /// <summary>Appends a snapshot of the supplied watermarks in declaration order.</summary>
    /// <remarks>The artifact retains its semantic model. Repeated calls append presentation layers.</remarks>
    public static VisualArtifact WithWatermarks(this VisualArtifact artifact, params VisualWatermark[] watermarks) =>
        WithWatermarks(artifact, null, watermarks);

    /// <summary>Appends watermark presentation over a static source resolved with the supplied core options.</summary>
    public static VisualArtifact WithWatermarks(this VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions,
        params VisualWatermark[] watermarks) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        if (watermarks == null) throw new ArgumentNullException(nameof(watermarks));
        if (watermarks.Length == 0) return artifact;
        var snapshots = new VisualWatermark[watermarks.Length];
        for (var index = 0; index < watermarks.Length; index++) {
            if (watermarks[index] == null) throw new ArgumentException("Watermarks cannot contain null entries.", nameof(watermarks));
            snapshots[index] = watermarks[index].Snapshot();
        }
        var source = VisualArtifactRendering.GetRenderSource(artifact, renderOptions);
        artifact.RenderSource = new WatermarkedStaticSource(source, artifact.NaturalSize, snapshots);
        return artifact;
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
