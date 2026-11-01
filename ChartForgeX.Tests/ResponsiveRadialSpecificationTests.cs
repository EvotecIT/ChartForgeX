using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ResponsiveRadialSpecificationTests {
    [Fact]
    public void SourceContainsEveryNativeSliceAndLegendWithoutMutatingTheCallerBudget() {
        var chart = ResponsiveRadialFixture.Model(false);
        chart.Options.LegendMaximumRows = 1;
        var html = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 480);
        using var document = JsonDocument.Parse(Regex.Match(html, "<script[^>]*data-cfx-spec>(.*?)</script>", RegexOptions.Singleline).Groups[1].Value);
        var root = document.RootElement;
        Assert.Equal("chartforgex/layout-4", root.GetProperty("schema").GetString());
        var radial = root.GetProperty("radial");
        Assert.Equal(420, radial.GetProperty("height").GetInt32());
        Assert.Equal(6, radial.GetProperty("entries").GetArrayLength());
        Assert.Equal(6, radial.GetProperty("slices").GetArrayLength());
        var svg = XDocument.Parse(Regex.Match(html, "<svg.*?</svg>", RegexOptions.Singleline).Value);
        Assert.Equal(6, svg.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == "legend-entry"));
        Assert.DoesNotContain(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-entry-omitted");
        Assert.Equal(1, chart.Options.LegendMaximumRows);
        Assert.Equal(1100, chart.Options.Size.Width); Assert.Equal(420, chart.Options.Size.Height);
    }
}
