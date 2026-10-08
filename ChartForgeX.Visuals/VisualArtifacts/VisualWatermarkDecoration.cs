using System;
using System.Globalization;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Decorates static artifact presentation while preserving its neutral semantic model.</summary>
public static class VisualWatermarkDecoration {
    /// <summary>Appends watermark layers to trusted SVG output, preserving existing presentation such as SVG animation.</summary>
    /// <remarks>The SVG must declare a positive root viewBox or numeric width and height. Watermarks are snapshotted.</remarks>
    public static string ApplyToSvg(string svg, params VisualWatermark[] watermarks) {
        if (svg == null) throw new ArgumentNullException(nameof(svg));
        return VisualWatermarkRendering.ApplyToSvg(svg, new VisualArtifact(), Snapshot(watermarks));
    }

    /// <summary>Appends a snapshot of the supplied watermarks in declaration order.</summary>
    /// <remarks>The artifact retains its semantic model. Repeated calls append presentation layers.</remarks>
    public static VisualArtifact WithWatermarks(this VisualArtifact artifact, params VisualWatermark[] watermarks) =>
        WithWatermarks(artifact, null, watermarks);

    /// <summary>Appends watermark presentation over a static source resolved with the supplied core options.</summary>
    public static VisualArtifact WithWatermarks(this VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions,
        params VisualWatermark[] watermarks) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        var snapshots = Snapshot(watermarks);
        if (snapshots.Length == 0) return artifact;
        var source = VisualArtifactRendering.GetRenderSource(artifact, renderOptions);
        var decorated = new WatermarkedStaticSource(source, artifact.NaturalSize, snapshots);
        artifact.RenderSource = decorated;
        artifact.Metadata["presentation.watermarks"] = decorated.WatermarkCount.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }

    private static VisualWatermark[] Snapshot(VisualWatermark[] watermarks) {
        if (watermarks == null) throw new ArgumentNullException(nameof(watermarks));
        var snapshots = new VisualWatermark[watermarks.Length];
        for (var index = 0; index < watermarks.Length; index++) {
            if (watermarks[index] == null) throw new ArgumentException("Watermarks cannot contain null entries.", nameof(watermarks));
            snapshots[index] = watermarks[index].Snapshot();
        }
        return snapshots;
    }

    private sealed class WatermarkedStaticSource : IStaticVisualSource {
        private readonly IStaticVisualSource _source;
        private readonly VisualArtifact _frame;
        private readonly VisualWatermark[] _watermarks;
        internal int WatermarkCount { get; }

        internal WatermarkedStaticSource(IStaticVisualSource source, VisualArtifactSize? size, VisualWatermark[] watermarks) {
            _source = source;
            _frame = new VisualArtifact { NaturalSize = size };
            _watermarks = watermarks;
            WatermarkCount = checked((source is WatermarkedStaticSource previous ? previous.WatermarkCount : 0) + watermarks.Length);
        }

        public string RenderSvg(string idScope) =>
            VisualWatermarkRendering.ApplyToSvg(_source.RenderSvg(idScope), _frame, _watermarks);

        public RgbaImage RenderRgba() {
            var image = _source.RenderRgba();
            return VisualWatermarkRendering.ApplyToImage(image, _frame, _source.RenderSvg(string.Empty), _watermarks);
        }
    }
}
