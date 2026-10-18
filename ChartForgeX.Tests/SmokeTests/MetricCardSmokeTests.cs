using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualMetricCardsRenderWithUnitsBadgesAndHistory() {

        var metric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Coverage", 0.982, "P1")
            .WithIcon(VisualIcon.Lightning)
            .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
            .WithTrend("+2.4 pp")
            .WithCaption("since previous run")
            .WithStatus(VisualStatus.Positive)
            .AddDetail("Ready", "84%", VisualStatus.Positive)
            .AddDetail("Risk", "6%", VisualStatus.Warning)
            .WithMiniBars(new[] { 72d, 78d, 83d, 80d, 98d }, maximum: 100)
            .WithTheme(VisualTheme.Report())
            .WithSize(360, 190);
        var metricSvg = metric.ToSvg("visual-block-metric");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-status-bar\"", StringComparison.Ordinal), "MetricCard should render status styling.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-detail\"", StringComparison.Ordinal), "MetricCard should render reusable supporting details.");
        Assert(metricSvg.Contains("data-cfx-role=\"visual-icon\"", StringComparison.Ordinal), "MetricCard should render reusable built-in icons.");
        Assert(metricSvg.Contains("data-cfx-placement=\"top-left\"", StringComparison.Ordinal), "MetricCard should render configurable badge placement.");
        Assert(metricSvg.Contains("since previous run", StringComparison.Ordinal), "MetricCard should preserve factual captions.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-mini-bars\"", StringComparison.Ordinal), "MetricCard should render compact mini bar groups.");
        Assert(metricSvg.Contains("data-cfx-role=\"metric-mini-bar-highlight\"", StringComparison.Ordinal), "MetricCard should emphasize one mini bar.");
        Assert(metric.ToHtmlFragment().Contains("chartforgex-visual-block", StringComparison.Ordinal), "MetricCard should render an embeddable HTML fragment.");
        Assert(metric.ToPng().Length > 64, "MetricCard should render PNG output.");

        var symbolMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Patch Rate", "94%", "OK")
            .WithSymbol("WRN")
            .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
            .WithStatus(VisualStatus.Warning)
            .WithTheme(VisualTheme.TransparentOverlay(), VisualThemeMode.Dark)
            .WithSize(300, 170);
        var symbolMetricSvg = symbolMetric.ToSvg("visual-block-metric-symbol");
        Assert(symbolMetricSvg.Contains("data-cfx-role=\"metric-symbol\"", StringComparison.Ordinal), "MetricCard should render text symbols when no icon is configured.");
        Assert(symbolMetricSvg.Contains("dominant-baseline=\"central\"", StringComparison.Ordinal), "MetricCard symbols should be vertically centered inside their badge.");
        Assert(symbolMetric.ToPng().Length > 64, "MetricCard symbol badges should render in PNG output.");

        var valueSurfaceMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Litres of water", "4.5", unit: "Litres")
            .WithIcon(VisualIcon.Droplet)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(300, 140);
        var valueSurfaceSvg = valueSurfaceMetric.ToSvg("visual-block-metric-value-surface");
        Assert(valueSurfaceSvg.Contains("data-cfx-role=\"metric-value-surface\"", StringComparison.Ordinal), "MetricCard should render compact metric value surfaces.");
        Assert(valueSurfaceSvg.Contains("data-cfx-role=\"metric-value-surface-badge\"", StringComparison.Ordinal), "MetricCard value surfaces should keep icons inside the value tray.");
        Assert(valueSurfaceSvg.Contains("data-cfx-role=\"metric-unit\"", StringComparison.Ordinal), "MetricCard should render optional metric units separately from emphasized values.");
        Assert(valueSurfaceMetric.ToPng().Length > 64, "MetricCard compact value surfaces should render PNG output.");

        var narrowValueSurfaceMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Energy", "123456789", unit: "kilocalories-per-day")
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(210, 140);
        var narrowValueSurfaceSvg = narrowValueSurfaceMetric.ToSvg("visual-block-metric-narrow-value-surface");
        Assert(narrowValueSurfaceSvg.Contains("data-cfx-role=\"metric-unit\"", StringComparison.Ordinal), "MetricCard narrow value surfaces should still render unit text.");
        var narrowElements = XDocument.Parse(narrowValueSurfaceSvg).Descendants().ToArray();
        var narrowValue = narrowElements.Single(element => (string?)element.Attribute("data-cfx-role") == "metric-value");
        var narrowUnit = narrowElements.Single(element => (string?)element.Attribute("data-cfx-role") == "metric-unit");
        var narrowSurface = narrowElements.Single(element => (string?)element.Attribute("data-cfx-role") == "metric-value-surface");
        double MetricNumber(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
        TextStyle MetricStyle(XElement element) => new() {
            Font = new FontSpec { Family = element.Attribute("font-family")!.Value, Weight = (int)MetricNumber(element, "font-weight") },
            FontSize = MetricNumber(element, "font-size"), LineHeight = 1 };
        double MetricWidth(string text, XElement element) => TextLayoutEngine.Measure(text, MetricStyle(element)).Width;
        var narrowRight = MetricNumber(narrowSurface, "x") + MetricNumber(narrowSurface, "width") - 22;
        var narrowUnitWidth = narrowRight - MetricNumber(narrowUnit, "x");
        Assert(narrowValue.Value.Any(char.IsDigit), "A narrow metric must retain a visible primary-value prefix.");
        Assert(MetricNumber(narrowValue, "font-size") >= 18, "Primary facts should retain readable emphasis.");
        Assert(narrowUnit.Value.Length > 0, "A narrow metric should retain a visible unit indication.");
        var minimumUnitWidth = Math.Min(MetricWidth(narrowValueSurfaceMetric.Unit, narrowUnit), MetricWidth("...", narrowUnit));
        if (MetricWidth(narrowValueSurfaceMetric.Value, narrowValue) + minimumUnitWidth + 6 <= narrowRight - MetricNumber(narrowValue, "x") + .01)
            Assert(narrowValue.Value == narrowValueSurfaceMetric.Value, "A primary fact should remain complete when it fits beside a visible unit indication.");
        else Assert(narrowValue.Value.StartsWith("1", StringComparison.Ordinal) && narrowValue.Value.EndsWith("...", StringComparison.Ordinal), "An oversized primary fact should show its prefix and truncation.");
        Assert(MetricNumber(narrowValue, "x") + MetricWidth(narrowValue.Value, narrowValue) + 2 <= MetricNumber(narrowUnit, "x"), "Metric value and unit ink should remain separate.");
        Assert(MetricWidth(narrowUnit.Value, narrowUnit) <= narrowUnitWidth + .01, "Metric unit ink should fit inside the remaining inset width.");
        foreach (var text in new[] { narrowValue, narrowUnit }) {
            var style = MetricStyle(text);
            var metrics = TextLayoutEngine.Measure(text.Value, style);
            var ascent = TypographyFontResolver.ResolveFace(style.Font).Font?.Ascent(style.FontSize) ?? metrics.LineHeight;
            var top = MetricNumber(text, "y") - ascent;
            Assert(top >= MetricNumber(narrowSurface, "y") - .01 && top + metrics.Height <= MetricNumber(narrowSurface, "y") + MetricNumber(narrowSurface, "height") + .01,
                "Primary value and unit line boxes should fit vertically inside their inset surface.");
        }
        if (MetricWidth(narrowValueSurfaceMetric.Unit, narrowUnit) > narrowUnitWidth)
            Assert(narrowUnit.Value.EndsWith("...", StringComparison.Ordinal), "A unit should show truncation when its full text cannot fit.");
        Assert(narrowValueSurfaceMetric.ToPng().Length > 64, "MetricCard narrow value surfaces should render PNG output.");

        var compactValueSurfaceMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Compact", "123", unit: "kilocalories-per-day")
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(210, 90);
        var compactValueSurfaceSvg = compactValueSurfaceMetric.ToSvg("visual-block-metric-compact-value-surface");
        var compactValueSurface = System.Text.RegularExpressions.Regex.Match(compactValueSurfaceSvg, "data-cfx-role=\"metric-value-surface\"[^>]*y=\"([^\"]+)\"[^>]*height=\"([^\"]+)\"");
        Assert(compactValueSurface.Success, "MetricCard compact inset values should render a measurable value surface.");
        var compactValueSurfaceY = double.Parse(compactValueSurface.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var compactValueSurfaceHeight = double.Parse(compactValueSurface.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert(compactValueSurfaceY + compactValueSurfaceHeight <= 90, "MetricCard compact inset value surfaces should stay within the card bounds.");
        Assert(compactValueSurfaceMetric.ToPng().Length > 64, "MetricCard compact inset values should render PNG output.");

        var sparkMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Network", "842 Mbps")
            .WithStatus(VisualStatus.Info)
            .WithMiniSparkline(new[] { 42d, 36d, 31d, 28d, 24d, 18d });
        var sparkSvg = sparkMetric.ToSvg("visual-block-metric-sparkline");
        Assert(sparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline\"", StringComparison.Ordinal), "MetricCard should render compact sparkline groups.");
        Assert(sparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-current\"", StringComparison.Ordinal), "MetricCard sparklines should mark the current value.");
        Assert(sparkSvg.Contains("842 Mbps", StringComparison.Ordinal), "MetricCard should shrink long values enough to fit beside micro visuals.");
        Assert(sparkMetric.ToPng().Length > 64, "MetricCard sparkline should render PNG output.");

        var areaSparkMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Hydration", "4.5 L")
            .WithMiniSparkline(new[] { 1d, 2d, 3d })
            .WithSecondaryMiniSparkline(new[] { 100d, 120d, 140d });
        var areaSparkSvg = areaSparkMetric.ToSvg("visual-block-metric-area-sparkline");
        Assert(areaSparkSvg.Contains("data-cfx-style=\"area\"", StringComparison.Ordinal), "MetricCard should keep the default filled area sparkline style.");
        Assert(areaSparkSvg.Contains("data-cfx-max=\"3\"", StringComparison.Ordinal), "Area-style MetricCard sparklines should derive bounds from the visible primary series only.");
        Assert(!areaSparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-secondary\"", StringComparison.Ordinal), "Area-style MetricCard sparklines should not render hidden secondary series.");

        var heroSparkMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Running", "30 mins")
            .WithMiniSparkline(new[] { 18d, 30d, 34d, 25d, 28d, 43d, 45d, 44d, 48d })
            .WithSecondaryMiniSparkline(new[] { 15d, 27d, 31d, 23d, 25d, 40d, 42d, 41d, 45d })
            .WithMiniSparklineStyle(ChartLineAreaForm.Line)
            .WithMicroVisualPlacement(MetricCardMicroVisualPlacement.Hero)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithSize(360, 300);
        var heroSparkSvg = heroSparkMetric.ToSvg("visual-block-metric-hero-sparkline");
        Assert(heroSparkSvg.Contains("data-cfx-role=\"metric-micro-surface\"", StringComparison.Ordinal), "MetricCard hero mini visuals should support an inset surface.");
        Assert(heroSparkSvg.Contains("data-cfx-style=\"line\"", StringComparison.Ordinal), "MetricCard should render line-style sparklines without area fill.");
        Assert(heroSparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-secondary\"", StringComparison.Ordinal), "MetricCard should render a true secondary sparkline series.");
        Assert(heroSparkSvg.Contains("data-cfx-role=\"metric-mini-sparkline-start\"", StringComparison.Ordinal), "MetricCard line-style sparklines should mark the starting value.");
        Assert(heroSparkMetric.ToPng().Length > 64, "MetricCard hero line sparkline should render PNG output.");

        var compactHeroSparkMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Compact", "10")
            .WithMiniSparkline(new[] { 2d, 5d, 3d, 8d })
            .WithMicroVisualPlacement(MetricCardMicroVisualPlacement.Hero)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .AddDetail("Ready", "84%")
            .WithSize(260, 140);
        var compactHeroSparkSvg = compactHeroSparkMetric.ToSvg("visual-block-metric-compact-hero-sparkline");
        var compactSurface = System.Text.RegularExpressions.Regex.Match(compactHeroSparkSvg, "data-cfx-role=\"metric-micro-surface\"[^>]*y=\"([^\"]+)\"[^>]*height=\"([^\"]+)\"");
        Assert(compactSurface.Success, "MetricCard compact hero inset sparklines should render a measurable inset surface.");
        var compactSurfaceY = double.Parse(compactSurface.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var compactSurfaceHeight = double.Parse(compactSurface.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert(compactSurfaceY + compactSurfaceHeight <= 140, "MetricCard compact hero inset surfaces should stay within the card bounds.");
        Assert(!compactHeroSparkSvg.Contains("data-cfx-role=\"metric-detail\"", StringComparison.Ordinal), "MetricCard compact hero sparklines should skip detail pills when there is no non-overlapping space.");
        Assert(compactHeroSparkMetric.ToPng().Length > 64, "MetricCard compact hero sparkline should render PNG output.");

        var retainedSparklineStyleMetric = MetricCard.Create().WithTheme(VisualTheme.Report())
            .WithMetric("Style", "1")
            .WithMiniSparkline(new[] { 1d, 2d })
            .WithMiniSparklineStyle(ChartLineAreaForm.Line)
            .WithoutMiniSparkline()
            .WithMiniSparkline(new[] { 2d, 4d });
        Assert(retainedSparklineStyleMetric.ToSvg("visual-block-metric-retained-sparkline-style").Contains("data-cfx-style=\"line\"", StringComparison.Ordinal), "MetricCard should preserve configured sparkline style after clearing and replacing sparkline data.");
    }
}
