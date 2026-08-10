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
        if (artifact.Model is TopologyChart) TopologyHtmlRenderer.EnsureStatic(TopologyOptions(artifact, options));
        if (options != null && options.Watermarks.Count > 0) return WrapSvgPage(artifact.Title.Length == 0 ? artifact.Id : artifact.Title, artifact.ToSvg(options), artifact.Accessibility.Language, clipSvgViewport: true);
        var html = artifact.Model switch {
            Chart chart => chart.ToHtmlPage(),
            ChartGrid grid => grid.ToHtmlPage(),
            VisualCanvas canvas => canvas.ToHtmlPage(),
            VisualStory story => story.ToHtmlPage(),
            TopologyChart topology => RenderTopologyHtml(artifact, topology, options),
            FlowArtifact flow => flow.ToHtmlPage(),
            TableArtifact table => table.ToHtmlPage(),
            SequenceArtifact sequence => WrapSvgPage(sequence.Title.Length == 0 ? sequence.Id : sequence.Title, sequence.ToSvg(), artifact.Accessibility.Language),
            IVisualBlock block => block.ToHtmlPage(),
            _ => throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported HTML render model.")
        };
        return WithDocumentLanguage(html, artifact.Accessibility.Language);
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
            TopologyChart topology => RenderTopologyPng(artifact, topology, options),
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

    internal static string WrapSvgPage(string title, string svg, string? language = null, bool clipSvgViewport = false) {
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "ChartForgeX visual artifact" : title.Trim();
        var safeLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language!.Trim();
        var svgOverflow = clipSvgViewport ? "hidden" : "visible";
        return "<!doctype html><html lang=\"" + EscapeHtml(safeLanguage) + "\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + EscapeHtml(safeTitle) + "</title><style>html,body{margin:0;min-height:100%;background:linear-gradient(180deg,#f8fafc,#e2e8f0)}body{display:grid;place-items:center;padding:24px;box-sizing:border-box;font-family:Inter,ui-sans-serif,system-ui,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-visual-artifact{max-width:100%;height:auto}.chartforgex-visual-artifact svg{display:block;max-width:100%;height:auto;overflow:" + svgOverflow + "}@media print{html,body{background:transparent}body{padding:0}.chartforgex-visual-artifact{max-width:none}}</style></head><body><div class=\"chartforgex-visual-artifact\">" + svg + "</div></body></html>";
    }

    private static string WithDocumentLanguage(string html, string? language) {
        var safeLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language!.Trim();
        var htmlStart = html.IndexOf("<html", StringComparison.OrdinalIgnoreCase);
        if (htmlStart < 0) return html;
        var tagEnd = html.IndexOf('>', htmlStart);
        if (tagEnd < 0) return html;
        var langStart = html.IndexOf(" lang=\"", htmlStart, tagEnd - htmlStart, StringComparison.OrdinalIgnoreCase);
        if (langStart < 0) return html.Insert(tagEnd, " lang=\"" + EscapeHtml(safeLanguage) + "\"");
        var valueStart = langStart + 7;
        var valueEnd = html.IndexOf('\"', valueStart);
        if (valueEnd < 0 || valueEnd > tagEnd) return html;
        return html.Substring(0, valueStart) + EscapeHtml(safeLanguage) + html.Substring(valueEnd);
    }

    private static string RenderSvg(VisualArtifact artifact, VisualArtifactRenderOptions? options) {
        return artifact.Model switch {
            Chart chart => chart.ToSvg(),
            ChartGrid grid => grid.ToSvg(),
            VisualCanvas canvas => canvas.ToSvg(),
            VisualStory story => story.ToSvg(),
            TopologyChart topology => RenderTopologySvg(artifact, topology, options),
            FlowArtifact flow => flow.ToSvg(),
            TableArtifact table => table.ToSvg(),
            SequenceArtifact sequence => sequence.ToSvg(),
            IVisualBlock block => block.ToSvg(),
            _ => throw new InvalidOperationException("Artifact '" + artifact.Id + "' does not expose a supported SVG render model.")
        };
    }

    private static TopologyRenderOptions? TopologyOptions(VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions) {
        var topologyOptions = renderOptions?.Topology?.CloneForRendering();
        if (!artifact.PreserveNaturalSize) return topologyOptions;
        if (topologyOptions == null) return new TopologyRenderOptions { FitContentToViewport = true };
        topologyOptions.FitContentToViewport = true;
        return topologyOptions;
    }

    private static string RenderTopologySvg(VisualArtifact artifact, TopologyChart topology, VisualArtifactRenderOptions? renderOptions) {
        var model = TopologyModel(artifact, topology);
        var options = TopologyOptions(artifact, renderOptions);
        TopologyArtifactRendering.RefreshRegions(artifact, model, options);
        return model.ToSvg(options);
    }

    private static string RenderTopologyHtml(VisualArtifact artifact, TopologyChart topology, VisualArtifactRenderOptions? renderOptions) {
        var model = TopologyModel(artifact, topology);
        var options = TopologyOptions(artifact, renderOptions);
        TopologyArtifactRendering.RefreshRegions(artifact, model, options);
        return model.ToHtmlPage(options);
    }

    private static byte[] RenderTopologyPng(VisualArtifact artifact, TopologyChart topology, VisualArtifactRenderOptions? renderOptions) {
        var model = TopologyModel(artifact, topology);
        var options = TopologyOptions(artifact, renderOptions);
        TopologyArtifactRendering.RefreshRegions(artifact, model, options);
        return model.ToPng(options);
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
