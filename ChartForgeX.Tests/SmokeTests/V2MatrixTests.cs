using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2MatrixTests {
    [Fact]
    public void NumericHeatmapKeepsZeroSurfaceGapAndObservedScaleValues() {
        var chart = Chart.Create().AddHeatmapRow("Count", new[] { 0d, 20, 100 });
        chart.Options.HeatmapRelativeScale = true;
        var context = Context(); var prepared = chart.Prepare(context); var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal("2", ByRole(document, "heatmap").Single().Attribute("data-cfx-cell-gap")?.Value);
        var zero = ByRole(document, "heatmap-cell").Single(element => element.Attribute("data-cfx-value")?.Value == "0");
        Assert.Equal(ChartHeatmapSurface.ZeroBlend(context.Theme.Resolve(context.ThemeMode)).Color.ToCss(),
            zero.Descendants().Single(element => element.Name.LocalName == "rect").Attribute("fill")?.Value);
        var steps = ByRole(document, "heatmap-scale-step");
        Assert.Equal(5, steps.Length);
        Assert.Equal(new[] { "20", "40", "60", "80", "100" }, steps.Select(element => element.Attribute("data-cfx-value")?.Value));
        Assert.Single(ByRole(document, "heatmap-scale-zero"));
        Assert.Contains(prepared.Regions, region => region.Role == "heatmap-scale-step" && region.Label == "100");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public void NativeMatricesPreserveMasksPointColorsBoundsAndDetachedExports(bool hexagonal, VisualThemeMode mode) {
        var chart = Chart.Create().WithXLabels("A", "B", "C", "D").WithHeatmapScaleLegend(false);
        if (hexagonal) chart.AddHexbinHeatmapRow("Service", new double?[] { 0, null, 20, null });
        else chart.AddHeatmapRow("Service", new double?[] { 0, null, 20, null });
        var color = ChartColor.FromHex("#d4302f"); chart.Series[0].PointColors.Add(color);
        var prepared = chart.Prepare(Context(mode));
        var cells = prepared.Regions.Where(region => region.Role is "heatmap-cell" or "hexbin-cell").ToArray();
        Assert.Equal(2, cells.Length); Assert.Equal("series-0-point-0", cells[0].Id);
        Assert.True(cells[1].Bounds.Left > cells[0].Bounds.Right);
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal("4", ByRole(document, hexagonal ? "hexbin-heatmap" : "heatmap").Single().Attribute("data-cfx-column-count")?.Value);
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var offset = ((int)(cells[0].Bounds.Top + cells[0].Bounds.Height / 2) * image.Width + (int)(cells[0].Bounds.Left + cells[0].Bounds.Width / 2)) * 4;
        Assert.Equal(color.R, image.Pixels[offset]); Assert.Equal(color.G, image.Pixels[offset + 1]); Assert.Equal(color.B, image.Pixels[offset + 2]);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); chart.Series.Clear(); chart.Options.XAxisLabels.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(640, image.Width); Assert.Equal(400, image.Height);
    }

    [Fact]
    public void CategoricalCellsRetainSafeLinksTooltipsPatternsUnknownKeysAndFullIdentity() {
        var chart = Chart.Create().WithXLabels("Check A", "Check B", "Check C")
            .WithStateCategories(new ChartStateCategory("quiet", "Not evaluated", ChartColor.FromHex("#7c818a"), ChartStatePattern.CrossHatched, ChartStateEmphasis.Quiet))
            .AddHeatmapCategoryRow("Same", new ChartHeatmapCell?[] { new("quiet", "1", "Full evidence", "#details"), null, new("pending") }, "Group")
            .AddHeatmapCategoryRow("Same", new ChartHeatmapCell("quiet"));
        chart.Options.PinStateColorsInForcedColors = true;
        var prepared = chart.Prepare(Context()); var document = XDocument.Parse(prepared.ToSvg());
        var cells = ByRole(document, "heatmap-cell"); Assert.Equal(3, cells.Length);
        Assert.Equal(3, prepared.Regions.Where(region => region.Role == "heatmap-cell").Select(region => region.Id).Distinct().Count());
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "a" && (string?)element.Attribute("href") == "#details");
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "title" && element.Value.Contains("Full evidence"));
        Assert.Contains(cells, element => (string?)element.Attribute("data-cfx-status") == "pending");
        Assert.Contains(document.Descendants(), element => (string?)element.Attribute("data-cfx-pattern") == "cross-hatched");
        Assert.NotEmpty(ByRole(document, "state-legend-swatch"));
        Assert.Contains(prepared.Regions, region => region.Label != null && region.Label.Contains("pending"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void CalendarRetainsZeroMissingLocalizedDatesAndSizingAcrossBothBackends() {
        var start = new DateTime(2026, 9, 28);
        var chart = Chart.Create().AddCalendarHeatmap("Activity", new[] { new ChartCalendarHeatmapItem(start, 0), new ChartCalendarHeatmapItem(start.AddDays(2), 5) },
            firstDayOfWeek: DayOfWeek.Monday, dayNames: new[] { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" });
        chart.WithCalendarHeatmapCells(maximumSize: 18, gap: 4);
        chart.Options.Labels.DateFormatter = date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var prepared = chart.Prepare(Context()); var document = XDocument.Parse(prepared.ToSvg());
        var cells = prepared.Regions.Where(region => region.Role == "calendar-cell").ToArray(); Assert.Equal(7, cells.Length);
        Assert.All(cells, cell => Assert.InRange(cell.Bounds.Width, 0, 18));
        Assert.Contains(cells, cell => cell.Label == "Activity, 28/09/2026: 0"); Assert.Contains(cells, cell => cell.Label == "Activity, 29/09/2026: No data");
        Assert.Equal("1", ByRole(document, "calendar-heatmap").Single().Attribute("data-cfx-zero-count")?.Value);
        Assert.Contains(ByRole(document, "calendar-weekday"), element => element.Value == "Mo");
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var zero = cells[0].Bounds; var missing = cells[1].Bounds;
        var zeroOffset = ((int)(zero.Top + zero.Height / 2) * image.Width + (int)(zero.Left + zero.Width / 2)) * 4;
        var emptyOffset = ((int)(missing.Top + missing.Height / 2) * image.Width + (int)(missing.Left + missing.Width / 2)) * 4;
        Assert.False(image.Pixels.Skip(zeroOffset).Take(3).SequenceEqual(image.Pixels.Skip(emptyOffset).Take(3)));
    }

    [Fact]
    public void RotatedColumnsAndExplicitCellTextStylesArePreparedWithNativeTransforms() {
        var chart = Chart.Create().WithXLabels("Long first column", "Long second column").WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always)
            .AddHeatmapRow("Row", new[] { 12d, 34d });
        chart.Options.XAxisLabelAngle = -45;
        chart.Series[0].WithPointDataLabelStyle(0, style => style.FontSize = 17);
        var prepared = chart.Prepare(Context());
        Assert.Contains("rotate(-45", prepared.ToSvg());
        Assert.Equal(2, ByRole(XDocument.Parse(prepared.ToSvg()), "data-label").Length);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void QuietStateLabelsUsePreparedBackdropRatherThanLegacyTheme() {
        var state = new ChartStateCategory("quiet", "Quiet", ChartColor.FromHex("#777777"), emphasis: ChartStateEmphasis.Quiet);
        var chart = Chart.Create().WithStateCategories(state).WithMarkBackdrop(ChartMarkBackdrop.Background)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always).AddHeatmapCategoryRow("Service", new ChartHeatmapCell("quiet", "Ready"));
        var context = Context(VisualThemeMode.Dark); var prepared = chart.Prepare(context);
        var svg = prepared.ToSvg(); chart.Options.Theme = ChartTheme.Light();
        Assert.Equal(svg, prepared.ToSvg());
        var colors = context.Theme.Resolve(context.ThemeMode);
        var ink = ChartMarkText.OnStateMark(chart, colors, context.Frame, ChartStateMark.For(state, colors.Background));
        var label = ByRole(XDocument.Parse(svg), "data-label").Single();
        Assert.Equal(ink.Color.ToCss(), label.Descendants().Single(element => element.Name.LocalName == "text").Attribute("fill")?.Value);
    }

    [Fact]
    public void TranslucentStateColorKeepsAlphaAndResolvesContrastAgainstPreparedSurface() {
        var state = new ChartStateCategory("partial", "Partial", ChartColor.FromRgba(240, 40, 50, 96));
        var chart = Chart.Create().WithStateCategories(state).WithMarkBackdrop(ChartMarkBackdrop.Plot)
            .AddHeatmapCategoryRow("Service", new ChartHeatmapCell("partial"));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240)), frame: new VisualFrame(showLegend: false, showSurface: true));
        var prepared = chart.Prepare(context); var box = prepared.Regions.Single(region => region.Role == "heatmap-cell").Bounds;
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var offset = ((int)(box.Top + box.Height / 2) * image.Width + (int)(box.Left + box.Width / 2)) * 4;
        var expected = ChartStateMark.For(state, context.Theme.Resolve(context.ThemeMode).Surface).Surface;
        Assert.InRange(Math.Abs(image.Pixels[offset] - expected.R), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[offset + 1] - expected.G), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[offset + 2] - expected.B), 0, 1);
    }

    [Fact]
    public void MatrixStateAndExplicitPointColorsWithEqualRgbKeepDistinctPaintRoles() {
        var same = ChartColor.FromHex("#717171");
        var variables = new SvgColorVariables().Add("--source", same, SvgColorRole.Series).Add("--state", same, SvgColorRole.Status)
            .Add("--surface", same, SvgColorRole.Surface);
        var numeric = Chart.Create().AddHeatmapRow("Source", new[] { 12d }); numeric.Series[0].PointColors.Add(same);
        var state = Chart.Create().WithStateCategories(new ChartStateCategory("known", "Known", same)).AddHeatmapCategoryRow("State", new ChartHeatmapCell("known"));
        var sourceFill = ByRole(XDocument.Parse(numeric.Prepare(Context()).ToSvg(new VisualSvgOptions(colorVariables: variables))), "heatmap-cell-shape").Single().Attribute("fill")!.Value;
        var stateFill = ByRole(XDocument.Parse(state.Prepare(Context()).ToSvg(new VisualSvgOptions(colorVariables: variables))), "heatmap-cell-shape").Single().Attribute("fill")!.Value;
        Assert.Contains("--source", sourceFill); Assert.DoesNotContain("--state", sourceFill);
        Assert.Contains("--state", stateFill); Assert.DoesNotContain("--source", stateFill);
    }

    [Fact]
    public void PreparedMapBlendSharesScaleInterpolationAndRetainsStopOperands() {
        var low = ChartColor.FromHex("#102030"); var high = ChartColor.FromHex("#90a0b0");
        var chart = Chart.Create(); chart.Options.MapColorScale = ChartMapColorScale.Sequential(low, high);
        var colors = Context().Theme.Resolve(VisualThemeMode.Light);
        var blend = ChartHeatmapSurface.MapBlend(chart, colors, null, null, 25, 0, 100);
        Assert.Equal(chart.Options.MapColorScale.ColorFor(25, 0, 100), blend.Color);
        Assert.Equal(low, blend.From); Assert.Equal(high, blend.To); Assert.Equal(.25, blend.Amount);
        var explicitPoint = ChartHeatmapSurface.MapBlend(chart, colors, high, low, 25, 0, 100);
        Assert.Equal(high, explicitPoint.Color); Assert.Equal(SvgColorRole.Series, explicitPoint.FromRole);
        var variables = new SvgColorVariables().Add("--low", low, SvgColorRole.Ramp).Add("--high", high, SvgColorRole.Ramp);
        Assert.Contains("--low", SvgPaint.Resolve(blend.Paint.Value!, variables));
        Assert.Contains("--high", SvgPaint.Resolve(blend.Paint.Value!, variables));
    }

    private static VisualRenderContext Context(VisualThemeMode mode = VisualThemeMode.Light) =>
        new(new VisualLayoutOptions(new VisualSize(640, 400)), themeMode: mode, frame: new VisualFrame("Matrix", "Native scene"));
    private static XElement[] ByRole(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
