using System;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactRendering {
    /// <summary>Measures the exact destination pixel dimensions of a core native artifact without allocating its raster canvas.</summary>
    /// <remarks>Uses the same native preparation and output scale as raster export, including natural topology and
    /// sequence growth. Arbitrary producer-owned render sources have no core measurement contract and are rejected.</remarks>
    public static VisualArtifactSize MeasureRasterSize(this VisualArtifact artifact, VisualArtifactRenderOptions? options = null) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        if (artifact.RenderSource != null) throw new InvalidOperationException("A producer-owned render source must supply its own raster-size policy.");
        double width, height;
        var scale = 1;
        switch (artifact.Model) {
            case PreparedVisual prepared:
                var captured = PreparedModel(artifact, prepared).Size;
                width = captured.Width; height = captured.Height;
                break;
            case Chart chart:
                width = chart.Options.Size.Width; height = chart.Options.Size.Height;
                scale = chart.Options.PngOutputScale;
                break;
            case TopologyChart topology:
                var topologyOptions = TopologyOptions(artifact, options);
                var layout = PrepareTopologyArtifact(artifact, TopologyModel(artifact, topology), topologyOptions);
                width = layout.Width; height = layout.Height;
                scale = topologyOptions?.PngOutputScale ?? 1;
                break;
            case SequenceArtifact sequence:
                var sequenceSize = SequencePreparedCompiler.PrepareDefault(sequence).Size;
                width = sequenceSize.Width; height = sequenceSize.Height;
                break;
            case FlowArtifact flow:
                var request = VisualExportRequest.ForFlow(flow);
                var flowSize = flow.Prepare(request.Context).Size;
                width = flowSize.Width; height = flowSize.Height;
                scale = request.RasterOptions.Scale;
                break;
            case IVisualBlock block when SvgVisualBlockRenderer.IsNativeBlock(block):
                width = block.Options.Size.Width; height = block.Options.Size.Height;
                scale = block.Options.PngOutputScale;
                break;
            default:
                throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a core native raster-size contract.");
        }
        return new VisualArtifactSize(Math.Ceiling(width * scale), Math.Ceiling(height * scale));
    }
}
