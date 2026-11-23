using System;
using ChartForgeX.Html;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Serializes a native chart layout specification for offline browser layout at arbitrary container widths.</summary>
/// <remarks>Native preparation owns data, semantic axes and paint. The browser measures text and projects the serialized instructions without scaling typography or strokes.</remarks>
public sealed class HtmlResponsiveChartRenderer {
    private static readonly Lazy<string> LayoutScript = new(() => {
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
    public static string Script => RadialScript.Value + LayoutScript.Value;
    /// <summary>Print sizing shared by complete fragments and hosts that install assets separately.</summary>
    /// <remarks>This is style-only CSS. Asset-free hosts also register <see cref="ChartForgeX.Typography.ChartFontAssets.StyleSheet"/>
    /// once when using bundled fonts, unless the host already supplies them. Explicitly registered families require matching host-provided browser CSS.</remarks>
    public static string StyleSheet => "@media print{[data-cfx-responsive-chart]{min-width:0!important}}";

    /// <summary>Gets or sets the content security policy nonce. The fragment's script element carries it, and the fragment
    /// has no <c>style</c> attributes: every inline style and style element moves into one <c>style</c> element with this
    /// nonce (see <see cref="HtmlStrictStyles"/>).</summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// Returns the scoped stylesheet that lets a <see cref="ThemeMode.Auto"/> chart follow the viewer's colour scheme, or
    /// null when the chart has an explicit mode or its own SVG colour variables.
    /// </summary>
    /// <remarks><see cref="RenderFragment"/> includes it. Hosts of <see cref="RenderFragmentWithoutAssets"/> register it
    /// once per distinct theme, next to <see cref="StyleSheet"/>; the fragment root carries the class it targets.</remarks>
    /// <param name="chart">The chart the fragment is rendered from.</param>
    /// <returns>Plain CSS, or null.</returns>
    public static string? AutoThemeStyleSheet(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return AutoTheme(chart)?.StyleSheet;
    }

    private static AutoThemeStyle? AutoTheme(Chart chart) =>
        AutoThemeStyle.ForHtml(chart.Options.Theme, chart.Options.ThemeMode, chart.Options.SvgColorVariables);

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
        HtmlStrictStyles.Apply(Render(chart, minimumWidth, maximumWidth, height, idScope, compressLayouts, printWidth, true, TrimmedNonce), TrimmedNonce);

    /// <summary>Renders a script-free host fragment with inert layout data for composition inside an interactive section.</summary>
    /// <remarks>Install <see cref="StyleSheet"/>, <see cref="Script"/> and matching fonts outside the section.
    /// Register <see cref="ChartForgeX.Typography.ChartFontAssets.StyleSheet"/> once for bundled fonts unless the host already supplies them.
    /// Source drawing, layout data and slice identities match <see cref="RenderFragment"/>.</remarks>
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
        HtmlStrictStyles.Apply(Render(chart, minimumWidth, maximumWidth, height, idScope, compressLayouts, printWidth, false, null), TrimmedNonce);

    private string? TrimmedNonce => string.IsNullOrWhiteSpace(Nonce) ? null : Nonce!.Trim();

    private static string Render(Chart chart, int minimumWidth, int maximumWidth, int? height,
        string? idScope, bool compressLayouts, int? printWidth, bool assets, string? scriptNonce) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (minimumWidth < 160 || maximumWidth < minimumWidth || maximumWidth > 8192) throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        if (height <= 0 || printWidth <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        var size = chart.Options.Size; var x = chart.Options.XAxis; var y = chart.Options.YAxis; var window = chart.Options.Heatmap.ColumnWindow;
        var drawingHeight = height ?? size.Height;
        var fallbackWidth = printWidth ?? Math.Max(minimumWidth, Math.Min(maximumWidth, size.Width));
        var auto = AutoTheme(chart);
        try {
            // The serialized drawing writes the Auto colours as scoped properties; the caller's options are restored below.
            if (auto != null) chart.Options.SvgColorVariables = auto.Variables;
            var specification = HtmlChartLayoutSpecification.Serialize(chart, minimumWidth, maximumWidth, drawingHeight, fallbackWidth, idScope, out var fallback);
            var data = compressLayouts ? Compress(specification) : specification;
            var attribute = compressLayouts ? "data-cfx-spec-gzip" : "data-cfx-spec";
            var payload = assets ? "<script type=\"" + (compressLayouts ? "application/octet-stream" : "application/json") + "\" " + attribute + ">" + data + "</script>"
                : "<span hidden " + attribute + ">" + WebUtility.HtmlEncode(data) + "</span>";
            var runtime = HtmlRadialLayoutSpecification.Supports(chart) ? RadialScript.Value + LayoutScript.Value : LayoutScript.Value;
            return "<div data-cfx-responsive-chart" + auto?.RootAttributes + " style=\"min-width:" + minimumWidth + "px;max-width:" + maximumWidth + "px;margin-inline:auto\">" +
                (assets ? "<style>" + StyleSheet + "</style>" + auto?.StyleElement : "") + fallback + payload + (assets ? (scriptNonce == null ? "<script>" : "<script nonce=\"" + WebUtility.HtmlEncode(scriptNonce) + "\">") + runtime + "</script>" : "") + "</div>";
        } finally { if (auto != null) chart.Options.SvgColorVariables = null; chart.Size(size.Width, size.Height); chart.Options.XAxis = x; chart.Options.YAxis = y; chart.Options.Heatmap.ColumnWindow = window; }
    }
    private static string Compress(string text) {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true)) {
            var bytes = Encoding.UTF8.GetBytes(text); gzip.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(buffer.ToArray());
    }
}
