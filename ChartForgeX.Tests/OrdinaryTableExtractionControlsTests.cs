using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class OrdinaryTableExtractionControlsTests {
    [Theory]
    [InlineData("default", 390, false)] [InlineData("default", 390, true)]
    [InlineData("default", 760, false)] [InlineData("default", 760, true)]
    [InlineData("rich", 390, false)] [InlineData("rich", 390, true)]
    [InlineData("rich", 760, false)] [InlineData("rich", 760, true)]
    [InlineData("bare", 390, false)] [InlineData("bare", 390, true)]
    [InlineData("bare", 760, false)] [InlineData("bare", 760, true)]
    public void OrdinaryTableOptionsAndRichCellsProduceCapturedNativeOutputWithoutChangingSource(string variant, int width, bool dark) {
        var table = Create(variant, width, dark); var before = Source(table);
        var svg = table.ToSvg("ordinary-table-control"); var png = table.ToPng();
        var labelSvg = new SvgFactBlockRenderer().RenderLabelScene(table).ToSvg();
        var document = XDocument.Parse(svg); var nodes = document.Descendants().ToArray();
        var words = nodes.Where(node => node.Name.LocalName == "text").Select(node => node.Value).ToArray();
        Assert.Contains("Publication facts", words); Assert.Contains("Source values and independent cell presentations", words);
        Assert.Contains("API", words); Assert.Contains("Queue", words);
        Assert.Equal(variant != "bare", nodes.Any(node => Role(node) == "table-header"));
        Assert.DoesNotContain(nodes, node => node.Name.LocalName is "script" or "image" or "foreignObject");
        if (variant == "rich") {
            Assert.Contains(nodes, node => Role(node) == "visual-badge" && (string?)node.Attribute("data-cfx-style") == "Outline");
            Assert.Equal(3, nodes.Count(node => Role(node) == "table-cell-mini-bar"));
            Assert.Equal(2, nodes.Count(node => Role(node) == "table-cell-sparkline"));
            Assert.Contains(nodes, node => Role(node) == "table-status");
            Assert.Contains("Ready", words);
        } else {
            Assert.Contains("0", words); Assert.Contains("False", words);
            Assert.DoesNotContain(nodes, node => Role(node) is "visual-badge" or "table-cell-microvisual");
        }
        var image = RasterImageDecoder.Decode(png); Assert.Equal(width, image.Width); Assert.Equal(280, image.Height);
        Assert.Contains(XDocument.Parse(labelSvg).Descendants(), node => node.Name.LocalName == "text");
        Assert.Equal(before, Source(table));
        var output = Environment.GetEnvironmentVariable("CFX_TABLE_BASELINE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "ordinary-table-" + variant + "-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png);
            File.WriteAllText(stem + ".label-scene.svg", labelSvg);
            File.WriteAllText(stem + ".geometry.json", Geometry(document)); File.WriteAllText(stem + ".source.json", before);
        }
    }

    private static ChartTable Create(string variant, int width, bool dark) {
        var table = ChartTable.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(width, 280).WithPngOutputScale(1).WithTitle("Publication facts")
            .WithSubtitle("Source values and independent cell presentations");
        if (variant == "rich") {
            table.WithDenseMode(width == 390).AddColumn("Name", width: 70).AddColumn("State", width: 80)
                .AddColumn("Load", TextAlignment.Right).AddColumn("History", TextAlignment.Center)
                .WithStatusColumn(1).AddRow("API", "Ready", "bars", "trend").AddRow("Queue", "Warning", 0, false);
            table.ConfigureRow(0, row => {
                row.Background = ChartColor.FromHex("#18394A"); row.Foreground = ChartColor.FromHex("#F2F6FA");
                row.Cells[1].WithBadge("Ready", VisualStatus.Positive, style: VisualBadgeStyle.Outline);
                row.Cells[2].WithMiniBars(new[] { 0d, 2, 4 }, minimum: 0, maximum: 4);
                row.Cells[3].WithSparkline(new SparklineData(new double?[] { 0, null, 4, 2 }, minimum: 0, maximum: 4));
            });
            table.ConfigureRow(1, row => { row.Cells[1].Status = VisualStatus.Warning; row.Cells[2].Alignment = TextAlignment.Center;
                row.Cells[2].Foreground = ChartColor.FromHex("#245C41"); row.Cells[2].Background = ChartColor.FromHex("#D8F0E2"); });
        } else {
            table.WithColumns("Name", "Value").AddRow("API", 0).AddRow("Queue", false).AddRow("Missing", null).AddRow("Empty", "");
            if (variant == "bare") table.WithHeader(false).WithRowStriping(false).WithDenseMode().WithCard(false).WithTransparentBackground().WithPadding(12);
        }
        return table;
    }
    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static string Geometry(XDocument document) => JsonSerializer.Serialize(document.Descendants()
        .Where(node => node.Name.LocalName is "rect" or "circle" or "polyline" or "path" or "text")
        .Select(node => new { kind = node.Name.LocalName, text = node.Name.LocalName == "text" ? node.Value : null,
            attributes = node.Attributes().Where(attribute => attribute.Name.LocalName != "id")
                .OrderBy(attribute => attribute.Name.LocalName).Select(attribute => new { name = attribute.Name.LocalName, value = attribute.Value }) }));
    private static string Source(ChartTable table) => JsonSerializer.Serialize(new { table.Dense, table.ShowHeader, table.RowStriping, table.StatusColumnIndex,
        columns = table.Columns.Select(column => new { column.Header, column.Alignment, column.Width, column.Format }),
        rows = table.Rows.Select(row => new { row.Background, row.Foreground, cells = row.Cells.Select(cell => new {
            cell.Text, cell.Alignment, cell.Foreground, cell.Background, cell.Status, cell.BadgeText, cell.BadgeStyle, cell.BadgeStatus, cell.BadgeColor,
            cell.MicroVisualKind, cell.MicroVisualMinimum, cell.MicroVisualMaximum, cell.MicroVisualColor, cell.MicroVisualValues,
            slots = cell.SparklineData?.Values }) }) });
}
