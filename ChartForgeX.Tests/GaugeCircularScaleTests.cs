using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects bounded, uniform circular scales and equivalent default gauge anatomy.</summary>
public sealed class GaugeCircularScaleTests {
    [Theory]
    [InlineData(ChartGaugeForm.Arc, 40)]
    [InlineData(ChartGaugeForm.Needle, 40)]
    [InlineData(ChartGaugeForm.Arc, 60)]
    [InlineData(ChartGaugeForm.Needle, 60)]
    public void RegeneratedDefaultCaptionsDistinguishTicksAndRetainedTargetFacts(ChartGaugeForm form, double fontSize) {
        var chart = CreatePrecision(form, fontSize);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Regions.Where(region => region.Id.StartsWith("series-0-gauge-scale-", StringComparison.Ordinal)).ToArray();
        Assert.InRange(labels.Length, 2, 3);
        Assert.Equal(labels.Length, labels.Select(region => region.Label).Distinct(StringComparer.Ordinal).Count());
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge");
        Assert.Equal("1.01", group.Metadata["data-cfx-gauge-target-0-value"]);
        if (labels.Length == 2)
            Assert.DoesNotContain(labels, region => region.Label == group.Metadata["data-cfx-gauge-target-0-formatted-value"]);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.WithValueFormatter(_ => throw new InvalidOperationException("Prepared facts must remain captured."));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Capture(prepared, "default-precision-" + form + "-" + fontSize);
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc, 97, 360, 360, false, 360)]
    [InlineData(ChartGaugeForm.Needle, 97, 800, 440, false, 360)]
    [InlineData(ChartGaugeForm.Arc, 96, 360, 360, false, 360)]
    [InlineData(ChartGaugeForm.Needle, 9, 520, 260, true, 360)]
    [InlineData(ChartGaugeForm.Arc, 97, 520, 260, true, 240)]
    [InlineData(ChartGaugeForm.Needle, 97, 520, 260, true, 240)]
    public void GeneratedScalesCoarsenUniformlyWithinTheViewportAndCaptureEachValueOnce(ChartGaugeForm form, int requested,
        int width, int height, bool longFormat, double sweep) {
        var calls = new Dictionary<double, int>();
        var chart = Create(form, requested, width, height, longFormat, false, value => calls[value] = calls.GetValueOrDefault(value) + 1);
        chart.ConfigureGauge(options => options.SweepAngleDegrees = sweep);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "gauge-tick").ToArray();
        Assert.InRange(ticks.Length, 2, longFormat ? 2 : 40);
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-track");
        if (sweep == 360) {
            var angles = ticks.Select(tick => (Math.Atan2(tick.Start.Y - track.Cy, tick.Start.X - track.Cx) + Math.PI * 2) % (Math.PI * 2)).OrderBy(value => value).ToArray();
            for (var index = 0; index < angles.Length; index++) {
                var step = (angles[(index + 1) % angles.Length] - angles[index] + Math.PI * 2) % (Math.PI * 2);
                Assert.Equal(Math.PI * 2 / angles.Length, step, 7);
            }
            Assert.DoesNotContain(prepared.Regions, region => region.Role == "gauge-max-label");
        } else {
            Assert.Equal(2, ticks.Length);
            Assert.Contains(prepared.Regions, region => region.Role == "gauge-min-label" && region.Label?.StartsWith("0.00 ", StringComparison.Ordinal) == true);
            Assert.Contains(prepared.Regions, region => region.Role == "gauge-max-label" && region.Label?.StartsWith("100.00 ", StringComparison.Ordinal) == true);
        }
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "gauge");
        Assert.Equal("100", group.Metadata["data-cfx-max"]); Assert.Equal("50", group.Metadata["data-cfx-value"]);
        Assert.Equal("75", group.Metadata["data-cfx-gauge-target-0-value"]);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        var captured = calls.ToArray(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.WithValueFormatter(_ => throw new InvalidOperationException("A prepared scale must retain captured captions."));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(captured, calls.ToArray());
        Capture(prepared, form + "-" + requested + "-" + sweep + "-" + width);
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc, true, false)]
    [InlineData(ChartGaugeForm.Needle, true, true)]
    [InlineData(ChartGaugeForm.Arc, false, true)]
    [InlineData(ChartGaugeForm.Needle, false, false)]
    public void EquivalentDefaultAnglesAndTickCountsKeepTheSameExportedGeometry(ChartGaugeForm form, bool compact, bool dark) {
        Chart Baseline() => Chart.Create().WithSize(compact ? 360 : 800, compact ? 360 : 440).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
                new ChartGaugeValue(50, targets: new[] { new ChartGaugeTarget(75, "Goal") })))
            .ConfigureGauge(options => options.Form = form);
        var baselineChart = Baseline();
        var baseline = baselineChart.Prepare(VisualExportRequest.ForChart(baselineChart).Context);
        var svg = baseline.ToSvg("same"); var png = baseline.ToPng();
        foreach (var setting in new (double Start, int? Count)[] { (150, 2), (510, null), (-210, null), (-210, 2) }) {
            var chart = Baseline().ConfigureGauge(options => { options.StartAngleDegrees = setting.Start; options.TickCount = setting.Count; });
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            Assert.Equal(setting.Start, chart.Options.Gauge.StartAngleDegrees); Assert.Equal(setting.Count, chart.Options.Gauge.TickCount);
            Assert.Equal(svg, prepared.ToSvg("same")); Assert.Equal(png, prepared.ToPng());
            Assert.Equal(baseline.Regions.Select(region => (region.Role, region.Label)), prepared.Regions.Select(region => (region.Role, region.Label)));
            foreach (var pair in baseline.Regions.Zip(prepared.Regions, (left, right) => (Left: left.Bounds, Right: right.Bounds))) {
                Assert.Equal(pair.Left.X, pair.Right.X, 7); Assert.Equal(pair.Left.Y, pair.Right.Y, 7);
                Assert.Equal(pair.Left.Width, pair.Right.Width, 7); Assert.Equal(pair.Left.Height, pair.Right.Height, 7);
            }
        }
        Capture(baseline, "equivalent-default-" + form + "-" + (compact ? "compact" : "wide") + "-" + (dark ? "dark" : "light"));
    }

    [Fact]
    public void AuthoredCircularLabelsAndDifferentGeneratedCountsRetainTheirLocations() {
        var chart = Chart.Create().WithSize(800, 440).WithLegend(false).AddGauge("Capacity", 50)
            .ConfigureGauge(options => { options.TickCount = 2; options.TickLabels = new[] { new ChartAxisLabel(50, "Midpoint") }; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "gauge-track");
        var tick = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gauge-tick");
        Assert.Equal(track.Cx, tick.Start.X, 7); Assert.True(tick.Start.Y < track.Cy);
        Assert.Contains(prepared.Regions, region => region.Label == "Midpoint");
        chart.ConfigureGauge(options => { options.TickLabels = Array.Empty<ChartAxisLabel>(); options.TickCount = 3; });
        Assert.Equal(3, chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.Count(node => node.Role == "gauge-tick"));
    }

    internal static Chart Create(ChartGaugeForm form, int requested, int width, int height, bool longFormat, bool dark, Action<double>? observe = null) =>
        Chart.Create().WithSize(width, height).WithHeader(false).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
                new ChartGaugeValue(50, targets: new[] { new ChartGaugeTarget(75, "Goal") })))
            .WithValueFormatter(value => { observe?.Invoke(value); return value.ToString(longFormat ? "0.00" : "0.0", CultureInfo.InvariantCulture)
                + (longFormat ? " calibrated capacity measurement units" : " u"); })
            .ConfigureGauge(options => { options.Form = form; options.StartAngleDegrees = -90; options.SweepAngleDegrees = 360; options.TickCount = requested; });

    internal static Chart CreatePrecision(ChartGaugeForm form, double fontSize) {
        var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
            new ChartGaugeValue(1.011, 1, 1.015, new[] { new ChartGaugeTarget(1.01) }));
        series.ShowDataLabels = false;
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        // Match the browser fixture's room for an authored 60 px face rather than relying on host fallback metrics.
        var chart = Chart.Create().WithSize(520, fontSize >= 60 ? 360 : 260).WithPngFont(font).WithHeader(false).WithLegend(false).AddSeries(series)
            .ConfigureGauge(options => { options.Form = form; options.StartAngleDegrees = -90; options.SweepAngleDegrees = 360; options.TickCount = 97; });
        chart.Options.TickLabelStyle.FontSize = fontSize;
        return chart;
    }

    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_GAUGE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }
}
