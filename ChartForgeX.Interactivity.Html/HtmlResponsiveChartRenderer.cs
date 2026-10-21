using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Prepares native SVG geometry for offline container resizing without scaling text or strokes.</summary>
/// <remarks>
/// Native preparations at bounded width bands choose labels and ticks. Additional native preparations validate
/// coordinate interpolation and refine bands at layout transitions. The browser applies these coordinates at its actual width; it does not
/// transform the drawing or run another axis engine. Height, typography, paint and stroke widths remain authored.
/// </remarks>
public sealed class HtmlResponsiveChartRenderer {
    private static readonly Regex Number = new(@"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?", RegexOptions.CultureInvariant);
    private static readonly string[] Geometry = { "x", "y", "x1", "y1", "x2", "y2", "cx", "cy", "r", "rx", "ry", "width", "height", "d", "points", "transform" };
    private static readonly Lazy<string> Script = new(() => {
        using var stream = typeof(HtmlResponsiveChartRenderer).Assembly.GetManifestResourceStream("ChartForgeX.Interactivity.Html.Assets.responsive-chart.js")
            ?? throw new InvalidOperationException("The responsive chart asset is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
    /// <summary>The shared initializer for hosts that insert fragments through inert templates or compressed panels.</summary>
    public static string InitializationScript => Script.Value;

    /// <summary>Renders an offline fragment that lays out at its container width, bounded by the minimum and cap.</summary>
    /// <param name="chart">A chart owned by the caller. Its dimensions are restored and authored tick settings are retained.</param>
    /// <param name="minimumWidth">Below this width the host scrolls. Use 240 for phone layouts.</param>
    /// <param name="maximumWidth">Maximum drawing width, from the minimum through 8192 pixels.</param>
    /// <param name="height">Fixed drawing height, or the chart's configured height.</param>
    /// <param name="idScope">Optional stable scope for SVG definition identifiers.</param>
    /// <param name="compressLayouts">Compresses the native layout bands for browsers supporting DecompressionStream. The fallback remains visible.</param>
    /// <returns>A fragment with a native SVG fallback, native layout bands and a ResizeObserver.</returns>
    public string RenderFragment(Chart chart, int minimumWidth = 480, int maximumWidth = 1600, int? height = null, string? idScope = null, bool compressLayouts = false) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (minimumWidth < 160 || maximumWidth < minimumWidth || maximumWidth > 8192) throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        var size = chart.Options.Size;
        var axis = chart.Options.XAxis;
        var scopeBase = idScope ?? chart.ToSvg();
        var drawingHeight = height ?? size.Height;
        var frames = new StringBuilder();
        try {
            var cache = new SortedDictionary<int, XElement>();
            XElement Native(int width) {
                if (cache.TryGetValue(width, out var found)) return found;
                if (cache.Count >= 2048) throw new InvalidOperationException("The chart needs too many native layout transitions for an offline responsive drawing. Use a fixed drawing.");
                chart.Options.XAxis = axis.Clone();
                if (axis.HasExplicitTickCount) chart.Options.XAxis.TickCount = Math.Min(axis.TickCount, Math.Max(2, (width - 80) / 120));
                var options = new VisualSvgOptions(VisualSvgOptions.NamespaceFromExternalId(scopeBase + "|layout"), chart.Options.SvgColorVariables).WithPatternTiles();
                var drawing = XElement.Parse(chart.WithSize(width, drawingHeight).Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg(options));
                NormalizeIds(drawing, options.IdPrefix!);
                cache.Add(width, drawing); return drawing;
            }
            // Refine only where native geometry or label decisions change. Never extrapolate a native derivative.
            var widths = new SortedSet<int> { minimumWidth, maximumWidth, Math.Max(minimumWidth, Math.Min(maximumWidth, size.Width)) };
            if (minimumWidth <= 320 && maximumWidth >= 320) widths.Add(320);
            for (var width = minimumWidth; width < maximumWidth; width += 160) widths.Add(width);
            void Refine(int left, int right) {
                if (right - left <= 1) return;
                var a = Native(left); var b = Native(right);
                var samples = new[] { left + (right - left) / 4, left + (right - left) / 2, left + 3 * (right - left) / 4 }.Distinct().Where(w => w > left && w < right);
                if (Compatible(a, b) && samples.All(w => Interpolates(a, b, Native(w), (w - left) / (double)(right - left)))) return;
                var middle = left + (right - left) / 2;
                widths.Add(middle);
                if (widths.Count > 512) throw new InvalidOperationException("Chart '" + chart.Accessibility.Name + "' (" + (idScope ?? "unscoped") + ") needs too many native layout bands near " + left + "–" + right + " pixels. Use a fixed drawing.");
                Refine(left, middle); Refine(middle, right);
            }
            var seeds = widths.ToArray();
            for (var i = 0; i + 1 < seeds.Length; i++) Refine(seeds[i], seeds[i + 1]);
            var prepared = widths.ToArray();
            for (var i = 0; i < prepared.Length; i++) {
                var width = prepared[i];
                if (frames.Length > 0) frames.Append(',');
                var native = Native(width);
                var next = i + 1 < prepared.Length ? Native(prepared[i + 1]) : native;
                var compatible = Compatible(native, next);
                var changes = compatible ? Coordinates(native, next, i + 1 < prepared.Length ? prepared[i + 1] - width : 1) : "[]";
                // The first member remains a complete native drawing, also usable without script.
                frames.Append("{\"width\":").Append(width).Append(",\"svg\":")
                    .Append(HtmlJsonString.Encode(native.ToString(SaveOptions.DisableFormatting))).Append(",\"interpolate\":").Append(compatible ? "true" : "false")
                    .Append(",\"coordinates\":").Append(changes).Append('}');
            }
            chart.WithSize(Math.Max(minimumWidth, Math.Min(maximumWidth, size.Width)), drawingHeight);
            chart.Options.XAxis = axis;
            var fallback = chart.ToSvg(scopeBase + "|fallback");
            var payload = "[" + frames + "]";
            var data = compressLayouts ? "<script type=\"application/octet-stream\" data-cfx-layouts-gzip>" + Compress(payload)
                : "<script type=\"application/json\" data-cfx-layouts>" + payload;
            return "<div data-cfx-responsive-chart style=\"min-width:" + minimumWidth + "px;max-width:" + maximumWidth + "px;margin-inline:auto\">" +
                fallback + data + "</script><script>" + Script.Value + "</script></div>";
        } finally {
            chart.WithSize(size.Width, size.Height);
            chart.Options.XAxis = axis;
        }
    }

    private static void NormalizeIds(XElement drawing, string prefix) {
        var ids = drawing.Descendants().Attributes("id").Select((attribute, index) => new { Original = attribute.Value, Alias = prefix + "-layout-" + index.ToString(CultureInfo.InvariantCulture) })
            .ToDictionary(item => item.Original, item => item.Alias, StringComparer.Ordinal);
        foreach (var attribute in drawing.DescendantsAndSelf().Attributes()) {
            if (attribute.Name.LocalName == "id" && ids.TryGetValue(attribute.Value, out var id)) attribute.Value = id;
            else if ((attribute.Name.LocalName == "href") && attribute.Value.StartsWith("#", StringComparison.Ordinal) && ids.TryGetValue(attribute.Value.Substring(1), out var target)) attribute.Value = "#" + target;
            else if (attribute.Name.LocalName == "aria-labelledby" || attribute.Name.LocalName == "aria-describedby") attribute.Value = string.Join(" ", attribute.Value.Split(' ').Select(value => ids.TryGetValue(value, out var alias) ? alias : value));
            else if (attribute.Value.Contains("url(#")) foreach (var item in ids) attribute.Value = attribute.Value.Replace("url(#" + item.Key + ")", "url(#" + item.Value + ")");
        }
    }

    private static string Compress(string text) {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true)) {
            var bytes = Encoding.UTF8.GetBytes(text); gzip.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(buffer.ToArray());
    }

    private static bool Compatible(XElement native, XElement adjacent) {
        var a = native.Descendants().ToArray();
        var b = adjacent.Descendants().ToArray();
        if (a.Length != b.Length || a.Where((element, i) => element.Name != b[i].Name).Any())
            return false;
        for (var i = 0; i < a.Length; i++) {
            if (a[i].Value != b[i].Value) return false;
            var x = a[i].Attributes().ToArray(); var y = b[i].Attributes().ToArray();
            if (x.Length != y.Length) return false;
            foreach (var attribute in x) {
                var other = b[i].Attribute(attribute.Name);
                if (other == null) return false;
                if (!Geometry.Contains(attribute.Name.LocalName) && attribute.Value != other.Value) return false;
                if (Geometry.Contains(attribute.Name.LocalName) && Number.Replace(attribute.Value, "#") != Number.Replace(other.Value, "#")) return false;
            }
        }
        return true;
    }

    private static bool Interpolates(XElement left, XElement right, XElement sample, double fraction) {
        if (!Compatible(left, sample)) return false;
        var a = left.Descendants().ToArray(); var b = right.Descendants().ToArray(); var c = sample.Descendants().ToArray();
        for (var i = 0; i < a.Length; i++) foreach (var name in Geometry) {
            var value = (string?)a[i].Attribute(name);
            if (value == null) continue;
            var x = Number.Matches(value); var y = Number.Matches((string)b[i].Attribute(name)!); var z = Number.Matches((string)c[i].Attribute(name)!);
            // Collision-avoiding annotation plates move in native placement increments. A small
            // placement tolerance avoids encoding that jitter without relaxing plotted geometry.
            var annotationLabel = a[i].AncestorsAndSelf().Any(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("annotation-label", StringComparison.Ordinal) == true);
            var tolerance = annotationLabel ? 2 : .5;
            for (var n = 0; n < x.Count; n++) {
                var expected = Numeric(x[n]) + (Numeric(y[n]) - Numeric(x[n])) * fraction;
                if (Math.Abs(expected - Numeric(z[n])) > tolerance) return false;
            }
        }
        return true;
    }

    private static double Numeric(Match value) => double.Parse(value.Value, CultureInfo.InvariantCulture);

    private static string Coordinates(XElement native, XElement adjacent, int span) {
        var a = native.Descendants().ToArray(); var b = adjacent.Descendants().ToArray();
        var output = new StringBuilder("[");
        for (var i = 0; i < a.Length; i++) foreach (var name in Geometry) {
            var from = (string?)a[i].Attribute(name);
            var to = (string?)b[i].Attribute(name);
            if (from == null || to == null || from == to) continue;
            var x = Number.Matches(from); var y = Number.Matches(to);
            if (output.Length > 1) output.Append(',');
            output.Append('[').Append(i).Append(',').Append(HtmlJsonString.Encode(name)).Append(',').Append(HtmlJsonString.Encode(from)).Append(",[");
            for (var n = 0; n < x.Count; n++) {
                if (n > 0) output.Append(',');
                var delta = (Numeric(y[n]) - Numeric(x[n])) / span;
                output.Append(delta.ToString("0.########", CultureInfo.InvariantCulture));
            }
            output.Append("]]");
        }
        return output.Append(']').ToString();
    }
}
