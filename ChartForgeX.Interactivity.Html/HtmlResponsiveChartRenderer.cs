using System;
using System.IO;
using System.IO.Compression;
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
    /// <summary>The shared initializer for ordinary, inert, revealed and compressed chart fragments.</summary>
    public static string InitializationScript => Script.Value;

    /// <summary>Renders an offline fragment with one serialized specification and a script-free native fallback.</summary>
    /// <param name="chart">The caller's chart; authored size and axis policies are restored after preparation.</param>
    /// <param name="minimumWidth">Width below which the container scrolls.</param>
    /// <param name="maximumWidth">Maximum drawing width, at most 8192 pixels.</param>
    /// <param name="height">Fixed drawing height; defaults to the authored height.</param>
    /// <param name="idScope">Optional stable SVG identifier scope.</param>
    /// <param name="compressLayouts">Compresses the serialized specification; the static fallback remains available.</param>
    /// <param name="printWidth">Width of the native script-free/print fallback; defaults to the authored width within the bounds.</param>
    /// <returns>A self-contained fragment with an offline debounced resize and print lifecycle.</returns>
    public string RenderFragment(Chart chart, int minimumWidth = 480, int maximumWidth = 1600, int? height = null,
        string? idScope = null, bool compressLayouts = false, int? printWidth = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (minimumWidth < 160 || maximumWidth < minimumWidth || maximumWidth > 8192) throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        if (height <= 0 || printWidth <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        var size = chart.Options.Size; var x = chart.Options.XAxis; var y = chart.Options.YAxis;
        var drawingHeight = height ?? size.Height;
        var fallbackWidth = printWidth ?? Math.Max(minimumWidth, Math.Min(maximumWidth, size.Width));
        try {
            var specification = HtmlChartLayoutSpecification.Serialize(chart, minimumWidth, maximumWidth, drawingHeight, fallbackWidth, idScope);
            chart.Options.XAxis = x; chart.Options.YAxis = y;
            chart.WithSize(fallbackWidth, drawingHeight);
            var fallback = idScope == null ? chart.ToSvg() : chart.ToSvg(idScope + "|fallback");
            var payload = compressLayouts ? "<script type=\"application/octet-stream\" data-cfx-spec-gzip>" + Compress(specification)
                : "<script type=\"application/json\" data-cfx-spec>" + specification;
            return "<div data-cfx-responsive-chart style=\"min-width:" + minimumWidth + "px;max-width:" + maximumWidth + "px;margin-inline:auto\">" +
                fallback + payload + "</script><script>" + Script.Value + "</script></div>";
        } finally { chart.WithSize(size.Width, size.Height); chart.Options.XAxis = x; chart.Options.YAxis = y; }
    }
    private static string Compress(string text) {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true)) {
            var bytes = Encoding.UTF8.GetBytes(text); gzip.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(buffer.ToArray());
    }
}
