using System;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactRendering {
    /// <summary>Renders an artifact directly to native pixels, before format encoding.</summary>
    /// <param name="artifact">The source semantics and optional static presentation.</param>
    /// <param name="options">Optional core rendering options. Raster encoding options apply when encoding the result.</param>
    /// <returns>A detached RGBA image at the producer's configured output density.</returns>
    public static RgbaImage ToRgbaImage(this VisualArtifact artifact, VisualArtifactRenderOptions? options = null) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        var source = artifact.RenderSource ?? artifact.Model as IStaticVisualSource;
        if (source != null) return source.RenderRgba();
        return artifact.Model switch {
            PreparedVisual prepared => PreparedModel(artifact, prepared).ToRgba(),
            Chart chart => RasterRenderer.RenderImage(chart),
            ChartGrid grid => RasterRenderer.RenderImage(grid),
            TopologyChart topology => PrepareTopologyArtifact(artifact, TopologyModel(artifact, topology), TopologyOptions(artifact, options)).ToRgba(),
            FlowArtifact flow => RenderFlowImage(flow),
            SequenceArtifact sequence => SequencePreparedCompiler.PrepareDefault(sequence).ToRgba(),
            _ => throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported static render model.")
        };
    }

    private static RgbaImage RenderFlowImage(FlowArtifact flow) {
        var request = VisualExportRequest.ForFlow(flow);
        return flow.Prepare(request.Context).ToRgba(request.RasterOptions);
    }

    // Optional decorators capture this handoff before replacing RenderSource. The source envelope is
    // detached from later presentation/identity edits; its semantic model retains ordinary producer mutability.
    internal static IStaticVisualSource GetRenderSource(VisualArtifact artifact, VisualArtifactRenderOptions? options = null) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        if (artifact.RenderSource != null) return artifact.RenderSource;
        if (artifact.Model is IStaticVisualSource source) return source;
        var model = artifact.Model ?? throw new InvalidOperationException("Artifact '" + artifact.Id + "' has no static render model.");
        var snapshot = VisualArtifact.Create(artifact.Id, artifact.Kind, model);
        snapshot.Title = artifact.Title; snapshot.Subtitle = artifact.Subtitle;
        snapshot.NaturalSize = artifact.NaturalSize; snapshot.PreserveNaturalSize = artifact.PreserveNaturalSize;
        snapshot.TopologyNaturalSizeSnapshot = artifact.TopologyNaturalSizeSnapshot;
        snapshot.HasModelAccessibilitySnapshot = artifact.HasModelAccessibilitySnapshot;
        CopyAccessibility(artifact.Accessibility, snapshot.Accessibility);
        CopyAccessibility(artifact.ModelAccessibilitySnapshot, snapshot.ModelAccessibilitySnapshot);
        var capturedOptions = options == null ? null : new VisualArtifactRenderOptions {
            Topology = options.Topology?.CloneForRendering(),
            Raster = options.Raster == null ? null : new RasterImageOptions {
                Background = options.Raster.Background, JpegQuality = options.Raster.JpegQuality,
                PngCompressionLevel = options.Raster.PngCompressionLevel, Dpi = options.Raster.Dpi
            }
        };
        return new ArtifactStaticSource(snapshot, capturedOptions);
    }

    private static void CopyAccessibility(Accessibility.VisualAccessibility source, Accessibility.VisualAccessibility target) {
        target.Name = source.Name; target.Description = source.Description;
        target.Language = source.Language; target.IsDecorative = source.IsDecorative;
    }

    private sealed class ArtifactStaticSource : IStaticVisualSource {
        private readonly VisualArtifact _artifact;
        private readonly VisualArtifactRenderOptions? _options;
        internal ArtifactStaticSource(VisualArtifact artifact, VisualArtifactRenderOptions? options) { _artifact = artifact; _options = options; }
        public string RenderSvg(string idScope) => VisualArtifactRendering.RenderSvg(_artifact, _options, idScope ?? throw new ArgumentNullException(nameof(idScope)));
        public RgbaImage RenderRgba() => _artifact.ToRgbaImage(_options);
    }
}
