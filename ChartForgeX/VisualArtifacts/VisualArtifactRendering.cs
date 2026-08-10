using System;
using System.IO;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Composition;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
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
    public static string ToSvg(this VisualArtifact artifact) => ToSvg(artifact, null);

    /// <summary>Renders a supported visual artifact model to SVG with artifact-wide options.</summary>
    public static string ToSvg(this VisualArtifact artifact, VisualArtifactRenderOptions? options) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        var svg = RenderSvg(artifact, options);
        return options == null || options.Watermarks.Count == 0 ? svg : VisualWatermarkRendering.ApplyToSvg(svg, artifact, options.Watermarks);
    }

    /// <summary>
    /// Renders a supported visual artifact model to a standalone HTML page.
    /// </summary>
    /// <param name="artifact">The artifact envelope.</param>
    /// <returns>HTML markup.</returns>
    public static string ToHtmlPage(this VisualArtifact artifact) => ToHtmlPage(artifact, null);

    /// <summary>Renders a supported visual artifact model to a standalone HTML page with artifact-wide options.</summary>
    public static string ToHtmlPage(this VisualArtifact artifact, VisualArtifactRenderOptions? options) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        if (options != null && options.Watermarks.Count > 0) return WrapSvgPage(artifact.Title.Length == 0 ? artifact.Id : artifact.Title, artifact.ToSvg(options));
        switch (artifact.Model) {
            case Chart chart:
                return chart.ToHtmlPage();
            case ChartGrid grid:
                return grid.ToHtmlPage();
            case VisualCanvas canvas:
                return canvas.ToHtmlPage();
            case VisualStory story:
                return story.ToHtmlPage();
            case TopologyChart topology:
                return TopologyModel(artifact, topology).ToHtmlPage(TopologyOptions(artifact, options));
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
    public static byte[] ToPng(this VisualArtifact artifact) => ToPng(artifact, null);

    /// <summary>Renders a supported visual artifact model to PNG with artifact-wide options.</summary>
    public static byte[] ToPng(this VisualArtifact artifact, VisualArtifactRenderOptions? options) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        var png = artifact.Model switch {
            Chart chart => chart.ToPng(),
            ChartGrid grid => grid.ToPng(),
            VisualCanvas canvas => canvas.ToPng(),
            VisualStory story => story.ToPng(),
            TopologyChart topology => TopologyModel(artifact, topology).ToPng(TopologyOptions(artifact, options)),
            FlowArtifact flow => flow.ToPng(),
            TableArtifact table => table.ToPng(),
            SequenceArtifact sequence => sequence.ToPng(),
            IVisualBlock block => block.ToPng(),
            _ => throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported PNG render model.")
        };
        if (options == null || options.Watermarks.Count == 0 && options.Raster == null) return png;
        var image = RasterImageDecoder.Decode(png);
        if (options.Watermarks.Count > 0) image = VisualWatermarkRendering.ApplyToImage(image, artifact, RenderSvg(artifact, options), options.Watermarks);
        return RasterImageEncoder.Encode(image, RasterImageFormat.Png, options.Raster);
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

    /// <summary>Saves a supported visual artifact model to SVG with artifact-wide options.</summary>
    public static void SaveSvg(this VisualArtifact artifact, string path, VisualArtifactRenderOptions? options) => File.WriteAllText(path, artifact.ToSvg(options), Encoding.UTF8);

    /// <summary>Saves a supported visual artifact model to standalone HTML with artifact-wide options.</summary>
    public static void SaveHtml(this VisualArtifact artifact, string path, VisualArtifactRenderOptions? options) => File.WriteAllText(path, artifact.ToHtmlPage(options), Encoding.UTF8);

    /// <summary>Saves a supported visual artifact model to PNG with artifact-wide options.</summary>
    public static void SavePng(this VisualArtifact artifact, string path, VisualArtifactRenderOptions? options) => File.WriteAllBytes(path, artifact.ToPng(options));

    internal static string WrapSvgPage(string title, string svg) {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "ChartForgeX visual artifact" : title.Trim();
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + EscapeHtml(safeTitle) + "</title><style>html,body{margin:0;min-height:100%;background:linear-gradient(180deg,#f8fafc,#e2e8f0)}body{display:grid;place-items:center;padding:24px;box-sizing:border-box;font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-visual-artifact{max-width:100%;height:auto}.chartforgex-visual-artifact svg{display:block;max-width:100%;height:auto;overflow:visible}@media print{html,body{background:transparent}body{padding:0}.chartforgex-visual-artifact{max-width:none}}</style></head><body><div class=\"chartforgex-visual-artifact\">" + svg + "</div></body></html>";
    }

    private static string RenderSvg(VisualArtifact artifact, VisualArtifactRenderOptions? options) {
        return artifact.Model switch {
            Chart chart => chart.ToSvg(),
            ChartGrid grid => grid.ToSvg(),
            VisualCanvas canvas => canvas.ToSvg(),
            VisualStory story => story.ToSvg(),
            TopologyChart topology => TopologyModel(artifact, topology).ToSvg(TopologyOptions(artifact, options)),
            FlowArtifact flow => flow.ToSvg(),
            TableArtifact table => table.ToSvg(),
            SequenceArtifact sequence => sequence.ToSvg(),
            IVisualBlock block => block.ToSvg(),
            _ => throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported SVG render model.")
        };
    }

    private static TopologyRenderOptions? TopologyOptions(VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions) {
        var topologyOptions = renderOptions?.Topology?.Clone();
        if (!artifact.PreserveNaturalSize) return topologyOptions;
        if (topologyOptions == null) return new TopologyRenderOptions { FitContentToViewport = true };
        topologyOptions.FitContentToViewport = true;
        return topologyOptions;
    }

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
