using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using ChartForgeX.Core;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Serializes a native chart layout specification for offline browser layout at arbitrary container widths.</summary>
/// <remarks>Native preparation owns data, semantic axes and paint. The browser measures text and projects the serialized instructions without scaling typography or strokes.</remarks>
public sealed class HtmlResponsiveChartRenderer {
    private static readonly Lazy<string> Script = new(() => {
        var source = new StringBuilder();
        foreach (var name in new[] { "chart-layout-geometry.js", "chart-layout-legend.js", "chart-layout.js", "responsive-chart.js" }) {
            using var stream = typeof(HtmlResponsiveChartRenderer).Assembly.GetManifestResourceStream("ChartForgeX.Interactivity.Html.Assets." + name)
                ?? throw new InvalidOperationException("The chart layout asset is missing: " + name);
            using var reader = new StreamReader(stream); source.AppendLine(reader.ReadToEnd());
        }
        return source.ToString();
    });
    private static readonly Lazy<string> RadialScript = new(() => {
        using var stream = typeof(HtmlResponsiveChartRenderer).Assembly.GetManifestResourceStream("ChartForgeX.Interactivity.Html.Assets.chart-layout-radial.js")
            ?? throw new InvalidOperationException("The radial layout asset is missing.");
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    });
    /// <summary>The shared initializer for ordinary, inert, revealed and compressed chart fragments.</summary>
    public static string InitializationScript => RadialScript.Value + Script.Value;
    /// <summary>Print sizing shared by complete fragments and hosts that install assets separately.</summary>
    public static string Stylesheet => "@media print{[data-cfx-responsive-chart]{min-width:0!important}}";

    /// <summary>Renders an offline fragment with one serialized specification and a script-free native fallback.</summary>
    /// <param name="chart">The caller's chart; authored size and axis policies are restored after preparation.</param>
    /// <param name="minimumWidth">Width below which the container scrolls.</param>
    /// <param name="maximumWidth">Maximum drawing width, at most 8192 pixels.</param>
    /// <param name="height">Native and print drawing height; defaults to the authored height. Responsive heatmaps, pies and donuts fit their screen height to readable content.</param>
    /// <param name="idScope">Optional stable SVG identifier scope.</param>
    /// <param name="compressLayouts">Compresses the serialized specification; the static fallback remains available.</param>
    /// <param name="printWidth">Maximum CSS width of the scaled script-free fallback; defaults to the authored width within the bounds.</param>
    /// <returns>A self-contained fragment with an offline debounced resize and print lifecycle.</returns>
    public string RenderFragment(Chart chart, int minimumWidth = 480, int maximumWidth = 1600, int? height = null,
        string? idScope = null, bool compressLayouts = false, int? printWidth = null) =>
        Render(chart, minimumWidth, maximumWidth, height, idScope, compressLayouts, printWidth, true);

    /// <summary>Renders a script-free host fragment with inert layout data for composition inside an interactive section.</summary>
    /// <remarks>Install <see cref="Stylesheet"/> and <see cref="InitializationScript"/> outside the section. Source drawing, layout data and slice identities match <see cref="RenderFragment"/>.</remarks>
    /// <param name="chart">The caller's chart; authored size and axis policies are restored.</param>
    /// <param name="minimumWidth">Minimum screen drawing width.</param>
    /// <param name="maximumWidth">Maximum screen drawing width.</param>
    /// <param name="height">Authored drawing height, or null to use the chart height.</param>
    /// <param name="idScope">Optional stable SVG identifier scope.</param>
    /// <param name="compressLayouts">Compresses the inert layout data.</param>
    /// <param name="printWidth">Maximum width of the native fallback.</param>
    /// <returns>The drawing and inert layout data, without style or executable script elements.</returns>
    public string RenderFragmentWithoutAssets(Chart chart, int minimumWidth = 480, int maximumWidth = 1600, int? height = null,
        string? idScope = null, bool compressLayouts = false, int? printWidth = null) =>
        Render(chart, minimumWidth, maximumWidth, height, idScope, compressLayouts, printWidth, false);

    private static string Render(Chart chart, int minimumWidth, int maximumWidth, int? height,
        string? idScope, bool compressLayouts, int? printWidth, bool assets) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (minimumWidth < 160 || maximumWidth < minimumWidth || maximumWidth > 8192) throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        if (height <= 0 || printWidth <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        var size = chart.Options.Size; var x = chart.Options.XAxis; var y = chart.Options.YAxis; var window = chart.Options.HeatmapColumnWindow;
        var drawingHeight = height ?? size.Height;
        var fallbackWidth = printWidth ?? Math.Max(minimumWidth, Math.Min(maximumWidth, size.Width));
        try {
            var specification = HtmlChartLayoutSpecification.Serialize(chart, minimumWidth, maximumWidth, drawingHeight, fallbackWidth, idScope, out var fallback);
            var data = compressLayouts ? Compress(specification) : specification;
            var attribute = compressLayouts ? "data-cfx-spec-gzip" : "data-cfx-spec";
            var payload = assets ? "<script type=\"" + (compressLayouts ? "application/octet-stream" : "application/json") + "\" " + attribute + ">" + data + "</script>"
                : "<span hidden " + attribute + ">" + WebUtility.HtmlEncode(data) + "</span>";
            var runtime = HtmlRadialLayoutSpecification.Supports(chart) ? RadialScript.Value + Script.Value : Script.Value;
            return "<div data-cfx-responsive-chart style=\"min-width:" + minimumWidth + "px;max-width:" + maximumWidth + "px;margin-inline:auto\">" +
                (assets ? "<style>" + Stylesheet + "</style>" : "") + fallback + payload + (assets ? "<script>" + runtime + "</script>" : "") + "</div>";
        } finally { chart.WithSize(size.Width, size.Height); chart.Options.XAxis = x; chart.Options.YAxis = y; chart.Options.HeatmapColumnWindow = window; }
    }
    private static string Compress(string text) {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true)) {
            var bytes = Encoding.UTF8.GetBytes(text); gzip.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(buffer.ToArray());
    }
}
