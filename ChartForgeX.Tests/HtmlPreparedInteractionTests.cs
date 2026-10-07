using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlPreparedInteractionTests {
    [Fact]
    public void InteractiveHostEmbedsPreparedSourceIdentitiesAndCompletedRegions() {
        const string name = "Observations <script> & \"quoted\"";
        var chart = Chart.Create().WithSize(520, 320).WithXLabels("First", "Second", "Third")
            .AddLine(name, ChartPoints.FromValues(3, 7, 5));
        chart.Series[0].WithInteractionKey("observed-values");
        var html = chart.ToInteractiveHtmlFragmentWithoutAssets();
        using var metadata = Metadata(html);
        var series = metadata.RootElement.GetProperty("series")[0];
        Assert.Equal(name, series.GetProperty("name").GetString());
        Assert.Equal("observed-values", series.GetProperty("key").GetString());
        Assert.Equal("line", series.GetProperty("kind").GetString());
        Assert.Equal(new[] { "First", "Second", "Third" }, metadata.RootElement.GetProperty("xLabels").EnumerateArray().Select(item => item.GetProperty("text").GetString()));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(prepared.Regions.Count, metadata.RootElement.GetProperty("regions").GetArrayLength());
        var svg = EmbeddedSvg(html);
        var points = svg.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(3, points.Length);
        Assert.All(points, point => Assert.NotNull(point.Attribute("data-cfx-source-id")));
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-browser-hit-area", chart.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void DecimatedInteractiveTargetsRetainOriginalPointIndicesWithoutALegend() {
        var chart = Chart.Create().WithLegend(false).AddDecimatedLine("Latency",
            Enumerable.Range(0, 100).Select(index => new ChartPoint(index, Math.Sin(index / 3d))), 12);
        chart.Series[0].WithInteractionKey("latency");
        var html = chart.ToInteractiveHtmlFragmentWithoutAssets();
        using var metadata = Metadata(html);
        Assert.Equal(chart.Series[0].SourcePointIndices, metadata.RootElement.GetProperty("series")[0].GetProperty("indices").EnumerateArray().Select(item => item.GetInt32()));
        var points = EmbeddedSvg(html).Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(chart.Series[0].Points.Count, points.Length);
        Assert.Equal(chart.Series[0].SourcePointIndices.Select(index => index.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            points.Select(point => (string?)point.Attribute("data-cfx-source-point")));
    }

    private static JsonDocument Metadata(string html) {
        var match = Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"");
        Assert.True(match.Success);
        return JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
    }

    private static XElement EmbeddedSvg(string html) {
        var start = html.IndexOf("<svg", StringComparison.Ordinal);
        var end = html.IndexOf("</svg>", start, StringComparison.Ordinal) + "</svg>".Length;
        Assert.True(start >= 0 && end > start);
        return XElement.Parse(html.Substring(start, end - start));
    }
}
