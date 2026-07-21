using System;
using System.IO;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Provides static rendering helpers for product-neutral visual artifact envelopes.
/// </summary>
public static class VisualArtifactRendering {
    /// <summary>
    /// Renders a supported visual artifact model to SVG.
    /// </summary>
    /// <param name="artifact">The artifact envelope.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this VisualArtifact artifact) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        switch (artifact.Model) {
            case Chart chart:
                return chart.ToSvg();
            case TopologyChart topology:
                return TopologyModel(artifact, topology).ToSvg(TopologyOptions(artifact));
            case FlowArtifact flow:
                return flow.ToSvg();
            case TableArtifact table:
                return table.ToSvg();
            case SequenceArtifact sequence:
                return sequence.ToSvg();
            case IVisualBlock block:
                return block.ToSvg();
            default:
                throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported SVG render model.");
        }
    }

    /// <summary>
    /// Renders a supported visual artifact model to a standalone HTML page.
    /// </summary>
    /// <param name="artifact">The artifact envelope.</param>
    /// <returns>HTML markup.</returns>
    public static string ToHtmlPage(this VisualArtifact artifact) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        switch (artifact.Model) {
            case Chart chart:
                return chart.ToHtmlPage();
            case TopologyChart topology:
                return TopologyModel(artifact, topology).ToHtmlPage(TopologyOptions(artifact));
            case FlowArtifact flow:
                return flow.ToHtmlPage();
            case TableArtifact table:
                return table.ToHtmlPage();
            case SequenceArtifact sequence:
                return WrapSvgPage(sequence.Title.Length == 0 ? sequence.Id : sequence.Title, sequence.ToSvg());
            case IVisualBlock block:
                return block.ToHtmlPage();
            default:
                throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported HTML render model.");
        }
    }

    /// <summary>
    /// Renders a supported visual artifact model to PNG.
    /// </summary>
    /// <param name="artifact">The artifact envelope.</param>
    /// <returns>PNG bytes.</returns>
    public static byte[] ToPng(this VisualArtifact artifact) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        switch (artifact.Model) {
            case Chart chart:
                return chart.ToPng();
            case TopologyChart topology:
                return TopologyModel(artifact, topology).ToPng(TopologyOptions(artifact));
            case FlowArtifact flow:
                return flow.ToPng();
            case TableArtifact table:
                return table.ToPng();
            case SequenceArtifact sequence:
                return sequence.ToPng();
            case IVisualBlock block:
                return block.ToPng();
            default:
                throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported PNG render model.");
        }
    }

    /// <summary>
    /// Saves a supported visual artifact model to SVG.
    /// </summary>
    /// <param name="artifact">The artifact envelope.</param>
    /// <param name="path">The target SVG path.</param>
    public static void SaveSvg(this VisualArtifact artifact, string path) => File.WriteAllText(path, artifact.ToSvg(), Encoding.UTF8);

    /// <summary>
    /// Saves a supported visual artifact model to a standalone HTML page.
    /// </summary>
    /// <param name="artifact">The artifact envelope.</param>
    /// <param name="path">The target HTML path.</param>
    public static void SaveHtml(this VisualArtifact artifact, string path) => File.WriteAllText(path, artifact.ToHtmlPage(), Encoding.UTF8);

    /// <summary>
    /// Saves a supported visual artifact model to PNG.
    /// </summary>
    /// <param name="artifact">The artifact envelope.</param>
    /// <param name="path">The target PNG path.</param>
    public static void SavePng(this VisualArtifact artifact, string path) => File.WriteAllBytes(path, artifact.ToPng());

    internal static string WrapSvgPage(string title, string svg) {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "ChartForgeX visual artifact" : title.Trim();
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + EscapeHtml(safeTitle) + "</title><style>html,body{margin:0;min-height:100%;background:linear-gradient(180deg,#f8fafc,#e2e8f0)}body{display:grid;place-items:center;padding:24px;box-sizing:border-box;font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-visual-artifact{max-width:100%;height:auto}.chartforgex-visual-artifact svg{display:block;max-width:100%;height:auto;overflow:visible}@media print{html,body{background:transparent}body{padding:0}.chartforgex-visual-artifact{max-width:none}}</style></head><body><div class=\"chartforgex-visual-artifact\">" + svg + "</div></body></html>";
    }

    private static TopologyRenderOptions? TopologyOptions(VisualArtifact artifact) =>
        artifact.PreserveNaturalSize ? new TopologyRenderOptions { FitContentToViewport = true } : null;

    private static TopologyChart TopologyModel(VisualArtifact artifact, TopologyChart topology) {
        if (!artifact.PreserveNaturalSize) return topology;
        if (!artifact.NaturalSize.HasValue) {
            throw new InvalidOperationException("Artifact '" + artifact.Id + "' cannot preserve its natural size because no natural size is defined.");
        }

        var naturalSize = artifact.NaturalSize.Value;
        if (topology.Viewport.Width == naturalSize.Width && topology.Viewport.Height == naturalSize.Height) return topology;
        var copy = TopologyLayoutEngine.Clone(topology);
        copy.WithViewport(naturalSize.Width, naturalSize.Height, topology.Viewport.Padding);
        return copy;
    }

    private static string EscapeHtml(string value) {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
