using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CategoricalHeatmapTests {
    private static readonly ChartColor Pass = ChartColor.FromHex("#1d8a52");
    private static readonly ChartColor Critical = ChartColor.FromHex("#d4302f");
    private static readonly ChartColor Neutral = ChartColor.FromHex("#7c818a");

    [Fact]
    public void ToSvg_CategoricalRows_UseStateColoursLinksTooltipsAndLegend() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        Assert.Equal(5, cells.Length);
        Assert.Equal(new[] { "pass", "critical", "notEvaluated", "pass", "critical" }, cells.Select(cell => (string)cell.Attribute("data-cfx-status")!).ToArray());
        Assert.Equal(new[] { Pass.ToCss(), Critical.ToCss(), Neutral.ToCss() }, cells.Take(3).Select(cell => (string)cell.RenderedAttribute("fill")!).ToArray());
        Assert.Equal("DC01, LDAP: Critical", (string)cells[1].Attribute("aria-label")!);
        Assert.Equal("DC02, Backup: Critical. Backup is 9 days old", Title(cells[4]));

        var links = ByRole(svg, "heatmap-cell-link");
        Assert.Equal(new[] { "#dc01-ldap", "evidence/dc02.html#backup" }, links.Select(link => (string)link.Attribute("href")!).ToArray());
        Assert.Contains(links[0].Ancestors(), owner => (string?)owner.Attribute("data-cfx-role") == "heatmap-cell");

        Assert.NotEmpty(ByRole(svg, "heatmap-cell-shape-hatch"));
        Assert.NotEmpty(ByRole(svg, "legend-swatch-hatch"));
        Assert.Equal(new[] { "Passed", "Critical", "Not evaluated" }, ByRole(svg, "legend-label").Select(label => label.Value).ToArray());
        Assert.Empty(ByRole(svg, "legend-item"));
        Assert.Empty(ByRole(svg, "heatmap-scale-step"));
    }

    [Fact]
    public void ToSvg_CellText_DrawsOnlyForCellsWithTextAndRespectsHiddenMode() {
        var chart = CreateChart();
        var labels = ByRole(XDocument.Parse(chart.ToSvg()), "data-label").Select(label => label.Value).ToArray();
        Assert.Equal(new[] { "3", "1" }, labels);

        chart.Options.HeatmapValueTextMode = ChartHeatmapValueTextMode.Hidden;
        Assert.Empty(ByRole(XDocument.Parse(chart.ToSvg()), "data-label"));
    }

    [Fact]
    public void ToSvg_CellText_DoesNotInterceptLinkedCellPointerEvents() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        var labels = ByRole(svg, "data-label");
        Assert.NotEmpty(labels);
        var linkedCell = ByRole(svg, "heatmap-cell").Single(cell => cell.Descendants().Any(element => (string?)element.Attribute("href") == "evidence/dc02.html#backup"));
        var linkedLabel = Assert.Single(linkedCell.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "data-label");
        Assert.Contains(linkedLabel.Ancestors(), owner => (string?)owner.Attribute("href") == "evidence/dc02.html#backup");
        Assert.All(labels, label => Assert.Contains(label.Ancestors(), owner => (string?)owner.Attribute("data-cfx-role") == "heatmap-cell"));
    }

    [Fact]
    public void ToPng_CellColoursMatchSvgGeometry() {
        var chart = CreateChart();
        var cell = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell")[1];
        var image = PngReader.Decode(chart.ToPng());
        var scale = image.Width / (double)chart.Options.Size.Width;
        var x = (int)((Number(cell, "x") + 6) * scale);
        var y = (int)((Number(cell, "y") + 6) * scale);
        var offset = (y * image.Width + x) * 4;
        Assert.True(Math.Abs(image.Pixels[offset] - Critical.R) <= 12 && Math.Abs(image.Pixels[offset + 1] - Critical.G) <= 12 && Math.Abs(image.Pixels[offset + 2] - Critical.B) <= 12);
        Assert.Equal(chart.ToPng(), CreateChart().ToPng());
        Assert.Equal(chart.ToSvg(), CreateChart().ToSvg());
    }

    [Theory]
    [InlineData("#dc01")]
    [InlineData("evidence/dc01.html")]
    [InlineData("../report.html?dc=DC01")]
    [InlineData("https://example.test/dc01")]
    [InlineData("mailto:ops@example.test")]
    public void Cell_SafeHref_IsAccepted(string href) => Assert.Equal(href, new ChartHeatmapCell("pass", href: href).Href);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(1)")]
    [InlineData("data:text/html,<b>x</b>")]
    [InlineData("vbscript:msgbox")]
    [InlineData("java\tscript:alert(1)")]
    [InlineData("")]
    public void Cell_UnsafeHref_IsRejected(string href) => Assert.Throws<ArgumentException>(() => new ChartHeatmapCell("pass", href: href));

    [Fact]
    public void Validation_RejectsStatelessCellsMixedRowsAndDuplicateCategories() {
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHeatmapCategoryRow("DC01", default(ChartHeatmapCell)));
        Assert.Throws<ArgumentException>(() => new ChartHeatmapCell(" "));

        var mixed = CreateChart().AddHeatmapRow("Numeric", new[] { 1d, 2d, 3d });
        Assert.Throws<InvalidOperationException>(() => mixed.ToSvg());

        var duplicate = CreateChart();
        duplicate.Options.StateCategories.Add(new ChartStateCategory("pass", "Again", Critical));
        Assert.Throws<InvalidOperationException>(() => duplicate.ToPng());
    }

    [Fact]
    public void ToSvg_TitleAndWrappingLegend_StayBelowColumnLabelsWithoutOverlap() {
        var categories = Enumerable.Range(0, 9).Select(index => new ChartStateCategory("s" + index.ToString(CultureInfo.InvariantCulture), "Category number " + index.ToString(CultureInfo.InvariantCulture), Pass)).ToArray();
        var chart = Chart.Create().WithSize(520, 360).WithLegendPosition(ChartLegendPosition.Bottom).WithXAxis("Checks").WithStateCategories(categories).WithXLabels("A", "B", "C")
            .AddHeatmapCategoryRow("DC01", new ChartHeatmapCell("s0"), new ChartHeatmapCell("s1"), new ChartHeatmapCell("s2"));
        var svg = XDocument.Parse(chart.ToSvg());
        var legendRows = ByRole(svg, "state-legend-swatch").Select(swatch => Number(swatch, "y")).Distinct().ToArray();
        Assert.True(legendRows.Length > 1, "Nine long labels should wrap onto several legend rows.");
        var titleY = Number(ByRole(svg, "heatmap-x-axis-title").Single(), "y");
        var lastCellBottom = ByRole(svg, "heatmap-cell").Max(cell => Number(cell, "y") + Number(cell, "height"));
        Assert.True(legendRows.Min() > titleY, "The category legend should sit below the x-axis title.");
        Assert.True(titleY > lastCellBottom);
        Assert.True(legendRows.Max() + ChartStateCategoryLegendHeightOfSwatch <= chart.Options.Size.Height);
    }

    [Fact]
    public void ToSvg_OnlyLinkedCellsAreTabStopsAndCellsExposeStateLabel() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        // A static cell is named for screen readers but is not a tab stop; a linked cell is reached through its link.
        Assert.All(cells, cell => Assert.Null(cell.Attribute("tabindex")));
        Assert.Equal("DC01, LDAP: Critical", (string?)cells[1].Attribute("aria-label"));
        Assert.NotEmpty(ByRole(svg, "heatmap-cell-link"));
        Assert.Equal("Not evaluated", (string?)cells[2].Attribute("data-cfx-meta-state"));
    }

    [Fact]
    public void ToSvg_UnregisteredKeyAndTrailingMask_FallBackAndKeepColumns() {
        var chart = Chart.Create().WithSize(520, 260).WithXLabels("A", "B", "C")
            .AddHeatmapCategoryRow("DC01", new List<ChartHeatmapCell> { new("pending"), new("pending") })
            .AddHeatmapCategoryRow("DC02", new ChartHeatmapCell("pending"), null, null);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Equal(chart.Options.Theme.MutedText.ToCss(), (string)ByRole(svg, "heatmap-cell")[0].RenderedAttribute("fill")!);
        Assert.Equal(3, ByRole(svg, "heatmap-column-label").Length);
        Assert.Equal(3, ByRole(svg, "heatmap-cell").Length);
    }

    private const double ChartStateCategoryLegendHeightOfSwatch = 10;

    [Theory]
    [InlineData(390)]
    [InlineData(720)]
    public void Render_ManyStyledCategories_BudgetLegendAndKeepCellsVisible(int width) {
        var states = Enumerable.Range(0, 40).Select(i => new ChartStateCategory("s" + i, "Long category " + i, Pass)).ToArray();
        var chart = Chart.Create().WithSize(width, 400).WithStateCategories(states).WithXLabels("A", "B")
            .AddHeatmapCategoryRow("Service", new ChartHeatmapCell("s0", "1", href: "#evidence"), new ChartHeatmapCell("s1"));
        chart.Options.LegendStyle.FontSize = 32;
        var svg = XDocument.Parse(chart.ToSvg());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var summary = Assert.Single(prepared.Regions, region => region.Id == "legend-overflow");
        Assert.Contains("more entries", summary.Label);
        Assert.NotEmpty(ByRole(svg, "legend-entry-omitted"));
        Assert.All(ByRole(svg, "heatmap-cell"), cell => Assert.True(Number(cell, "height") >= 18));
        Assert.All(ByRole(svg, "legend-label"), label => Assert.True(Number(label, "y") < 400));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void ToSvg_RepeatedTooltips_KeepDistinctStableCellIdentities() {
        var chart = Chart.Create().WithSize(390, 300).WithXLabels("A", "B")
            .AddHeatmapCategoryRow("Same", new ChartHeatmapCell("pass", tooltip: "Open evidence"), new ChartHeatmapCell("pass", tooltip: "Open evidence"))
            .AddHeatmapCategoryRow("Same", new ChartHeatmapCell("pass", tooltip: "Open evidence"), new ChartHeatmapCell("pass", tooltip: "Open evidence"));
        var cells = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell");
        var ids = cells.Select(cell => (string?)cell.Attribute("data-cfx-source-id")).ToArray();
        Assert.All(ids, id => Assert.False(string.IsNullOrEmpty(id)));
        Assert.Equal(4, ids.Distinct().Count());
        Assert.All(cells, cell => Assert.EndsWith(": pass. Open evidence", Title(cell), StringComparison.Ordinal));
        Assert.Equal(ids, ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell").Select(cell => (string?)cell.Attribute("data-cfx-source-id")).ToArray());
    }

    [Fact]
    public void ToPng_AutoMode_RetainsShortTextThatFitsDenseCategoricalCells() {
        var chart = Chart.Create().WithSize(560, 240)
            .AddHeatmapCategoryRow("Service", Enumerable.Range(0, 12).Select(_ => new ChartHeatmapCell("pass", "1")).ToArray());
        chart.Options.ShowAxes = false;
        chart.Options.ShowLegend = false;
        var cells = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell");
        Assert.All(cells, cell => Assert.InRange(Number(cell, "width"), 34, 45));
        Assert.Equal(12, ByRole(XDocument.Parse(chart.ToSvg()), "data-label").Length);
        var auto = chart.ToPng();
        chart.Options.HeatmapValueTextMode = ChartHeatmapValueTextMode.Always;
        Assert.Equal(chart.ToPng(), auto);
    }

    [Theory]
    [InlineData("1234", 28)]
    [InlineData("LONG COUNT", 13)]
    [InlineData("1", 80)]
    public void AutoMode_HidesCategoricalTextThatDoesNotFitAtConfiguredSize(string text, double fontSize) {
        var chart = Chart.Create().WithSize(560, 240)
            .AddHeatmapCategoryRow("Service", Enumerable.Range(0, 12).Select(_ => new ChartHeatmapCell("pass", text)).ToArray());
        chart.Options.ShowAxes = false;
        chart.Options.ShowLegend = false;
        chart.Options.DataLabelStyle.FontSize = fontSize;
        Assert.Empty(ByRole(XDocument.Parse(chart.ToSvg()), "data-label"));
        var auto = chart.ToPng();
        chart.Options.HeatmapValueTextMode = ChartHeatmapValueTextMode.Hidden;
        Assert.Equal(chart.ToPng(), auto);
    }

    [Fact]
    public void AutoMode_MeasuresPointLabelOverrideInBothRenderers() {
        var chart = Chart.Create().WithSize(560, 240)
            .AddHeatmapCategoryRow("Service", Enumerable.Range(0, 12).Select(_ => new ChartHeatmapCell("pass", "1")).ToArray());
        chart.Options.ShowAxes = false;
        chart.Options.ShowLegend = false;
        foreach (var index in Enumerable.Range(0, 12)) chart.Series[0].WithPointLabel(index, "LONG COUNT");
        Assert.Empty(ByRole(XDocument.Parse(chart.ToSvg()), "data-label"));
        var auto = chart.ToPng();
        chart.Options.HeatmapValueTextMode = ChartHeatmapValueTextMode.Hidden;
        Assert.Equal(chart.ToPng(), auto);
    }

    [Fact]
    public void AutoMode_UsesReadableMinimumForTinyConfiguredFonts() {
        var chart = Chart.Create().WithSize(560, 240)
            .AddHeatmapCategoryRow("Service", Enumerable.Range(0, 12).Select(_ => new ChartHeatmapCell("pass", "1")).ToArray());
        chart.Options.ShowAxes = false;
        chart.Options.ShowLegend = false;
        chart.Options.DataLabelStyle.FontSize = 2;
        Assert.All(ByRole(XDocument.Parse(chart.ToSvg()), "data-label"), label => Assert.True(Number(label, "font-size") >= 8));
        var tiny = chart.ToPng();
        chart.Options.DataLabelStyle.FontSize = 8;
        Assert.Equal(chart.ToPng(), tiny);
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Left)]
    [InlineData(ChartDataLabelPlacement.Right)]
    [InlineData(ChartDataLabelPlacement.Outside)]
    public void CategoricalCenteredLabels_DoNotReserveUnusedSideLanes(ChartDataLabelPlacement placement) {
        var chart = Chart.Create().WithSize(560, 240).WithDataLabels()
            .AddHeatmapCategoryRow("Service", Enumerable.Range(0, 12).Select(_ => new ChartHeatmapCell("pass", "1")).ToArray());
        chart.Options.ShowAxes = false;
        chart.Options.ShowLegend = false;
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Center;
        var centered = chart.ToSvg();
        var centeredPng = chart.ToPng();
        chart.Options.DataLabelPlacement = placement;
        Assert.Equal(centered, chart.ToSvg());
        Assert.Equal(centeredPng, chart.ToPng());
    }

    private static Chart CreateChart() => Chart.Create().WithSize(720, 320)
        .WithStateCategories(
            new ChartStateCategory("pass", "Passed", Pass),
            new ChartStateCategory("critical", "Critical", Critical),
            new ChartStateCategory("notEvaluated", "Not evaluated", Neutral, ChartStatePattern.Hatched))
        .WithXLabels("Replication", "LDAP", "Backup")
        .AddHeatmapCategoryRow("DC01", new ChartHeatmapCell("pass"), new ChartHeatmapCell("critical", "3", href: "#dc01-ldap"), new ChartHeatmapCell("notEvaluated"))
        .AddHeatmapCategoryRow("DC02", new ChartHeatmapCell("pass"), null, new ChartHeatmapCell("critical", "1", "Backup is 9 days old", "evidence/dc02.html#backup"));

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static string Title(XElement element) => element.Tooltip();

    private static double Number(XElement element, string attribute) => double.Parse((string)element.RenderedAttribute(attribute)!, CultureInfo.InvariantCulture);
}
