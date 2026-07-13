using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PictorialItemsRenderSymbolRows() {
        var chart = Chart.Create()
            .WithSize(760, 430)
            .WithTheme(ChartTheme.Candy())
            .AddPictorial("Audience", new[] {
                new ChartPictorialItem("New users", 84),
                new ChartPictorialItem("Returning users", 62),
                new ChartPictorialItem("Advocates", 29)
            }, ChartPictorialShape.Star);

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"pictorial-chart\"", StringComparison.Ordinal), "Pictorial charts should expose a chart role marker.");
        Assert(svg.Contains("data-cfx-shape=\"Star\"", StringComparison.Ordinal), "Pictorial charts should expose the selected built-in shape.");
        Assert(svg.Contains("data-cfx-columns=\"12\"", StringComparison.Ordinal), "Pictorial charts should expose the configured symbol count.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"pictorial-symbol\"") == 36, "Pictorial charts should render a fixed symbol row per item.");
        Assert(svg.Contains("data-cfx-role=\"pictorial-value\" data-cfx-point=\"0\" data-cfx-label=\"New users\" data-cfx-value=\"84\"", StringComparison.Ordinal), "Pictorial value labels should expose data metadata.");
        Assert(svg.Contains("data-cfx-role=\"pictorial-label\"", StringComparison.Ordinal), "Pictorial charts should render item labels.");
        Assert(chart.ToPng().Length > 64, "Pictorial charts should render PNG output.");
        var fiveStar = Chart.Create()
            .WithSize(520, 260)
            .WithPictorialColumns(5)
            .WithPictorialMaximum(5)
            .AddPictorial("Rating", new[] {
                new ChartPictorialItem("Product", 3.5),
                new ChartPictorialItem("Support", 5)
            }, ChartPictorialShape.Star);
        var fiveStarSvg = fiveStar.ToSvg();
        Assert(fiveStarSvg.Contains("data-cfx-columns=\"5\"", StringComparison.Ordinal), "Pictorial charts should support compact five-symbol rows.");
        Assert(fiveStarSvg.Contains("data-cfx-maximum=\"5\"", StringComparison.Ordinal), "Pictorial charts should expose explicit scale maximum metadata.");
        Assert(fiveStarSvg.Contains("data-cfx-fill=\"0.5\"", StringComparison.Ordinal), "Pictorial charts should use explicit scale maximums for partial symbols.");
        Assert(fiveStarSvg.Contains("data-cfx-partial-fill=\"clip\"", StringComparison.Ordinal), "Pictorial partial symbols should render as clipped fills.");
        Assert(fiveStarSvg.Contains("data-cfx-symbol-layer=\"partial-fill\"", StringComparison.Ordinal), "Pictorial partial symbols should expose a filled overlay layer.");
        Assert(fiveStarSvg.Contains("<clipPath id=\"", StringComparison.Ordinal), "Pictorial partial symbols should use SVG clipping.");
        Assert(fiveStarSvg.Contains("data-cfx-symbol-scale=\"1\"", StringComparison.Ordinal), "Pictorial charts should expose symbol scale metadata.");
        Assert(fiveStarSvg.Contains("data-cfx-empty-opacity=\"0.22\"", StringComparison.Ordinal), "Pictorial charts should expose empty-symbol opacity metadata.");
        Assert(CountOccurrences(fiveStarSvg, "data-cfx-role=\"pictorial-symbol\"") == 10, "Pictorial column settings should control symbol density.");
        Assert(fiveStar.ToPng().Length > 64, "Pictorial column settings should render PNG output.");
        var tenStar = Chart.Create()
            .WithSize(520, 260)
            .WithPictorialColumns(5)
            .WithPictorialMaximum(10)
            .AddPictorial("Rating", new[] {
                new ChartPictorialItem("Product", 3.5),
                new ChartPictorialItem("Support", 5)
            }, ChartPictorialShape.Star);
        AssertNoDuplicateIds(fiveStarSvg + tenStar.ToSvg(), "Unscoped pictorial charts with distinct scaling options");
        var styledPictorial = Chart.Create()
            .WithSize(520, 260)
            .WithPictorialColumns(5)
            .WithPictorialMaximum(5)
            .WithPictorialSymbolScale(1.2)
            .WithPictorialEmptyOpacity(0.08)
            .AddPictorial("Rating", new[] { new ChartPictorialItem("Product", 3) }, ChartPictorialShape.Star);
        var styledPictorialSvg = styledPictorial.ToSvg();
        Assert(styledPictorialSvg.Contains("data-cfx-symbol-scale=\"1.2\"", StringComparison.Ordinal), "Pictorial symbol scale should be configurable.");
        Assert(styledPictorialSvg.Contains("data-cfx-empty-opacity=\"0.08\"", StringComparison.Ordinal), "Pictorial empty-symbol opacity should be configurable.");
        Assert(styledPictorial.ToPng().Length > 64, "Pictorial styling options should render PNG output.");
        var customColors = Chart.Create()
            .WithSize(520, 260)
            .WithPictorialColumns(5)
            .WithPictorialMaximum(5)
            .AddPictorial("Rating", new[] {
                new ChartPictorialItem("Product", 3.5, ChartColor.FromRgb(14, 165, 233)),
                new ChartPictorialItem("Support", 4.5, ChartColor.FromRgb(236, 72, 153))
        }, ChartPictorialShape.Heart);
        var customColorsSvg = customColors.ToSvg();
        Assert(customColorsSvg.Contains("#0EA5E9", StringComparison.Ordinal), "Pictorial item colors should override the theme palette.");
        Assert(customColorsSvg.Contains("#EC4899", StringComparison.Ordinal), "Pictorial item colors should apply per row.");
        var pointLegendSvg = customColors.WithPointLegend().ToSvg();
        Assert(pointLegendSvg.Contains("data-cfx-role=\"legend-item\" data-cfx-series=\"0\" data-cfx-point=\"1\"", StringComparison.Ordinal), "Pictorial point legends should expose item metadata.");
        Assert(pointLegendSvg.Contains("#EC4899", StringComparison.Ordinal), "Pictorial point legends should match item colors.");
        Assert(customColors.ToPng().Length > 64, "Pictorial item colors should render PNG output.");
        var isotype = Chart.Create()
            .WithSize(520, 260)
            .WithPictorialColumns(25)
            .WithPictorialValuePerSymbol(1)
            .WithPictorialValues(false)
            .AddPictorial("People", new[] {
                new ChartPictorialItem("A", 24, ChartColor.FromRgb(239, 68, 68)),
                new ChartPictorialItem("B", 4, ChartColor.FromRgb(239, 68, 68)),
                new ChartPictorialItem("C", 1, ChartColor.FromRgb(239, 68, 68))
            }, ChartPictorialShape.Person);
        var isotypeSvg = isotype.ToSvg();
        Assert(isotypeSvg.Contains("data-cfx-value-per-symbol=\"1\"", StringComparison.Ordinal), "Pictorial charts should expose value-per-symbol scale metadata.");
        Assert(isotypeSvg.Contains("data-cfx-show-values=\"false\"", StringComparison.Ordinal), "Pictorial charts should expose hidden value label metadata.");
        Assert(!isotypeSvg.Contains("data-cfx-role=\"pictorial-value\"", StringComparison.Ordinal), "Pictorial value labels should be optional for Isotype-style charts.");
        Assert(CountOccurrences(isotypeSvg, "data-cfx-role=\"pictorial-symbol\"") == 75, "Pictorial value-per-symbol charts should still render the configured symbol grid.");
        Assert(CountOccurrences(isotypeSvg, "data-cfx-fill=\"1\"") == 29, "Pictorial value-per-symbol charts should render one filled symbol per unit when values are integral.");
        Assert(isotype.ToPng().Length > 64, "Isotype-style pictorial charts should render PNG output.");
        var wrappedIsotype = Chart.Create()
            .WithSize(560, 300)
            .WithPictorialColumns(20)
            .WithPictorialValuePerSymbol(1)
            .WithPictorialValues(false)
            .AddPictorial("People", new[] {
                new ChartPictorialItem("A", 50, ChartColor.FromRgb(239, 68, 68)),
                new ChartPictorialItem("B", 25, ChartColor.FromRgb(239, 68, 68))
            }, ChartPictorialShape.Person);
        var wrappedIsotypeSvg = wrappedIsotype.ToSvg();
        Assert(CountOccurrences(wrappedIsotypeSvg, "data-cfx-role=\"pictorial-symbol\"") == 100, "Pictorial value-per-symbol rows should wrap into additional symbol rows when values exceed the configured columns.");
        Assert(wrappedIsotypeSvg.Contains("data-cfx-row=\"2\"", StringComparison.Ordinal), "Wrapped pictorial rows should expose their symbol row metadata.");
        Assert(CountOccurrences(wrappedIsotypeSvg, "data-cfx-fill=\"1\"") == 75, "Wrapped pictorial rows should preserve the represented value count.");
        Assert(wrappedIsotype.ToPng().Length > 64, "Wrapped Isotype pictorial rows should render PNG output.");
        const string SparkPath = "M12 2 L15 9 L22 9 L17 14 L19 22 L12 17 L5 22 L7 14 L2 9 L9 9 Z";
        var customSymbol = Chart.Create()
            .WithSize(520, 260)
            .WithPictorialColumns(5)
            .WithPictorialSvgPath(SparkPath, ChartPictorialShape.Star)
            .AddPictorial("Custom", new[] { new ChartPictorialItem("Spark", 4) }, ChartPictorialShape.Circle);
        var customSymbolSvg = customSymbol.ToSvg();
        Assert(customSymbolSvg.Contains("data-cfx-custom-symbol=\"true\"", StringComparison.Ordinal), "Pictorial SVG output should expose custom symbol metadata.");
        Assert(customSymbolSvg.Contains("data-cfx-png-fallback-shape=\"Star\"", StringComparison.Ordinal), "Pictorial SVG output should expose the PNG fallback shape.");
        Assert(customSymbolSvg.Contains("d=\"" + SparkPath + "\"", StringComparison.Ordinal), "Pictorial SVG output should render custom path data.");
        Assert(customSymbol.ToPng().Length > 64, "Custom pictorial symbols should render PNG output with a fallback shape.");
        var resetSymbol = Chart.Create()
            .WithSize(520, 260)
            .WithPictorialSvgPath(SparkPath, ChartPictorialShape.Star)
            .AddPictorial("Built-in", new[] { new ChartPictorialItem("Diamond", 4) }, ChartPictorialShape.Circle)
            .WithPictorialShape(ChartPictorialShape.Diamond);
        var resetSymbolSvg = resetSymbol.ToSvg();
        Assert(resetSymbolSvg.Contains("data-cfx-custom-symbol=\"false\"", StringComparison.Ordinal), "Pictorial shape selection should clear custom symbol paths.");
        Assert(resetSymbolSvg.Contains("data-cfx-shape=\"Diamond\"", StringComparison.Ordinal), "Pictorial shape selection should update the built-in symbol.");
        Assert(!resetSymbolSvg.Contains("d=\"" + SparkPath + "\"", StringComparison.Ordinal), "Built-in pictorial symbols should not retain custom path data.");
        Assert(resetSymbol.ToPng().Length > 64, "Reset pictorial symbols should render PNG output.");
        foreach (var shape in new[] { ChartPictorialShape.Circle, ChartPictorialShape.Square, ChartPictorialShape.Diamond, ChartPictorialShape.Triangle, ChartPictorialShape.Star, ChartPictorialShape.Heart, ChartPictorialShape.Shield, ChartPictorialShape.Check, ChartPictorialShape.Person, ChartPictorialShape.PersonDress }) {
            var shaped = Chart.Create()
                .WithSize(420, 260)
                .AddPictorial("Shape", new[] { new ChartPictorialItem(shape.ToString(), 10) }, shape);
            Assert(shaped.ToSvg().Contains("data-cfx-shape=\"" + shape + "\"", StringComparison.Ordinal), "Pictorial SVG output should support the " + shape + " shape.");
            Assert(shaped.ToPng().Length > 64, "Pictorial PNG output should support the " + shape + " shape.");
        }
    }

    private static void ProgressBarsRenderSliderRows() {
        var chart = Chart.Create()
            .WithSize(560, 320)
            .WithTheme(ChartTheme.PeopleInfographic())
            .WithProgressBarThickness(0.34)
            .WithProgressTrackOpacity(0.18)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .AddProgressBars("Preference", new[] {
                new ChartProgressItem("Male", 40, ChartColor.FromRgb(6, 182, 212)),
                new ChartProgressItem("Female", 60, ChartColor.FromRgb(219, 39, 119))
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"progress-bar-chart\"", StringComparison.Ordinal), "Progress-bar charts should expose a chart role marker.");
        Assert(svg.Contains("data-cfx-maximum=\"100\"", StringComparison.Ordinal), "Progress-bar charts should expose their shared maximum.");
        Assert(svg.Contains("data-cfx-show-handles=\"true\"", StringComparison.Ordinal), "Progress-bar charts should expose handle visibility metadata.");
        Assert(svg.Contains("data-cfx-bar-thickness-ratio=\"0.34\"", StringComparison.Ordinal), "Progress-bar charts should expose thickness metadata.");
        Assert(svg.Contains("data-cfx-track-opacity=\"0.18\"", StringComparison.Ordinal), "Progress-bar charts should expose track-opacity metadata.");
        Assert(svg.Contains("data-cfx-role=\"progress-track\"", StringComparison.Ordinal), "Progress-bar charts should render track rows.");
        Assert(svg.Contains("data-cfx-role=\"progress-fill\" data-cfx-point=\"0\" data-cfx-value=\"40\" data-cfx-ratio=\"0.4\"", StringComparison.Ordinal), "Progress-bar fills should expose value and ratio metadata.");
        Assert(svg.Contains("data-cfx-role=\"progress-handle\"", StringComparison.Ordinal), "Progress-bar charts should render slider handles.");
        Assert(svg.Contains("40%", StringComparison.Ordinal) && svg.Contains("60%", StringComparison.Ordinal), "Progress-bar charts should render formatted value labels.");
        Assert(chart.ToPng().Length > 64, "Progress-bar charts should render PNG output.");

        var hiddenValues = Chart.Create()
            .WithSize(560, 320)
            .WithProgressValues(false)
            .WithProgressHandles(false)
            .AddProgressBars("Completion", new[] { new ChartProgressItem("Done", 75) });
        var hiddenSvg = hiddenValues.ToSvg();
        Assert(hiddenSvg.Contains("data-cfx-show-values=\"false\"", StringComparison.Ordinal), "Progress-bar charts should expose hidden value label metadata.");
        Assert(hiddenSvg.Contains("data-cfx-show-handles=\"false\"", StringComparison.Ordinal), "Progress-bar charts should expose hidden handle metadata.");
        Assert(!hiddenSvg.Contains("data-cfx-role=\"progress-value\"", StringComparison.Ordinal), "Progress-bar value labels should be optional.");
        Assert(!hiddenSvg.Contains("data-cfx-role=\"progress-handle\"", StringComparison.Ordinal), "Progress-bar handles should be optional.");
        Assert(hiddenValues.ToPng().Length > 64, "Progress-bar hidden value labels should render PNG output.");

        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithProgressMaximum(0), "Progress-bar charts should reject zero maximums.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithProgressBarThickness(0.1), "Progress-bar thickness should reject tiny ratios.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithProgressTrackOpacity(1.2), "Progress-bar track opacity should reject values above one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialSymbolScale(0.3), "Pictorial symbol scale should reject tiny values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPictorialEmptyOpacity(1.1), "Pictorial empty opacity should reject values above one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddProgressBars("Bad", new[] { new ChartProgressItem("A", 1) }, double.NaN), "Progress-bar charts should reject non-finite maximums.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartProgressItem("Bad", -1), "Progress-bar items should reject negative values.");
    }
}
