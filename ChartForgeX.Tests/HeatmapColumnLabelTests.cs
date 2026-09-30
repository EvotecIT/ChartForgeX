using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
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
        var chart = Matrix().WithXAxisLabelAngle(-45);
        var svg = XDocument.Parse(chart.ToSvg());
        var labels = ByRole(svg, "heatmap-column-label");
        var tickSize = chart.Options.Theme.TickLabelFontSize;

        Assert.Equal(Checks, labels.Select(label => label.Value).ToArray());
        Assert.All(labels, label => {
            Assert.StartsWith("rotate(-45 ", (string?)label.Attribute("transform"), StringComparison.Ordinal);
            Assert.Equal(tickSize, Number(label, "font-size"));
            Assert.Equal("end", (string?)label.Attribute("text-anchor"));
        });

        var cellBottom = ByRole(svg, "heatmap-cell").Max(cell => Number(cell, "y") + Number(cell, "height"));
        Assert.All(labels, label => Assert.True(Number(label, "y") > cellBottom, "Labels hang below the cells."));
        // The longest label, drawn at 45 degrees, must end above the legend that follows it.
        var legendTop = ByRole(svg, "state-legend-swatch").Min(swatch => Number(swatch, "y"));
        var drop = Math.Sin(Math.PI / 4) * Checks.Max(check => check.Length) * tickSize * 0.58;
        Assert.True(labels.Max(label => Number(label, "y")) + drop <= legendTop, "Rotated labels must not run into the legend.");
        Assert.NotEqual(Matrix().ToPng(), chart.ToPng());
    }

    [Theory]
    [InlineData(45)]
    [InlineData(-45)]
    public void ToSvg_RotatedColumnLabels_LeaveRoomForTheOutermostLabel(double angle) {
        // Labels slant towards one side; the plot moves in there so the outermost label is drawn in full.
        var chart = Matrix().WithXAxisLabelAngle(angle);
        var svg = XDocument.Parse(chart.ToSvg());
        var labels = ByRole(svg, "heatmap-column-label");
        Assert.Equal(Checks, labels.Select(label => label.Value).ToArray());
        Assert.All(labels, label => Assert.Equal(angle < 0 ? "end" : "start", (string?)label.Attribute("text-anchor")));
        Assert.NotEqual(Matrix().ToPng(), chart.ToPng());
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
        Assert.All(labels, label => Assert.Null(label.Attribute("transform")));
        Assert.Contains(labels, label => Number(label, "font-size") < 12 || label.Value.EndsWith("…", StringComparison.Ordinal) || label.Value.EndsWith("...", StringComparison.Ordinal));
    }

    [Fact]
    public void ToSvg_RotatedLabelsOnANumericHeatmap_PutTheScaleBelowThem() {
        var chart = Chart.Create().WithSize(520, 360).WithXAxisLabelAngle(60).WithXLabels(Checks)
            .AddHeatmapRow("DC01", Enumerable.Range(0, Checks.Length).Select(i => (double)(i * 7 % 10)).ToArray())
            .AddHeatmapRow("DC02", Enumerable.Range(0, Checks.Length).Select(i => (double)(i * 3 % 10)).ToArray());
        var svg = XDocument.Parse(chart.ToSvg());
        var labels = ByRole(svg, "heatmap-column-label");
        Assert.All(labels, label => Assert.StartsWith("rotate(60 ", (string?)label.Attribute("transform"), StringComparison.Ordinal));
        var tickSize = chart.Options.Theme.TickLabelFontSize;
        var drop = Math.Sin(Math.PI / 3) * Math.Min(120, Checks.Max(check => check.Length) * tickSize * 0.58);
        var scaleTop = ByRole(svg, "heatmap-scale-step").Min(step => Number(step, "y"));
        Assert.True(labels.Max(label => Number(label, "y")) + drop <= scaleTop + 1, "The scale sits below the rotated labels.");
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

    private static Chart Matrix() {
        var chart = Chart.Create().WithSize(520, 380)
            .WithStateCategories(new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52")), new ChartStateCategory("fail", "Failed", ChartColor.FromHex("#d4302f")))
            .WithXLabels(Checks);
        for (var row = 0; row < 4; row++) {
            chart.AddHeatmapCategoryRow("DC0" + (row + 1).ToString(CultureInfo.InvariantCulture), Checks.Select((_, column) => (ChartHeatmapCell?)new ChartHeatmapCell((row + column) % 5 == 0 ? "fail" : "pass")).ToArray());
        }

        return chart;
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);
}
