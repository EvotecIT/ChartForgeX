using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
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
        Assert.Equal(new[] { Pass.ToCss(), Critical.ToCss(), Neutral.ToCss() }, cells.Take(3).Select(cell => (string)cell.Attribute("fill")!).ToArray());
        Assert.Equal("DC01, LDAP: Critical", (string)cells[1].Attribute("aria-label")!);
        Assert.Equal("Backup is 9 days old", Title(cells[4]));

        var links = ByRole(svg, "heatmap-cell-link");
        Assert.Equal(new[] { "#dc01-ldap", "evidence/dc02.html#backup" }, links.Select(link => (string)link.Attribute("href")!).ToArray());
        Assert.Equal("heatmap-cell", (string)links[0].Elements().Single().Attribute("data-cfx-role")!);

        Assert.Single(ByRole(svg, "heatmap-cell-hatch"));
        Assert.Single(ByRole(svg, "state-segment-hatch"));
        Assert.Equal(new[] { "Passed", "Critical", "Not evaluated" }, ByRole(svg, "state-legend-label").Select(label => label.Value).ToArray());
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
    public void Validation_RejectsEmptyRowsMixedRowsAndDuplicateCategories() {
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHeatmapCategoryRow("DC01", null, null));
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
        var chart = Chart.Create().WithSize(520, 360).WithXAxis("Checks").WithStateCategories(categories).WithXLabels("A", "B", "C")
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
    public void ToSvg_LinkedCells_UseOneTabStopAndExposeStateLabel() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        Assert.Null(cells[1].Attribute("tabindex"));
        Assert.Equal("0", (string?)cells[0].Attribute("tabindex"));
        Assert.Equal("Not evaluated", (string?)cells[2].Attribute("data-cfx-meta-state"));
    }

    [Fact]
    public void ToSvg_UnregisteredKeyAndTrailingMask_FallBackAndKeepColumns() {
        var chart = Chart.Create().WithSize(520, 260).WithXLabels("A", "B", "C")
            .AddHeatmapCategoryRow("DC01", new List<ChartHeatmapCell> { new("pending"), new("pending") })
            .AddHeatmapCategoryRow("DC02", new ChartHeatmapCell("pending"), null, null);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Equal(chart.Options.Theme.MutedText.ToCss(), (string)ByRole(svg, "heatmap-cell")[0].Attribute("fill")!);
        Assert.Equal(3, ByRole(svg, "heatmap-column-label").Length);
        Assert.Equal(3, ByRole(svg, "heatmap-cell").Length);
    }

    private const double ChartStateCategoryLegendHeightOfSwatch = 10;

    private static Chart CreateChart() => Chart.Create().WithSize(720, 320)
        .WithStateCategories(
            new ChartStateCategory("pass", "Passed", Pass),
            new ChartStateCategory("critical", "Critical", Critical),
            new ChartStateCategory("notEvaluated", "Not evaluated", Neutral, hatched: true))
        .WithXLabels("Replication", "LDAP", "Backup")
        .AddHeatmapCategoryRow("DC01", new ChartHeatmapCell("pass"), new ChartHeatmapCell("critical", "3", href: "#dc01-ldap"), new ChartHeatmapCell("notEvaluated"))
        .AddHeatmapCategoryRow("DC02", new ChartHeatmapCell("pass"), null, new ChartHeatmapCell("critical", "1", "Backup is 9 days old", "evidence/dc02.html#backup"));

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static string Title(XElement element) => element.Elements().Single(child => child.Name.LocalName == "title").Value;

    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);
}
