using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class CircleValueTransitionDefaultControls {
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)]
    [InlineData(760, false)] [InlineData(760, true)]
    public void ExistingCirclePaintTypographyVisibilityAndCustomFormatPoliciesKeepOrdinaryNativeAndAssetFreeOutputs(int width, bool dark) {
        var renderer = new HtmlInteractiveChartRenderer();
        foreach (var variant in new[] { "paint", "label", "status-off", "labels-off" }) {
            var chart = CircleRecipe.Create().WithSize(width, 400).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                .WithFontFamily(BrowserProofFonts.Family);
            var series = chart.Series[0];
            if (variant == "paint") {
                series.StateRole = ChartSeriesState.Info; series.WithColor(ChartColor.FromHex("#2468AC")).WithPointColor(0, ChartColor.FromHex("#7B3294"));
                series.UpdateGaugeValue(0, new ChartGaugeValue(120));
            } else if (variant == "label") {
                series.WithPointLabel(0, "Completed assessment with complete retained context");
                series.ConfigurePointDataLabelStyle(0, style => style.WithUnderline(TextDecorationStyle.Dashed));
                chart.Options.CircleRadiusScale = 1.25; chart.Options.CircleStrokeScale = 1.65;
            } else if (variant == "status-off") {
                chart.Options.ShowCircleStatusLabel = false; series.UpdateGaugeValue(0, new ChartGaugeValue(-20));
                chart.WithValueFormat(ChartValueFormat.Number("0.0", CultureInfo.InvariantCulture));
            } else {
                series.ShowDataLabels = false; chart.WithValueFormat(ChartValueFormat.Custom(value => value.ToString(CultureInfo.InvariantCulture) + " units"));
            }
            var section = renderer.PrepareSection(chart);
            Assert.DoesNotContain("native-motion", section.Html); Assert.DoesNotContain("update-frame", section.Html);
            var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) continue;
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "circle-value-default-" + variant + "-" + width + "-" + dark);
            File.WriteAllText(stem + ".html", section.Html); File.WriteAllText(stem + ".svg", section.PreparedVisual.ToSvg());
            File.WriteAllBytes(stem + ".png", section.PreparedVisual.ToPng());
        }
    }
}
