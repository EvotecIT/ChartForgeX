using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DataLabelVisibilityInheritanceTests {
    [Theory]
    [InlineData("chord")]
    [InlineData("sankey")]
    [InlineData("gauge-arc")]
    [InlineData("gauge-needle-configured")]
    [InlineData("gauge-linear-horizontal")]
    [InlineData("gauge-linear-vertical")]
    [InlineData("circle")]
    [InlineData("progress-ring")]
    [InlineData("layered-radial")]
    [InlineData("progress-bar")]
    [InlineData("bullet")]
    [InlineData("donut-center")]
    public void DefaultOnCaptionsInheritChartVisibilityAndRestoreTheChartAfterAnOverride(string kind) {
        var chart = Create(kind); var series = chart.Series[0]; var raw = Facts(chart);
        Assert.Null(chart.Options.ShowDataLabels); Assert.Null(series.ShowDataLabels);
        var baseline = Capture(chart); var baselineLabels = Labels(baseline, Roles(kind));
        Assert.NotEmpty(baselineLabels);

        chart.WithDataLabels(false);
        var inheritedHidden = Capture(chart);
        Assert.Empty(Labels(inheritedHidden, Roles(kind)));
        Assert.Equal(raw, Facts(chart));

        series.WithDataLabels(true);
        var explicitlyVisible = Capture(chart);
        Assert.Equal(baselineLabels, Labels(explicitlyVisible, Roles(kind)));
        Assert.Equal(raw, Facts(chart));

        chart.WithDataLabels(true); series.WithDataLabels(false);
        var explicitlyHidden = Capture(chart);
        Assert.Empty(Labels(explicitlyHidden, Roles(kind)));
        Assert.Equal(Geometry(explicitlyHidden), Geometry(inheritedHidden));
        Assert.Equal(AllLabels(explicitlyHidden), AllLabels(inheritedHidden));
        Assert.Equal(raw, Facts(chart));

        chart.WithDataLabels(false); series.WithDataLabels(true).UseChartDataLabels();
        Assert.Null(series.ShowDataLabels);
        var inheritedAgain = Capture(chart);
        Assert.Empty(Labels(inheritedAgain, Roles(kind)));
        Assert.Equal(Geometry(explicitlyHidden), Geometry(inheritedAgain));
        Assert.Equal(AllLabels(explicitlyHidden), AllLabels(inheritedAgain));
        Assert.Equal(raw, Facts(chart));
        if (kind == "progress-bar") {
            Assert.Equal(2, Labels(inheritedAgain, new[] { "progress-label" }).Length);
            var group = Assert.Single(inheritedAgain.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "progress-bar-chart");
            Assert.Equal("false", (string?)group.Attribute("data-cfx-show-values"));
        }
    }

    [Theory]
    [InlineData("chord")]
    [InlineData("progress-ring")]
    [InlineData("circle")]
    [InlineData("donut-center")]
    [InlineData("progress-bar")]
    public void ExplicitDataLabelsDoNotOverrideTheSeparateContentSwitch(string kind) {
        var chart = Create(kind).WithDataLabels(false); chart.Series[0].WithDataLabels(true);
        var raw = Facts(chart);
        if (kind == "chord") chart.ConfigureChord(options => options.LabelContent = ChartChordLabelContent.None);
        else if (kind == "progress-ring") chart.WithProgressRingCenterLabel(false);
        else if (kind == "circle") chart.WithCircleStatusLabel(false);
        else if (kind == "donut-center") chart.WithDonutCenterLabel(false);
        else chart.Options.ShowProgressValues = false;
        var hidden = Capture(chart);
        Assert.Empty(Labels(hidden, kind == "circle" ? new[] { "circle-status-label" } : Roles(kind)));
        if (kind == "circle") Assert.Equal(2, Labels(hidden, new[] { "circle-label", "circle-title" }).Length);
        if (kind == "progress-bar") {
            Assert.Equal(2, Labels(hidden, new[] { "progress-label" }).Length);
            var group = Assert.Single(hidden.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "progress-bar-chart");
            Assert.Equal("false", (string?)group.Attribute("data-cfx-show-values"));
        }
        Assert.Equal(raw, Facts(chart));
    }

    private static Chart Create(string kind) {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite()).WithSize(390, 300).WithHeader(false).WithLegend(false);
        var nodes = new[] { new ChartNode("A", "Alpha"), new ChartNode("B", "Bravo"), new ChartNode("C", "Charlie") };
        var links = new[] { new ChartFlowLink("AB", "A", "B", 5), new ChartFlowLink("AC", "A", "C", 3), new ChartFlowLink("BC", "B", "C", 2) };
        if (kind == "chord") chart.AddChord("Processing", nodes, links);
        else if (kind == "sankey") chart.AddSankey("Processing", nodes, links);
        else if (kind.StartsWith("gauge", StringComparison.Ordinal)) {
            var targets = kind == "gauge-needle-configured" ? new[] { new ChartGaugeTarget(20, "Low"), new ChartGaugeTarget(60, "High") }
                : new[] { new ChartGaugeTarget(60, "Target") };
            chart.AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("reading"), "Measured", new ChartGaugeValue(37, -20, 80, targets)));
            if (kind == "gauge-needle-configured") chart.ConfigureGauge(options => {
                options.Form = ChartGaugeForm.Needle; options.StartAngleDegrees = 120; options.SweepAngleDegrees = 300; options.TickCount = 5;
            });
            if (kind.StartsWith("gauge-linear", StringComparison.Ordinal)) chart.ConfigureGauge(options => {
                options.Form = ChartGaugeForm.Linear;
                options.Orientation = kind.EndsWith("vertical", StringComparison.Ordinal) ? ChartOrientation.Vertical : ChartOrientation.Horizontal;
            });
        } else if (kind == "circle") chart.AddCircle("Completion", 73);
        else if (kind == "progress-ring") chart.AddProgressRing("Completion", new[] { new ChartPoint(1, 73), new ChartPoint(2, 45) });
        else if (kind == "layered-radial") chart.AddLayeredRadial("Completion", new[] {
            new ChartRadialLayer("Reviewed", 73) { RadiusRatio = 1, StrokeRatio = .13 },
            new ChartRadialLayer("Verified", 45) { RadiusRatio = .6, StrokeRatio = .13 }
        });
        else if (kind == "progress-bar") chart.AddProgressBars("Completion", new[] { new ChartProgressItem("Reviewed", 73), new ChartProgressItem("Verified", 45) });
        else if (kind == "bullet") chart.AddBullet("Reviewed", 37, 60, -20, 80, new[] { 20d, 50, 80 });
        else if (kind == "donut-center") chart.AddDonut("Distribution", new[] { new ChartPoint(1, 25), new ChartPoint(2, 45), new ChartPoint(3, 30) }).WithXLabels("Alpha", "Bravo", "Charlie");
        else throw new ArgumentException(kind, nameof(kind));
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        return chart;
    }

    private static string[] Roles(string kind) => kind switch {
        "chord" => new[] { "chord-node-label" }, "sankey" => new[] { "sankey-node-label" },
        "circle" => new[] { "circle-label", "circle-title", "circle-status-label" },
        "progress-ring" => new[] { "progress-ring-value", "progress-ring-title" },
        "layered-radial" => new[] { "layered-radial-value", "layered-radial-title" },
        "progress-bar" => new[] { "progress-value" },
        "bullet" => new[] { "bullet-row-label", "bullet-value-label", "bullet-target-label" },
        "donut-center" => new[] { "donut-total-label", "donut-title" }, _ => new[] { "gauge-label", "gauge-title" }
    };
    private static XDocument Capture(Chart chart) => XDocument.Parse(chart.ToSvg());
    private static string Role(XElement element) => element.AncestorsAndSelf().Select(ancestor => (string?)ancestor.Attribute("data-cfx-role")).FirstOrDefault(value => value != null) ?? "";
    private static string[] Labels(XDocument svg, string[] roles) => svg.Descendants().Where(element => element.Name.LocalName == "text" && roles.Contains(Role(element)))
        .Select(element => Role(element) + ":" + element.Value).ToArray();
    private static string[] AllLabels(XDocument svg) => svg.Descendants().Where(element => element.Name.LocalName == "text")
        .Select(element => Role(element) + ":" + element.Value).ToArray();
    private static string[] Geometry(XDocument svg) => svg.Descendants().Where(element => element.Name.LocalName is "path" or "rect" or "circle" or "ellipse" or "line" or "polygon" or "polyline")
        .Select(element => element.Name.LocalName + ":" + Role(element) + ":" + string.Join(";", element.Attributes()
            .Where(attribute => attribute.Name.LocalName is "x" or "y" or "width" or "height" or "d" or "cx" or "cy" or "r" or "rx" or "ry" or "x1" or "y1" or "x2" or "y2" or "points" or "transform")
            .OrderBy(attribute => attribute.Name.LocalName, StringComparer.Ordinal).Select(attribute => attribute.Name.LocalName + "=" + attribute.Value))).ToArray();
    private static string Facts(Chart chart) => JsonSerializer.Serialize(chart.Series.Select(series => new {
        series.Name, series.Kind, series.SourcePointCount, points = series.Points.Select(point => new { point.X, point.Y }),
        series.GaugeValues, series.BulletValues, series.Nodes, series.FlowLinks,
        layers = series.RadialLayers.Select(layer => new { layer.Name, layer.Value, layer.Minimum, layer.Maximum })
    }));
}
