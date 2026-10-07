using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Column labels under matrix and categorical heatmaps follow <see cref="ChartOptions.XAxisLabelAngle"/>.</summary>
public sealed class HeatmapColumnLabelTests {
    private static readonly string[] Checks = {
        "Replication health", "SYSVOL sharing", "DNS scavenging", "Time source", "LDAP signing", "Kerberos armoring",
        "Certificate expiry", "Backup age", "Service accounts", "Disk space", "Event log size", "Secure channel"
    };

    [Fact]
    public void ToSvg_RotatedColumnLabels_KeepTheTickSizeAndFullTextInsideTheChart() {
        var chart = MatrixWithPinnedFont().WithXAxisLabelAngle(-45);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var labels = ByRole(svg, "heatmap-column-label");
        var tickSize = chart.Options.Theme.TickLabelFontSize;

        Assert.Equal(Checks, labels.Select(label => label.Value).ToArray());
        Assert.All(labels, label => {
            Assert.StartsWith("rotate(-45 ", (string?)label.RenderedAttribute("transform"), StringComparison.Ordinal);
            Assert.Equal(tickSize, Number(label, "font-size"));
        });
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "heatmap-column-label"),
            node => Assert.Equal(TextAlignment.Right, node.Alignment));

        var cellBottom = ByRole(svg, "heatmap-cell").Max(cell => Number(cell, "y") + Number(cell, "height"));
        Assert.All(labels, label => Assert.True(Number(label, "y") > cellBottom, "Labels hang below the cells."));
        // The longest label, drawn at 45 degrees, must end above the legend that follows it.
        var legendTop = ByRole(svg, "state-legend-swatch").Min(swatch => Number(swatch, "y"));
        Assert.All(ColumnGeometry(prepared), label => Assert.All(label.Corners,
            corner => Assert.True(corner.Y <= legendTop + 1, "Rotated labels must not run into the legend.")));
        Assert.NotEqual(MatrixWithPinnedFont().ToPng(), chart.ToPng());
    }

    [Theory]
    [InlineData(45)]
    [InlineData(-45)]
    public void ToSvg_RotatedColumnLabels_LeaveRoomForTheOutermostLabel(double angle) {
        // Labels slant towards one side; the plot moves in there so the outermost label is drawn in full.
        var chart = MatrixWithPinnedFont().WithXAxisLabelAngle(angle);
        var svg = XDocument.Parse(chart.ToSvg());
        var labels = ByRole(svg, "heatmap-column-label");
        Assert.Equal(Checks, labels.Select(label => label.Value).ToArray());
        Assert.All(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "heatmap-column-label"),
            node => Assert.Equal(angle < 0 ? TextAlignment.Right : TextAlignment.Left, node.Alignment));
        Assert.NotEqual(MatrixWithPinnedFont().ToPng(), chart.ToPng());
    }

    [Theory]
    [InlineData(45)]
    [InlineData(-45)]
    public void DefaultFontColumnLabelsRetainCompleteSemanticsAndFitTheirMeasuredBands(double angle) {
        // Host fallbacks can require shortening in the bounded 520x380 panel. Keep that
        // default path covered without treating one font's advances as a portable text budget.
        var chart = Matrix().WithXAxisLabelAngle(angle);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(Checks, prepared.Regions.Where(region => region.Role == "heatmap-column-label").Select(region => region.Label).ToArray());
        var geometry = ColumnGeometry(prepared);
        Assert.Equal(Checks.Length, geometry.Count);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(geometry.Select(label => Assert.Single(label.Text.Text.Lines).Text),
            ByRole(svg, "heatmap-column-label").Select(label => label.Value));
        for (var column = 0; column < Checks.Length; column++) {
            var displayed = Assert.Single(geometry[column].Text.Text.Lines).Text;
            var prefix = displayed.TrimEnd('.', '…');
            Assert.True(prefix.Length >= Checks[column].Split(' ')[0].Length,
                "A fitted caption must retain at least its complete first word.");
            Assert.StartsWith(prefix, Checks[column], StringComparison.Ordinal);
        }
        var legendTop = ByRole(svg, "state-legend-swatch").Min(swatch => Number(swatch, "y"));
        Assert.All(geometry, label => {
            Assert.Equal(chart.Options.Theme.TickLabelFontSize, label.Text.Text.Size);
            Assert.All(label.Corners, corner => {
                Assert.InRange(corner.X, chart.Options.Padding.Left - .001, prepared.Size.Width - chart.Options.Padding.Right + .001);
                Assert.True(corner.Y <= legendTop + 1, "Measured column labels must not overlap the legend.");
            });
        });
        var cells = ByRole(svg, "heatmap-cell");
        Assert.All(cells, cell => Assert.True(Number(cell, "height") >= 12));
        Assert.True(cells.Max(cell => Number(cell, "x") + Number(cell, "width")) - cells.Min(cell => Number(cell, "x"))
            >= chart.Options.Size.Width / 3, "Column labels must retain a substantial cell plot.");
    }

    [Fact]
    public void ToSvg_PositiveColumnAnglesReserveTheRightEdgeIndependentlyOfWideRowLabels() {
        // Pin this regression's font so the wide left gutter reproduces the old right-edge
        // shortage independently of host fonts. The ordinary default-font cases remain above.
        var chart = MatrixWithPinnedFont("Production domain controller ").WithXAxisLabelAngle(45);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(Checks, ByRole(svg, "heatmap-column-label").Select(label => label.Value).ToArray());
        Assert.Equal(Checks, ColumnGeometry(prepared).Select(label => Assert.Single(label.Text.Text.Lines).Text).ToArray());
        var cells = ByRole(svg, "heatmap-cell");
        var plotLeft = cells.Min(cell => Number(cell, "x"));
        var plotRight = cells.Max(cell => Number(cell, "x") + Number(cell, "width"));
        Assert.True(plotLeft > chart.Options.Size.Width * .25, "The fixture must reserve a substantial left row-label gutter.");
        Assert.True(plotRight - plotLeft >= chart.Options.Size.Width / 3, "Column labels must retain a substantial cell plot.");
        Assert.All(cells, cell => Assert.True(Number(cell, "height") >= 12));
        var legendTop = ByRole(svg, "state-legend-swatch").Min(swatch => Number(swatch, "y"));
        Assert.All(ColumnGeometry(prepared), label => Assert.All(label.Corners, corner => {
            Assert.InRange(corner.X, chart.Options.Padding.Left - .001, prepared.Size.Width - chart.Options.Padding.Right + .001);
            Assert.True(corner.Y <= legendTop + 1, "Complete column labels must remain above the legend.");
        }));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void ToSvg_ShortRotatedLabels_AreNotThinned() {
        var chart = Chart.Create().WithSize(520, 300).WithXAxisLabelAngle(-20)
            .WithXLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")
            .AddHeatmapRow("Changes", Enumerable.Range(0, 12).Select(i => (double)i).ToArray());
        Assert.Equal(12, ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-column-label").Length);
    }

    [Fact]
    public void ToSvg_UnrotatedColumnLabels_StillShrinkToTheirColumn() {
        var svg = XDocument.Parse(Matrix().ToSvg());
        var labels = ByRole(svg, "heatmap-column-label");
        Assert.All(labels, label => Assert.Null(label.RenderedAttribute("transform")));
        Assert.Contains(labels, label => Number(label, "font-size") < 12 || label.Value.EndsWith("…", StringComparison.Ordinal) || label.Value.EndsWith("...", StringComparison.Ordinal));
    }

    [Fact]
    public void ToSvg_RotatedLabelsOnANumericHeatmap_PutTheScaleBelowThem() {
        // Leave space for the label band measured from the installed font, including wider Linux/macOS fallbacks.
        var chart = Chart.Create().WithSize(520, 380).WithXAxisLabelAngle(60).WithXLabels(Checks)
            .AddHeatmapRow("DC01", Enumerable.Range(0, Checks.Length).Select(i => (double)(i * 7 % 10)).ToArray())
            .AddHeatmapRow("DC02", Enumerable.Range(0, Checks.Length).Select(i => (double)(i * 3 % 10)).ToArray());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var labels = ByRole(svg, "heatmap-column-label");
        Assert.All(labels, label => Assert.StartsWith("rotate(60 ", (string?)label.RenderedAttribute("transform"), StringComparison.Ordinal));
        var scaleTop = ByRole(svg, "heatmap-scale-step").Min(step => Number(step, "y"));
        Assert.All(ColumnGeometry(prepared), label => Assert.All(label.Corners,
            corner => Assert.True(corner.Y <= scaleTop + 1, "The scale sits below the rotated labels.")));
        Assert.True(scaleTop + 8 <= chart.Options.Size.Height);
        // The chart-wide x-axis band is not reserved a second time on top of the heatmap's own label band.
        Assert.All(ByRole(svg, "heatmap-cell"), cell => Assert.True(Number(cell, "height") >= 12, "Rows keep a readable height."));
        Assert.True(chart.ToPng().Length > 64);
    }

    [Fact]
    public void ToSvg_ManyColumnsRotated_ThinsLabelsSoTheyDoNotOverlap() {
        var chart = Chart.Create().WithSize(480, 320).WithXAxisLabelAngle(-30)
            .WithXLabels(Enumerable.Range(1, 60).Select(i => "Check " + i.ToString(CultureInfo.InvariantCulture)).ToArray())
            .AddHeatmapRow("DC01", Enumerable.Range(0, 60).Select(i => (double)(i % 10)).ToArray());
        var labels = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-column-label");
        Assert.InRange(labels.Length, 2, 59);
        var xs = labels.Select(label => Number(label, "x")).ToArray();
        var pitch = xs.Zip(xs.Skip(1), (a, b) => b - a).Min();
        Assert.True(pitch * Math.Sin(Math.PI / 6) >= chart.Options.Theme.TickLabelFontSize * 0.9, "Neighbouring rotated labels keep a line of text between them.");
    }

    [Theory]
    [InlineData(5)] [InlineData(-5)]
    [InlineData(45)] [InlineData(-45)]
    [InlineData(80)] [InlineData(-80)]
    public void ToSvg_LongRotatedLabels_PreserveReadableCellsAndFitBothCanvasDimensions(double angle) {
        var longLabel = new string('W', 100);
        var chart = Chart.Create().WithSize(520, 380).WithXAxisLabelAngle(angle)
            .WithTickLabelStyle(style => style.WithWeight("700"))
            .WithXLabels(longLabel, longLabel, longLabel)
            .AddHeatmapRow("Row", new[] { 1d, 2d, 3d });
        var svg = XDocument.Parse(chart.ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        Assert.Equal(3, cells.Length);
        var plotLeft = cells.Min(cell => Number(cell, "x"));
        var plotRight = cells.Max(cell => Number(cell, "x") + Number(cell, "width"));
        Assert.True(plotRight - plotLeft >= chart.Options.Size.Width / 3, "Long column labels must leave a substantial width for the cells.");
        Assert.All(cells, cell => Assert.True(Number(cell, "height") >= 12));

        var labels = ByRole(svg, "heatmap-column-label");
        Assert.NotEmpty(labels);
        var scaleTop = ByRole(svg, "heatmap-scale-step").Min(step => Number(step, "y"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "heatmap-column-label" && region.Label == longLabel));
        Assert.All(ColumnGeometry(prepared), label => {
            Assert.All(label.Text.Text.Lines, line => Assert.True(line.Text.Length < longLabel.Length, "The fixture must require shortening."));
            Assert.All(label.Corners, corner => Assert.InRange(corner.X, -.001, prepared.Size.Width + .001));
            Assert.All(label.Corners, corner => Assert.True(corner.Y <= scaleTop + 1, "Measured rotated text must remain above the numeric scale."));
        });
    }

    private static Chart Matrix(string rowPrefix = "DC0") {
        var chart = Chart.Create().WithSize(520, 380).WithLegendPosition(ChartLegendPosition.Bottom)
            .WithStateCategories(new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52")), new ChartStateCategory("fail", "Failed", ChartColor.FromHex("#d4302f")))
            .WithXLabels(Checks);
        for (var row = 0; row < 4; row++) {
            chart.AddHeatmapCategoryRow(rowPrefix + (row + 1).ToString(CultureInfo.InvariantCulture), Checks.Select((_, column) => (ChartHeatmapCell?)new ChartHeatmapCell((row + column) % 5 == 0 ? "fail" : "pass")).ToArray());
        }

        return chart;
    }

    private static Chart MatrixWithPinnedFont(string rowPrefix = "DC0") {
        // Full-text assertions need a stable advance budget; default-font fitting has its own
        // measured geometry and complete semantic-caption proof above.
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font), "The existing Carlito example fixture must be available.");
        return Matrix(rowPrefix).WithPngFont(font);
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static IReadOnlyList<(VisualSceneText Text, ChartPoint[] Corners)> ColumnGeometry(PreparedVisual prepared) {
        var labels = new List<(VisualSceneText, ChartPoint[])>();
        var transforms = new Stack<VisualSceneTransform>();
        var transform = VisualSceneTransform.Identity;
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) {
                transforms.Push(transform);
                if (group.Rotation.HasValue) transform = transform.Rotate(group.Rotation.Value);
                if (group.Translation.HasValue) transform = transform.Translate(group.Translation.Value);
            } else if (node is VisualSceneEndGroup) transform = transforms.Pop();
            else if (node is VisualSceneText text && text.Role == "heatmap-column-label") {
                var line = Assert.Single(text.Text.Lines);
                var left = text.LineLeft(line); var top = text.Baseline - text.Text.Ascent;
                var corners = new[] { new ChartPoint(left, top), new ChartPoint(left + line.Width, top),
                    new ChartPoint(left, top + text.Text.Metrics.Height), new ChartPoint(left + line.Width, top + text.Text.Metrics.Height) }
                    .Select(transform.Apply).ToArray();
                labels.Add((text, corners));
            }
        }
        Assert.NotEmpty(labels);
        return labels;
    }

    private static double Number(XElement element, string attribute) => double.Parse((string)element.RenderedAttribute(attribute)!, CultureInfo.InvariantCulture);
}
