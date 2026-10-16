using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects configured gauge projections, reference facts and the detached native export.</summary>
public sealed class GaugeConfigurationRenderingTests {
    public static IEnumerable<object[]> Layouts() {
        foreach (var layout in new[] { "default", "semicircle", "needle", "full-circle", "horizontal", "vertical" })
            foreach (var dark in new[] { false, true })
                foreach (var compact in new[] { false, true }) yield return new object[] { layout, dark, compact };
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void ConfiguredLayoutsShareTheReadingDomainAndExportPairedScenes(string layout, bool dark, bool compact) {
        var chart = Create(layout, dark, compact);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge");
        Assert.Equal("-20", group.Metadata["data-cfx-min"]); Assert.Equal("80", group.Metadata["data-cfx-max"]);
        Assert.Equal("30", group.Metadata["data-cfx-value"]); Assert.Equal("0.5", group.Metadata["data-cfx-percent"]);
        Assert.Equal("2", group.Metadata["data-cfx-gauge-target-count"]);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "gauge-target"));
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "gauge-reference"));
        Assert.Equal(1, chart.Series[0].ObservationCount);
        if (layout is "horizontal" or "vertical") {
            var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-track");
            var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-value");
            var targets = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-target").ToArray();
            if (layout == "vertical") {
                Assert.Equal(track.Bounds.Height / 2, value.Bounds.Height, 7);
                Assert.Equal(track.Bounds.Bottom - track.Bounds.Height * .8, targets[1].Start.Y, 7);
                Assert.True(targets[1].Start.Y < targets[0].Start.Y);
            } else {
                Assert.Equal(track.Bounds.Width / 2, value.Bounds.Width, 7);
                Assert.Equal(track.Bounds.Left + track.Bounds.Width * .8, targets[1].Start.X, 7);
            }
        } else {
            var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-track");
            var expectedSweep = (layout == "semicircle" ? 180 : layout == "full-circle" ? 360 : layout == "needle" ? 210 : 240) * Math.PI / 180;
            Assert.Equal(expectedSweep, track.Sweep, 9);
            if (layout == "needle") {
                var needle = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gauge-needle");
                Assert.Equal(Math.Cos((20 + 210 * .5) * Math.PI / 180),
                    (needle.End.X - needle.Start.X) / Math.Sqrt(Math.Pow(needle.End.X - needle.Start.X, 2) + Math.Pow(needle.End.Y - needle.Start.Y, 2)), 7);
                var summary = Assert.Single(prepared.Regions, region => region.Role == "gauge-label");
                Assert.True(summary.Bounds.Top > Math.Max(needle.Start.Y, needle.End.Y));
            } else {
                var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-value");
                Assert.Equal(track.Sweep / 2, value.Sweep, 9);
            }
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "gauge.insufficient-space");
        if (layout is "semicircle" or "needle" or "full-circle" or "vertical") {
            var ticks = prepared.Scene.Nodes.Count(node => node.Role == "gauge-tick");
            var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Count(node => node.Role is "gauge-min-label" or "gauge-max-label"
                || node.Role?.StartsWith("gauge-tick-label-", StringComparison.Ordinal) == true);
            Assert.Equal(ticks, labels);
        }
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var document = XDocument.Parse(svg); Assert.Contains(document.Descendants(), element => (string?)element.Attribute("data-cfx-gauge-target-1-value") == "60");
        Assert.True(png.Length > 64);
        Capture(prepared, layout + "-" + (compact ? "compact" : "wide") + "-" + (dark ? "dark" : "light"));
    }

    [Fact]
    public void FullTurnGeneratedTicksHaveDistinctPositionsAndKeepTheAuthoredMaximum() {
        var chart = Chart.Create().WithSize(800, 440).WithLegend(false).AddGauge("Capacity", 100)
            .ConfigureGauge(options => { options.SweepAngleDegrees = 360; options.TickCount = 6; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-tick").ToArray();
        Assert.Equal(6, ticks.Length);
        Assert.Equal(ticks.Length, ticks.Select(tick => (Math.Round(tick.Start.X, 6), Math.Round(tick.Start.Y, 6))).Distinct().Count());
        Assert.Equal("100", Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge").Metadata["data-cfx-max"]);
        Assert.DoesNotContain(prepared.Regions.Where(region => region.Role is "gauge-min-label" or "gauge-max-label" || region.Role.StartsWith("gauge-tick-label-", StringComparison.Ordinal)), region => region.Label == "100");
    }

    [Fact]
    public void ExplicitFullTurnEndpointsShareOnePaintedSeamWhileBothCaptionsRemainSemantic() {
        var chart = Chart.Create().WithSize(640, 440).WithLegend(false).AddGauge("Capacity", 50)
            .ConfigureGauge(options => { options.StartAngleDegrees = -90; options.SweepAngleDegrees = 360;
                options.TickLabels = new[] { new ChartAxisLabel(0, "Empty"), new ChartAxisLabel(50, "Half"), new ChartAxisLabel(100, "Full") }; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "gauge-tick"));
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "gauge-min-label" or "gauge-max-label"
            || node.Role?.StartsWith("gauge-tick-label-", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(2, labels.Length);
        Assert.Contains(prepared.Regions, region => region.Role == "gauge-max-label" && region.Label == "Full");
        Assert.Equal("100", Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge").Metadata["data-cfx-max"]);
    }

    [Fact]
    public void ExplicitTickLabelsUseTheirValuesAndLeaveTheReadingDomainUnchanged() {
        var seen = new List<double>();
        var chart = Chart.Create().WithSize(800, 440).WithLegend(false)
            .AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity", new ChartGaugeValue(30, -20, 80)))
            .WithValueFormatter(number => { seen.Add(number); return "Value " + number.ToString(CultureInfo.InvariantCulture); })
            .ConfigureGauge(options => { options.Form = ChartGaugeForm.Linear; options.TickCount = 100;
                options.TickLabels = new[] { new ChartAxisLabel(80, "Full"), new ChartAxisLabel(-20, "Empty"), new ChartAxisLabel(30, "Half") }; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(30, Assert.Single(seen));
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-track");
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-tick").ToArray();
        Assert.Equal(new[] { track.Bounds.Left, track.Bounds.Left + track.Bounds.Width / 2, track.Bounds.Right }, ticks.Select(tick => tick.Start.X));
        Assert.Equal(new[] { "Empty", "Half", "Full" }, prepared.Regions.Where(region => region.Role is "gauge-min-label" or "gauge-max-label" || region.Role.StartsWith("gauge-tick-label-", StringComparison.Ordinal)).Select(region => region.Label));
        Assert.Equal("-20", Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge").Metadata["data-cfx-min"]);
    }

    [Fact]
    public void ReferencesCaptureEachNumericCaptionOnceAndSurviveOmissionAndLateMutation() {
        var seen = new List<double>();
        var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity", new ChartGaugeValue(30, targets: new[] {
            new ChartGaugeTarget(30, "Current baseline"), new ChartGaugeTarget(30, "Repeated reference"), new ChartGaugeTarget(120, "") }));
        var chart = Chart.Create().WithSize(240, 220).WithLegend(false).WithAxes(false).AddSeries(series)
            .WithValueFormatter(number => { seen.Add(number); return "Reading " + number.ToString(CultureInfo.InvariantCulture); })
            .ConfigureGauge(options => options.Form = ChartGaugeForm.Linear);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(new[] { 30d, 120d }, seen.OrderBy(value => value));
        var metadata = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge").Metadata;
        Assert.Equal("120", metadata["data-cfx-gauge-target-2-value"]); Assert.Equal("Reading 120", metadata["data-cfx-gauge-target-2-formatted-value"]);
        Assert.Equal("", metadata["data-cfx-gauge-target-2-label"]);
        Assert.Contains(prepared.Regions, region => region.Role == "gauge-reference" && region.Label == "Reading 120");
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "gauge-reference"));
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "gauge-track");
        var marks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-target").ToArray();
        Assert.Equal(track.Bounds.Right, marks[2].Start.X, 7);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "gauge.label-overflow");
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.UpdateGaugeValue(0, new ChartGaugeValue(70)); chart.WithValueFormatter(_ => throw new InvalidOperationException());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(2, seen.Count);
    }

    [Theory]
    [InlineData(ChartOrientation.Horizontal)]
    [InlineData(ChartOrientation.Vertical)]
    public void CrowdedGeneratedScalesChooseUniformCoarserTicks(ChartOrientation orientation) {
        var chart = Chart.Create().WithSize(360, 360).WithLegend(false).AddLinearGauge("Capacity", 50)
            .ConfigureGauge(options => { options.Orientation = orientation; options.TickCount = 100; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-tick").ToArray();
        Assert.InRange(ticks.Length, 2, 20);
        var coordinates = ticks.Select(tick => orientation == ChartOrientation.Horizontal ? tick.Start.X : -tick.Start.Y).ToArray();
        var steps = coordinates.Zip(coordinates.Skip(1), (left, right) => right - left).ToArray();
        Assert.All(steps, step => Assert.Equal(steps[0], step, 7));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
    }

    internal static Chart Create(string layout, bool dark, bool compact) {
        var reading = new ChartGaugeValue(30, -20, 80, new[] { new ChartGaugeTarget(10, "Baseline"), new ChartGaugeTarget(60, "Goal") });
        return Chart.Create().WithSize(compact ? 360 : 800, compact ? 360 : 440).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithTitle("Available capacity")
            .AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity", reading)).ConfigureGauge(options => {
                options.Caption = "Declared range -20 to 80";
                options.Bands.Add(new ChartGaugeBand(-20, 20, ChartSeriesState.Warning)); options.Bands.Add(new ChartGaugeBand(20, 80, ChartSeriesState.Success));
                if (layout == "semicircle") { options.StartAngleDegrees = 180; options.SweepAngleDegrees = 180; options.TickCount = 5; }
                if (layout == "needle") { options.Form = ChartGaugeForm.Needle; options.StartAngleDegrees = 20; options.SweepAngleDegrees = 210; options.TickCount = 5; }
                if (layout == "full-circle") { options.StartAngleDegrees = -90; options.SweepAngleDegrees = 360; options.TickCount = 6; }
                if (layout is "horizontal" or "vertical") { options.Form = ChartGaugeForm.Linear; options.Orientation = layout == "vertical" ? ChartOrientation.Vertical : ChartOrientation.Horizontal; options.TickCount = 5; }
            });
    }

    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_GAUGE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }
}
