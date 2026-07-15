using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AxisObjectsCentralizeBoundsTicksAndScales() {
        var chart = Chart.Create()
            .ConfigureXAxis(axis => {
                axis.WithBounds(1, 1000).WithScale(ChartScaleKind.Logarithmic);
                axis.TickCount = 5;
            })
            .ConfigureYAxis(axis => axis.WithBounds(-10, 10).WithScale(ChartScaleKind.SymmetricLogarithmic))
            .AddLine("Values", new[] { new ChartPoint(1, -10), new ChartPoint(10, 0), new ChartPoint(1000, 10) });

        Assert(chart.Options.XAxisMinimum == 1 && chart.Options.XAxisMaximum == 1000, "Legacy bounds should delegate to the shared x-axis object during migration.");
        Assert(chart.Options.XAxis.Scale == ChartScaleKind.Logarithmic, "The x-axis should own its mathematical scale.");
        Assert(chart.Options.YAxis.Scale == ChartScaleKind.SymmetricLogarithmic, "The y-axis should own its mathematical scale.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithXAxisScale((ChartScaleKind)4), "Unimplemented category scale values should be rejected instead of silently rendering as linear.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithXAxisScale((ChartScaleKind)5), "Unimplemented band scale values should be rejected instead of silently rendering as linear.");
    }

    private static void LogarithmicAxesRenderWithSvgPngParity() {
        var chart = Chart.Create()
            .WithSize(560, 340)
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddLine("Orders", new[] { new ChartPoint(1, 1), new ChartPoint(2, 10), new ChartPoint(3, 100), new ChartPoint(4, 1000) });

        var svg = chart.ToSvg();
        var png = chart.ToPng();
        Assert(svg.Contains(">10</text>", StringComparison.Ordinal) && svg.Contains(">100</text>", StringComparison.Ordinal), "Logarithmic SVG axes should render power-of-ten ticks.");
        Assert(png.Length > 200 && png[0] == 137 && png[1] == 80, "Logarithmic axes should render through the PNG path.");

        var pie = Chart.Create().WithYAxisScale(ChartScaleKind.Logarithmic).AddPie("Share", new[] { new ChartPoint(0, 70), new ChartPoint(1, 30) });
        Assert(pie.ToSvg().Contains("data-cfx-role=\"pie-slice\"", StringComparison.Ordinal), "Standalone SVG charts should bypass cartesian logarithmic-axis setup.");
        Assert(pie.ToPng().Length > 200, "Standalone logarithmic-axis options should remain renderer-neutral.");

        var gauge = Chart.Create().WithYAxisScale(ChartScaleKind.Logarithmic).AddGauge("Score", 87);
        Assert(gauge.ToSvg().Contains("data-cfx-role=\"gauge\"", StringComparison.Ordinal), "Gauge SVG rendering should bypass unused cartesian logarithmic-axis setup.");
        Assert(gauge.ToPng().Length > 200, "Gauge logarithmic-axis options should remain renderer-neutral.");

        var secondaryOnly = Chart.Create()
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .WithSecondaryYAxis("Rate")
            .AddLine("Rate", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) });
        secondaryOnly.Series[0].UseSecondaryYAxis();
        Assert(secondaryOnly.ToSvg().Contains(">Rate</text>", StringComparison.Ordinal), "Secondary-only charts should seed an empty logarithmic primary domain with valid positive bounds.");
        Assert(secondaryOnly.ToPng().Length > 200, "Empty primary logarithmic domains should preserve SVG and PNG rendering parity.");

        var radar = Chart.Create()
            .WithSize(560, 340)
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddRadar("Magnitude", new[] { new ChartPoint(0, 1), new ChartPoint(1, 10), new ChartPoint(2, 1000) });
        var radarRoot = SvgDocument.Parse(radar.ToSvg()).Root;
        var spoke = radarRoot.FindByTag("line").First(element => element.GetAttribute("data-cfx-role") == "radar-spoke");
        var centerX = double.Parse(spoke.GetAttribute("x1")!, CultureInfo.InvariantCulture);
        var centerY = double.Parse(spoke.GetAttribute("y1")!, CultureInfo.InvariantCulture);
        var radarRadii = radarRoot.FindByTag("circle")
            .Where(element => element.GetAttribute("data-cfx-role") == "radar-point")
            .Select(element => {
                var dx = double.Parse(element.GetAttribute("cx")!, CultureInfo.InvariantCulture) - centerX;
                var dy = double.Parse(element.GetAttribute("cy")!, CultureInfo.InvariantCulture) - centerY;
                return Math.Sqrt(dx * dx + dy * dy);
            })
            .ToArray();
        Assert(radarRadii.Length == 3 && radarRadii[1] > radarRadii[0] * 1.5 && radarRadii[1] < radarRadii[2] * 0.75, "Radar geometry should space values through the configured logarithmic Y-axis transform.");
        Assert(radar.ToPng().Length > 200, "Radar value scales should preserve SVG and PNG rendering parity.");
    }

    private static void LogarithmicAxesRejectNonPositiveData() {
        var chart = Chart.Create()
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddLine("Invalid", new[] { new ChartPoint(1, 0), new ChartPoint(2, 10) });

        AssertThrows<InvalidOperationException>(() => chart.ToSvg(), "Logarithmic axes should reject zero and negative values instead of producing invalid geometry.");
        AssertThrows<InvalidOperationException>(() => chart.ToPng(), "SVG and PNG should enforce the same logarithmic data contract.");

        var radar = Chart.Create()
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddRadar("Invalid", new[] { new ChartPoint(0, 0), new ChartPoint(1, 10), new ChartPoint(2, 100) });
        AssertThrows<InvalidOperationException>(() => radar.ToSvg(), "Logarithmic radar axes should reject non-positive values in SVG output.");
        AssertThrows<InvalidOperationException>(() => radar.ToPng(), "Logarithmic radar axes should reject non-positive values in PNG output.");
    }

    private static void LogarithmicBarsKeepPositiveBaselinesAndPadding() {
        var chart = Chart.Create()
            .WithSize(420, 260)
            .WithXAxisScale(ChartScaleKind.Logarithmic)
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddBar("Orders", new[] { new ChartPoint(0.1, 10) });

        var svg = chart.ToSvg();
        var png = chart.ToPng();
        Assert(svg.Contains("data-cfx-role=\"bar\"", StringComparison.Ordinal), "Logarithmic bars should not inject a zero baseline or non-positive x padding.");
        Assert(png.Length > 200 && png[0] == 137 && png[1] == 80, "Logarithmic bar bounds should remain valid in PNG rendering.");
        var bar = SvgDocument.Parse(svg).Root.FindByTag("rect").First(element => element.GetAttribute("data-cfx-role") == "bar");
        Assert(double.Parse(bar.GetAttribute("height")!, CultureInfo.InvariantCulture) > 1, "The smallest logarithmic bar should retain visible height above a positive baseline.");

        var horizontal = Chart.Create()
            .WithSize(420, 260)
            .WithXAxisScale(ChartScaleKind.Logarithmic)
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddHorizontalBar("Orders", new[] {
                new ChartPoint(0, 10),
                new ChartPoint(1, 100)
            });
        var horizontalSvg = horizontal.ToSvg();
        var horizontalPng = horizontal.ToPng();
        Assert(CountOccurrences(horizontalSvg, "data-cfx-role=\"horizontal-bar\"") == 2, "Horizontal logarithmic bars should use the positive plot edge instead of mapping a zero baseline.");
        Assert(horizontalPng.Length > 200 && horizontalPng[0] == 137 && horizontalPng[1] == 80, "Horizontal logarithmic bars should preserve SVG and PNG rendering parity.");
        var horizontalBar = SvgDocument.Parse(horizontalSvg).Root.FindByTag("rect").First(element => element.GetAttribute("data-cfx-role") == "horizontal-bar");
        Assert(double.Parse(horizontalBar.GetAttribute("width")!, CultureInfo.InvariantCulture) > 1, "The smallest horizontal logarithmic bar should retain visible width above a positive baseline.");

        var stackedBars = Chart.Create()
            .WithSize(420, 260)
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .WithStackedBars()
            .AddBar("Base", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20) })
            .AddBar("Top", new[] { new ChartPoint(1, 30), new ChartPoint(2, 40) });
        Assert(CountOccurrences(stackedBars.ToSvg(), "data-cfx-role=\"bar\"") == 4, "Stacked logarithmic bars should map their first zero base to the shared positive baseline.");
        Assert(stackedBars.ToPng().Length > 200, "Stacked logarithmic bars should preserve SVG and PNG rendering parity.");

        var stackedAreas = Chart.Create()
            .WithSize(420, 260)
            .WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddStackedArea("Base", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20) })
            .AddStackedArea("Top", new[] { new ChartPoint(1, 30), new ChartPoint(2, 40) });
        Assert(CountOccurrences(stackedAreas.ToSvg(), "data-cfx-role=\"stacked-area\"") == 2, "Stacked logarithmic areas should map their first zero base to the shared positive baseline.");
        Assert(stackedAreas.ToPng().Length > 200, "Stacked logarithmic areas should preserve SVG and PNG rendering parity.");

        var mixedMarks = Chart.Create()
            .WithXAxisScale(ChartScaleKind.Logarithmic)
            .AddLine("Earlier", new[] { new ChartPoint(0.1, 10) })
            .AddBar("Later", new[] { new ChartPoint(10, 20) });
        var mixedRange = ChartRange.FromChart(mixedMarks);
        Assert(mixedRange.MinX <= 0.1, "Logarithmic mark padding should not move the plot minimum above earlier positive data from another series.");
        Assert(mixedMarks.ToSvg().Contains("data-cfx-role=\"bar\"", StringComparison.Ordinal), "Mixed logarithmic marks should preserve the complete x-domain in SVG output.");
        Assert(mixedMarks.ToPng().Length > 200, "Mixed logarithmic marks should preserve SVG and PNG rendering parity.");
    }

    private static void AxisObjectsOwnExplicitLabelsAndFormatterFallbacks() {
        var chart = Chart.Create()
            .WithSize(520, 300)
            .WithYAxisBounds(0, 10)
            .WithSecondaryYAxis("Rate", null)
            .WithSecondaryYAxisBounds(0, 100)
            .AddLine("Primary", new[] { new ChartPoint(0, 0), new ChartPoint(1, 10) })
            .AddLine("Secondary", new[] { new ChartPoint(0, 0), new ChartPoint(1, 100) });
        chart.Series[1].UseSecondaryYAxis();
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(0, "Primary low"));
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(10, "Primary high"));
        chart.Options.SecondaryYAxis.Labels.Add(new ChartAxisLabel(0, "Secondary low"));
        chart.Options.SecondaryYAxis.Labels.Add(new ChartAxisLabel(100, "Secondary high"));
        chart.Options.YAxis.LabelFormatter = _ => "primary formatter";

        var svg = chart.ToSvg();
        Assert(svg.Contains("Primary low", StringComparison.Ordinal) && svg.Contains("Primary high", StringComparison.Ordinal), "Primary y-axis labels should override generated formatting.");
        Assert(svg.Contains("Secondary low", StringComparison.Ordinal) && svg.Contains("Secondary high", StringComparison.Ordinal), "Secondary y-axis labels should render through the same axis contract.");
        Assert(CountOccurrences(svg, "primary formatter") > 0, "Primary generated ticks should keep their primary formatter.");
        Assert(svg.Contains(">20</text>", StringComparison.Ordinal), "Secondary generated ticks without a formatter should use numeric labels instead of inheriting the primary y-axis formatter.");
        Assert(chart.ToPng().Length > 200, "Axis-owned labels and formatters should render through the PNG path.");

        var invalidPrimary = Chart.Create().AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        invalidPrimary.Options.YAxis.Labels.Add(default);
        AssertThrows<InvalidOperationException>(() => invalidPrimary.ToSvg(), "Mutable primary y-axis labels should reject default entries with null text before rendering.");
        var invalidSecondary = Chart.Create().AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        invalidSecondary.Options.SecondaryYAxis.Labels.Add(default);
        AssertThrows<InvalidOperationException>(() => invalidSecondary.ToPng(), "Mutable secondary y-axis labels should reject default entries with null text before rendering.");
    }

    private static void TimeAxesProvideDeterministicDefaultLabels() {
        var chart = Chart.Create()
            .WithSize(620, 320)
            .WithXAxisScale(ChartScaleKind.Time)
            .AddLine("Daily", new[] {
                new ChartPoint(new DateTime(2026, 7, 1), 10),
                new ChartPoint(new DateTime(2026, 7, 15), 18)
            });

        Assert(chart.ToSvg().Contains("2026-07", StringComparison.Ordinal), "Time axes should provide deterministic invariant date labels when no formatter is supplied.");

        var vertical = Chart.Create()
            .WithSize(620, 320)
            .ConfigureYAxis(axis => axis.Scale = ChartScaleKind.Time)
            .AddLine("Primary dates", new[] {
                new ChartPoint(0, new DateTime(2026, 7, 1).ToOADate()),
                new ChartPoint(1, new DateTime(2026, 7, 15).ToOADate())
            });
        var verticalSvg = vertical.ToSvg();
        Assert(verticalSvg.Contains("2026-07", StringComparison.Ordinal), "Primary vertical time axes should use the same deterministic date fallback as horizontal time axes.");
        var verticalPng = vertical.ToPng();
        vertical.Options.YAxis.LabelFormatter = value => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert(verticalPng.SequenceEqual(vertical.ToPng()), "Primary vertical time axes should use the deterministic date fallback in PNG output.");

        var secondary = Chart.Create()
            .WithSize(620, 320)
            .WithSecondaryYAxis("Date")
            .AddLine("Primary", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) })
            .AddLine("Secondary dates", new[] {
                new ChartPoint(0, new DateTime(2026, 8, 1).ToOADate()),
                new ChartPoint(1, new DateTime(2026, 8, 15).ToOADate())
            });
        secondary.Series[1].UseSecondaryYAxis();
        secondary.Options.SecondaryYAxis.Scale = ChartScaleKind.Time;
        var secondarySvg = secondary.ToSvg();
        Assert(secondarySvg.Contains("2026-08", StringComparison.Ordinal), "Secondary vertical time axes should provide deterministic date labels without inheriting primary formatting.");
        var secondaryPng = secondary.ToPng();
        secondary.Options.SecondaryYAxis.LabelFormatter = value => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert(secondaryPng.SequenceEqual(secondary.ToPng()), "Secondary vertical time axes should use the deterministic date fallback in PNG output.");

        var horizontal = Chart.Create()
            .WithSize(620, 320)
            .WithXAxisScale(ChartScaleKind.Time)
            .AddHorizontalBar("Deadlines", new[] {
                new ChartPoint(0, new DateTime(2026, 9, 1).ToOADate()),
                new ChartPoint(1, new DateTime(2026, 9, 15).ToOADate())
            });
        var horizontalRange = ChartRange.FromChart(horizontal);
        Assert(horizontalRange.MinX > new DateTime(2026, 1, 1).ToOADate(), "Horizontal time axes should not inject the numeric zero baseline into date ranges.");
        Assert(horizontal.ToSvg().Contains("2026-09", StringComparison.Ordinal), "Horizontal time bars should render deterministic date ticks without compressing data against the OLE epoch.");
        Assert(horizontal.ToPng().Length > 200, "Horizontal time bars should preserve SVG and PNG rendering parity.");

        var waterfall = Chart.Create()
            .WithSize(620, 320)
            .WithYAxisScale(ChartScaleKind.SymmetricLogarithmic)
            .AddWaterfall("Delta", new[] { new ChartPoint(0, 1), new ChartPoint(1, 999), new ChartPoint(2, -500) });
        var waterfallSvg = waterfall.ToSvg();
        var firstWaterfallBar = SvgDocument.Parse(waterfallSvg).Root.FindByTag("rect").First(element => element.GetAttribute("data-cfx-role") == "waterfall-bar");
        Assert(double.Parse(firstWaterfallBar.GetAttribute("height")!, CultureInfo.InvariantCulture) > 5, "Waterfall geometry should use the configured symmetric-logarithmic transform instead of linear normalization.");
        Assert(waterfall.ToPng().Length > 200, "Waterfall axis scales should preserve SVG and PNG rendering parity.");
    }
}
