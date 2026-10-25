using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>These controls use only ordinary native APIs and must run before the additional family extraction.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class RemainingValueTransitionDefaultControls {
    internal static readonly string[] Families = { "rings", "gauge-arc", "gauge-needle", "gauge-configured", "gauge-combined", "gauge-horizontal", "gauge-vertical", "bullet-legacy", "bullet-horizontal", "bullet-vertical", "bullet-shared-vertical" };
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(760, false)] [InlineData(760, true)]
    public void FiniteRingGaugeAndBothBulletOwnersRetainOrdinaryNativeAndAssetFreeOutputBytes(int width, bool dark) {
        var renderer = new HtmlInteractiveChartRenderer(); var family = BrowserProofFonts.Family;
        foreach (var kind in Families) foreach (var visibility in new[] { "default", "hidden" }) {
            var chart = Model(kind, width, dark, 40, family);
            if (visibility == "hidden") foreach (var series in chart.Series) series.WithDataLabels(false);
            Capture(renderer.PrepareSection(chart), kind + "-" + visibility, width, dark);
        }
        foreach (var kind in new[] { "rings", "gauge-arc", "bullet-legacy", "bullet-horizontal" }) {
            var chart = Model(kind, width, dark, 40, family).WithValueFormat(ChartValueFormat.Custom(value => value.ToString("0.000", CultureInfo.InvariantCulture) + " units"));
            if (kind == "bullet-horizontal") chart.Series[1].ConfigureBulletAxis(axis => axis.WithValueFormat(ChartValueFormat.Custom(value => value.ToString("0.000", CultureInfo.InvariantCulture) + " tasks")));
            Capture(renderer.PrepareSection(chart), kind + "-callback", width, dark);
        }
        foreach (var kind in new[] { "bullet-legacy", "bullet-horizontal" }) {
            var chart = Model(kind, width, dark, 40, family); chart.Series[1].WithPointLabel(0, "Accepted count");
            Capture(renderer.PrepareSection(chart), kind + "-authored", width, dark);
        }
    }
    private static void Capture(HtmlChartSection section, string sample, int width, bool dark) {
        Assert.DoesNotContain("native-motion", section.Html); Assert.DoesNotContain("update-frame", section.Html);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "remaining-value-default-" + sample + "-" + width + "-" + dark);
        File.WriteAllText(stem + ".html", section.Html); File.WriteAllText(stem + ".svg", section.PreparedVisual.ToSvg()); File.WriteAllBytes(stem + ".png", section.PreparedVisual.ToPng());
    }
    internal static Chart Model(string kind, int width, bool dark, double reading, string family, bool identified = true) {
        var chart = Chart.Create().WithSize(width, 440).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithFontFamily(family).WithLegend(false).WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture));
        if (kind == "rings") {
            chart.WithXLabels("Received", "Verified", "Complete").WithProgressRingCenterLabel();
            var series = new ChartSeries(new ChartSeriesId("completion"), "Completion", ChartSeriesKind.ProgressRing,
                new[] { new ChartPoint(0, 25), new ChartPoint(1, reading), new ChartPoint(2, 85) });
            if (identified) for (var index = 0; index < 3; index++) series.WithObservationId(index, new ChartObservationId("stage-" + index));
            chart.AddSeries(series); return chart;
        }
        if (kind.StartsWith("gauge-", StringComparison.Ordinal)) {
            var series = ChartSeries.CreateGauge(new ChartSeriesId("capacity"), "Capacity",
                new ChartGaugeValue(reading, targets: new[] { new ChartGaugeTarget(75, "Goal") }));
            if (identified) series.WithObservationId(0, new ChartObservationId("current"));
            chart.AddSeries(series);
            chart.ConfigureGauge(options => {
                options.Form = kind == "gauge-needle" ? ChartGaugeForm.Needle : kind == "gauge-combined" ? ChartGaugeForm.ArcWithNeedle
                    : kind is "gauge-horizontal" or "gauge-vertical" ? ChartGaugeForm.Linear : ChartGaugeForm.Arc;
                options.Orientation = kind == "gauge-vertical" ? ChartOrientation.Vertical : ChartOrientation.Horizontal;
                if (kind is "gauge-configured" or "gauge-combined") { options.StartAngleDegrees = 180; options.SweepAngleDegrees = 180; options.TickCount = 5; options.BandGap = 3; }
                options.Caption = "Available capacity";
                options.Bands.Add(new ChartGaugeBand(0, 50, ChartSeriesState.Danger));
                options.Bands.Add(new ChartGaugeBand(50, 80, ChartSeriesState.Warning));
                options.Bands.Add(new ChartGaugeBand(80, 100, ChartSeriesState.Success));
            });
            return chart;
        }
        var first = ChartSeries.CreateBullet(new ChartSeriesId("reviewed"), "Reviewed", new ChartBulletValue(35, 75, rangeEnds: new[] { 50d, 80 }));
        var second = ChartSeries.CreateBullet(new ChartSeriesId("verified"), "Verified", new ChartBulletValue(reading, 75, rangeEnds: new[] { 50d, 80 }));
        if (identified) { first.WithObservationId(0, new ChartObservationId("current")); second.WithObservationId(0, new ChartObservationId("current")); }
        chart.AddSeries(first).AddSeries(second).ConfigureBullet(options => {
            options.IndependentAxes = kind is "bullet-horizontal" or "bullet-vertical";
            options.Orientation = kind is "bullet-vertical" or "bullet-shared-vertical" ? ChartOrientation.Vertical : ChartOrientation.Horizontal;
        });
        if (kind is "bullet-horizontal" or "bullet-vertical") {
            first.ConfigureBulletAxis(axis => { axis.TickCount = 3; axis.WithValueFormat(ChartValueFormat.Number("0.0' items'")); });
            second.ConfigureBulletAxis(axis => { axis.Reversed = true; axis.TickCount = 3; axis.WithValueFormat(ChartValueFormat.Number("0.00' tasks'")); });
        }
        return chart;
    }
}
