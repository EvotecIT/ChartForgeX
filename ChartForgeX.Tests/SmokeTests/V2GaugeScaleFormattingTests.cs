using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects gauge scale precision and authored formatting at the detached scene boundary.</summary>
public sealed class V2GaugeScaleFormattingTests {
    [Theory]
    [InlineData(ChartGaugeForm.Arc, 1.001, 1.005)]
    [InlineData(ChartGaugeForm.Needle, 1.001, 1.005)]
    [InlineData(ChartGaugeForm.Linear, 1.001, 1.005)]
    [InlineData(ChartGaugeForm.Arc, 1_000_001, 1_000_005)]
    [InlineData(ChartGaugeForm.Needle, 1_000_001, 1_000_005)]
    [InlineData(ChartGaugeForm.Linear, 1_000_001, 1_000_005)]
    [InlineData(ChartGaugeForm.Arc, -1.005, -1.001)]
    [InlineData(ChartGaugeForm.Needle, -1.005, -1.001)]
    [InlineData(ChartGaugeForm.Linear, -1.005, -1.001)]
    [InlineData(ChartGaugeForm.Linear, 0, 5e307)]
    [InlineData(ChartGaugeForm.Linear, 0, 1e308)]
    public void CloseBoundsKeepDistinctScaleCaptionsInPreparedSvgAndNativePng(ChartGaugeForm form, double minimum, double maximum) {
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false)
            .AddGauge("Range", (minimum + maximum) / 2, minimum, maximum)
            .WithGauge(options => options.Form = form);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Capture(prepared, "gauge-" + form.ToString().ToLowerInvariant() + "-" + minimum.ToString("R", CultureInfo.InvariantCulture) + "-" + maximum.ToString("R", CultureInfo.InvariantCulture));
        var captions = ScaleCaptions(prepared);
        Assert.Equal(form == ChartGaugeForm.Linear ? 5 : 2, captions.Length);
        Assert.Equal(captions.Length, captions.Distinct(StringComparer.Ordinal).Count());
        var displayed = captions.Select(caption => double.Parse(caption, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(minimum, displayed[0]); Assert.Equal(maximum, displayed[^1]);
        Assert.True(displayed.Zip(displayed.Skip(1), (left, right) => left < right).All(increasing => increasing));
        var svgCaptions = XDocument.Parse(prepared.ToSvg()).Descendants()
            .Where(element => IsScaleRole((string?)element.Attribute("data-cfx-role"))).Select(element => element.Value).ToArray();
        Assert.Equal(captions, svgCaptions);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc, true)]
    [InlineData(ChartGaugeForm.Needle, true)]
    [InlineData(ChartGaugeForm.Linear, true)]
    [InlineData(ChartGaugeForm.Arc, false)]
    [InlineData(ChartGaugeForm.Needle, false)]
    [InlineData(ChartGaugeForm.Linear, false)]
    public void AuthoredCallbacksResolveEachDisplayedScaleValueOnceAndDoNotRunDuringDetachedExport(ChartGaugeForm form, bool axes) {
        var calls = new List<double>();
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithAxes(axes)
            .AddGauge("Range", 1.003, 1.001, 1.005).WithGauge(options => { options.Form = form; options.Target = 1.0025; })
            .WithValueFormatter(value => { calls.Add(value); return "Value " + value.ToString("0.0000", CultureInfo.InvariantCulture); });
        chart.Series[0].WithPointLabel(0, "Authored measurement");
        Assert.Empty(calls);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var expected = !axes ? new[] { 1.0025 } : form == ChartGaugeForm.Linear
            ? new[] { 1.001, 1.002, 1.0025, 1.003, 1.004, 1.005 } : new[] { 1.001, 1.0025, 1.005 };
        Assert.Equal(expected.Length, calls.Count);
        for (var index = 0; index < expected.Length; index++) Assert.Equal(expected[index], calls.Order().ElementAt(index), 12);
        Assert.Equal(axes ? form == ChartGaugeForm.Linear ? 5 : 2 : 0, ScaleCaptions(prepared).Length);
        Assert.Contains(prepared.Regions, region => region.Role == "gauge-label" && region.Label == "Authored measurement");
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.WithValueFormatter(_ => throw new InvalidOperationException("Detached exports must not invoke a late formatter."));
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(expected.Length, calls.Count);
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc)]
    [InlineData(ChartGaugeForm.Needle)]
    [InlineData(ChartGaugeForm.Linear)]
    public void AuthoredNumericPoliciesKeepTheirDeclaredPrecision(ChartGaugeForm form) {
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false)
            .AddGauge("Range", 1.003, 1.001, 1.005).WithGauge(options => options.Form = form)
            .WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture));
        var captions = ScaleCaptions(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.Equal(form == ChartGaugeForm.Linear ? 5 : 2, captions.Length);
        Assert.Equal(form == ChartGaugeForm.Linear ? new[] { "1.00", "1.00", "1.00", "1.00", "1.01" } : new[] { "1.00", "1.01" }, captions);
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc, true)]
    [InlineData(ChartGaugeForm.Needle, true)]
    [InlineData(ChartGaugeForm.Linear, true)]
    [InlineData(ChartGaugeForm.Arc, false)]
    [InlineData(ChartGaugeForm.Needle, false)]
    [InlineData(ChartGaugeForm.Linear, false)]
    public void SharedMeasurementAndTargetResolveOnceWhileHiddenMeasurementsKeepRawSemanticFacts(ChartGaugeForm form, bool labels) {
        var calls = new List<double>(); var target = labels ? 1.003 : 1.0025;
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithAxes(false)
            .AddGauge("Tolerance", 1.003, 1.001, 1.005).WithGauge(options => { options.Form = form; options.Target = target; })
            .WithValueFormatter(number => { calls.Add(number); return "Value " + number.ToString("0.0000", CultureInfo.InvariantCulture); });
        chart.Series[0].ShowDataLabels = labels;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(target, Assert.Single(calls));
        Assert.Contains(prepared.Regions, region => region.Role == "gauge-target-label" && region.Label == "Value " + target.ToString("0.0000", CultureInfo.InvariantCulture));
        if (labels) Assert.Contains(prepared.Regions, region => region.Role == "gauge-label" && region.Label == "Value 1.0030");
        else {
            Assert.DoesNotContain(prepared.Regions, region => region.Role == "gauge-label");
            Assert.Contains(prepared.Regions, region => region.Role == "gauge" && region.Label == "Tolerance: 1.003");
        }
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc, false, 360, 360)]
    [InlineData(ChartGaugeForm.Needle, false, 360, 360)]
    [InlineData(ChartGaugeForm.Linear, false, 360, 360)]
    [InlineData(ChartGaugeForm.Arc, true, 360, 360)]
    [InlineData(ChartGaugeForm.Needle, true, 360, 360)]
    [InlineData(ChartGaugeForm.Linear, true, 360, 360)]
    [InlineData(ChartGaugeForm.Arc, false, 800, 440)]
    [InlineData(ChartGaugeForm.Needle, false, 800, 440)]
    [InlineData(ChartGaugeForm.Linear, false, 800, 440)]
    [InlineData(ChartGaugeForm.Arc, true, 800, 440)]
    [InlineData(ChartGaugeForm.Needle, true, 800, 440)]
    [InlineData(ChartGaugeForm.Linear, true, 800, 440)]
    public void DefaultGaugeCaptionsRemainReadableAndSeparateFromTheirMeasurements(ChartGaugeForm form, bool dark, int width, int height) {
        var chart = Chart.Create().WithSize(width, height).WithLegend(false).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddGauge("Tolerance", 1.003, 1.001, 1.005).WithGauge(options => { options.Form = form; options.Target = 1.0025; });
        var context = VisualExportRequest.ForChart(chart).Context; var prepared = chart.Prepare(context);
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "gauge-title");
        Assert.Equal(context.Theme.Typography.DataLabelSize, caption.Text.Size);
        Assert.Equal("Tolerance", Assert.Single(caption.Text.Lines).Text);
        var bounds = Assert.Single(prepared.Regions, region => region.Role == "gauge-title").Bounds;
        var value = Assert.Single(prepared.Regions, region => region.Role == "gauge-label").Bounds;
        Assert.True(caption.Text.Metrics.Height <= bounds.Height + .000001);
        Assert.False(bounds.Top < value.Bottom && bounds.Bottom > value.Top && bounds.Left < value.Right && bounds.Right > value.Left);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc)]
    [InlineData(ChartGaugeForm.Needle)]
    [InlineData(ChartGaugeForm.Linear)]
    public void AuthoredGaugeFontSizesRemainAuthoritativeInCompactViews(ChartGaugeForm form) {
        var chart = Chart.Create().WithSize(360, 360).WithLegend(false).AddGauge("Tolerance", 1.003, 1.001, 1.005)
            .WithGauge(options => { options.Form = form; options.Target = 1.0025; });
        chart.Series[0].DataLabelStyle.FontSize = 9;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(9, Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "gauge-title").Text.Size);
        Assert.Equal(9, Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "gauge-label").Text.Size);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublishedBandedNeedleKeepsReadableMeasurementAndCaption(bool dark) {
        var chart = Chart.Create().WithSize(396, 294).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Readiness needle").WithSubtitle("Explicit target and bands").AddGauge("Readiness", 74)
            .WithGauge(options => {
                options.Form = ChartGaugeForm.Needle; options.Target = 90;
                options.Bands.Add(new ChartGaugeBand(0, 60, ChartSeriesState.Danger));
                options.Bands.Add(new ChartGaugeBand(60, 80, ChartSeriesState.Warning));
                options.Bands.Add(new ChartGaugeBand(80, 100, ChartSeriesState.Quiet));
            });
        var context = VisualExportRequest.ForChart(chart).Context; var prepared = chart.Prepare(context);
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "gauge-label");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "gauge-title");
        Assert.True(value.Text.Size >= context.Theme.Typography.DataLabelSize);
        Assert.Equal("74", Assert.Single(value.Text.Lines).Text);
        Assert.Equal(context.Theme.Typography.DataLabelSize, caption.Text.Size);
        Assert.Equal("Readiness", Assert.Single(caption.Text.Lines).Text);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(0d, false, 360, 360)]
    [InlineData(.1, false, 360, 360)]
    [InlineData(.9, false, 360, 360)]
    [InlineData(1d, false, 360, 360)]
    [InlineData(0d, true, 360, 360)]
    [InlineData(.1, true, 360, 360)]
    [InlineData(.9, true, 360, 360)]
    [InlineData(1d, true, 360, 360)]
    [InlineData(0d, false, 800, 440)]
    [InlineData(.1, false, 800, 440)]
    [InlineData(.9, false, 800, 440)]
    [InlineData(1d, false, 800, 440)]
    [InlineData(0d, true, 800, 440)]
    [InlineData(.1, true, 800, 440)]
    [InlineData(.9, true, 800, 440)]
    [InlineData(1d, true, 800, 440)]
    public void NeedleEndpointsKeepMeasuredValueAndCaptionClearOfTheirStroke(double ratio, bool dark, int width, int height) {
        var chart = Chart.Create().WithSize(width, height).WithLegend(false).WithTitle("Range")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddGauge("Tolerance", 1_000_001 + ratio * 4, 1_000_001, 1_000_005).WithGauge(options => options.Form = ChartGaugeForm.Needle);
        var context = VisualExportRequest.ForChart(chart).Context; var prepared = chart.Prepare(context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "gauge-needle");
        var obstacle = new LabelMarkShape(new[] { new List<ChartForgeX.Primitives.ChartPoint> { line.Start, line.End } }, false, line.StrokeWidth);
        foreach (var role in new[] { "gauge-label", "gauge-title" }) {
            var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == role);
            var bounds = new ChartForgeX.Primitives.ChartRect(text.Text.Lines.Min(text.LineLeft), text.Baseline - text.Text.Ascent,
                text.Text.Metrics.Width, text.Text.Metrics.Height);
            Assert.False(obstacle.Intersects(bounds), role + " must clear the actual needle stroke.");
            Assert.True(text.Text.Size >= context.Theme.Typography.DataLabelSize);
            Assert.Equal(Assert.Single(prepared.Regions, region => region.Role == role).Label, string.Join("\n", text.Text.Lines.Select(item => item.Text)));
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Capture(prepared, "needle-clearance-" + ratio.ToString("0.0", CultureInfo.InvariantCulture) + "-" + width + "-" + (dark ? "dark" : "light"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void NeedleEndpointPreservesUnfittableAuthoredSummaryInSemanticRegions() {
        var chart = Chart.Create().WithSize(360, 360).WithLegend(false).WithTitle("Range")
            .AddGauge("Tolerance", 1_000_005, 1_000_001, 1_000_005).WithGauge(options => options.Form = ChartGaugeForm.Needle);
        chart.Series[0].DataLabelStyle.FontSize = 40;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role is "gauge-label" or "gauge-title");
        Assert.Contains(prepared.Regions, region => region.Role == "gauge-label" && region.Label == "1000005");
        Assert.Contains(prepared.Regions, region => region.Role == "gauge-title" && region.Label == "Tolerance");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    private static bool IsScaleRole(string? role) => role is "gauge-min-label" or "gauge-max-label" or "gauge-tick-label-1" or "gauge-tick-label-2" or "gauge-tick-label-3";
    private static string[] ScaleCaptions(PreparedVisual prepared) => prepared.Regions.Where(region => IsScaleRole(region.Role)).Select(region => region.Label!).ToArray();

    private static void Capture(PreparedVisual prepared, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_GAUGE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg("gauge-scale-proof"));
        File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
    }
}
