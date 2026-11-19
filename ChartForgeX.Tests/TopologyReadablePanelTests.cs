using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

/// <summary>
/// Readable dense topologies keep their text readable where a report shows them. A host scales the SVG down to its
/// width, so group panels narrow or wrap instead of making the canvas wider, and small captions keep the theme's
/// minimum readable size.
/// </summary>
public sealed class TopologyReadablePanelTests {
    private const double Gutter = 44;
    private readonly ITestOutputHelper _output;

    public TopologyReadablePanelTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("critical-groups")]
    [InlineData("site-overview")]
    [InlineData("controller-detail")]
    public void ReportShapes_KeepEveryLabelReadableAtTheReportWidth(string fixture) {
        var (prepared, svg) = Render(Build(fixture));
        var text = RenderedText.Measure(svg, DirectoryScaleTopologyFixture.ReportWidth);
        _output.WriteLine(fixture + ": " + text);

        Assert.True(text.Width <= DirectoryScaleTopologyFixture.ReportWidth + .5, "The canvas is wider than the report. " + text);
        Assert.True(text.BelowMinimum == 0, "Labels are smaller than the readable minimum. " + text);
        var groups = prepared.Analyze().Groups;
        foreach (var a in groups) foreach (var b in groups) {
            if (string.CompareOrdinal(a.Id, b.Id) >= 0) continue;
            Assert.False(a.Bounds.X < b.Bounds.Right && b.Bounds.X < a.Bounds.Right && a.Bounds.Y < b.Bounds.Bottom && b.Bounds.Y < a.Bounds.Bottom,
                a.Id + " overlaps " + b.Id);
        }
    }

    [Fact]
    public void FiveDomainPanels_WrapIntoTwoColumnsInsteadOfShrinking() {
        var groups = Render(Build("critical-groups")).Prepared.Analyze().Groups;

        Assert.Equal(5, groups.Count);
        Assert.Equal(2, groups.Select(group => Math.Round(group.Bounds.X)).Distinct().Count());
        Assert.Equal(3, groups.Select(group => Math.Round(group.Bounds.Y)).Distinct().Count());
        Assert.Single(groups.Select(group => Math.Round(group.Bounds.Width)).Distinct());
    }

    [Fact]
    public void ThreeRegionOverview_StaysInOneRowWithNarrowerPanels() {
        var (prepared, svg) = Render(Build("site-overview"));
        var report = prepared.Analyze();

        Assert.Equal(3, report.Groups.Count);
        Assert.Single(report.Groups.Select(group => Math.Round(group.Bounds.Y)).Distinct());
        // Twenty sites per region take three columns instead of four.
        foreach (var group in report.Groups) {
            var columns = report.Nodes.Where(node => group.Bounds.X <= node.Bounds.X && node.Bounds.Right <= group.Bounds.Right)
                .Select(node => Math.Round(node.Bounds.X)).Distinct().Count();
            Assert.Equal(3, columns);
        }
        var edges = Regex.Matches(svg, "data-cfx-status=\"(?<status>\\w+)\"[^>]*?data-edge-emphasis=\"(?<emphasis>\\w+)\"")
            .Select(match => (Status: match.Groups["status"].Value, Emphasis: match.Groups["emphasis"].Value)).ToArray();
        Assert.Contains(edges, edge => edge.Status == "Healthy");
        Assert.All(edges.Where(edge => edge.Status == "Healthy"), edge => Assert.Equal("Subtle", edge.Emphasis));
        Assert.Contains(edges, edge => edge.Status == "Critical");
        Assert.All(edges.Where(edge => edge.Status != "Healthy"), edge => Assert.Equal("Normal", edge.Emphasis));
    }

    [Fact]
    public void ControllerDetail_PacksSitePanelsIntoFullColumns() {
        var groups = Render(Build("controller-detail")).Prepared.Analyze().Groups;
        var columns = groups.GroupBy(group => Math.Round(group.Bounds.X)).OrderBy(column => column.Key).ToArray();

        Assert.True(columns.Length >= 4, "Site panels use " + columns.Length + " columns.");
        Assert.Single(groups.Select(group => Math.Round(group.Bounds.Width)).Distinct());
        var top = groups.Min(group => group.Bounds.Y);
        foreach (var column in columns) {
            // Each panel starts at the top or one gutter below the panel above it, so no column leaves a hole.
            var stacked = column.OrderBy(group => group.Bounds.Y).ToArray();
            Assert.Equal(top, stacked[0].Bounds.Y, 3);
            for (var i = 1; i < stacked.Length; i++) Assert.Equal(stacked[i - 1].Bounds.Bottom + Gutter, stacked[i].Bounds.Y, 3);
        }
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    public void ThemeMinimum_RaisesEveryCaptionToIt(double minimum) {
        // 14 px is above the 12 px data-label size, so captions grow rather than only stop shrinking.
        var theme = VisualTheme.Graphite().WithMinimumReadableTextSize(minimum);
        var (prepared, svg) = Render(Build("controller-detail").WithTheme(theme));
        var text = RenderedText.Measure(svg, DirectoryScaleTopologyFixture.ReportWidth, minimum);

        Assert.Equal(11, VisualTheme.Graphite().Typography.MinimumReadableTextSize);
        Assert.Equal(minimum, theme.Typography.MinimumReadableTextSize);
        Assert.True(text.BelowMinimum == 0, text.ToString());
        Assert.Empty(prepared.Analyze().Collisions);
    }

    [Fact]
    public void ThemeMinimum_SurvivesAJsonRoundTripAndDefaultsStayUnwritten() {
        var theme = VisualTheme.Graphite().WithMinimumReadableTextSize(13);

        Assert.Equal(13, VisualTheme.FromJson(theme.ToJson()).Typography.MinimumReadableTextSize);
        Assert.DoesNotContain("minimumReadableTextSize", VisualTheme.Graphite().ToJson(), StringComparison.Ordinal);
        Assert.Equal(11, VisualTheme.FromJson(VisualTheme.Graphite().ToJson()).Typography.MinimumReadableTextSize);
    }

    [Fact]
    public void PinnedPanel_KeepsAutomaticPanelsOutOfItsArea() {
        var chart = Build("controller-detail");
        var pinned = chart.Groups[0];
        pinned.X = 24; pinned.Y = 24; pinned.Width = 300; pinned.Height = 400;
        var groups = Render(chart, save: false).Prepared.Analyze().Groups;

        var area = Assert.Single(groups, group => group.Id == pinned.Id).Bounds;
        Assert.All(groups.Where(group => group.Id != pinned.Id), group => Assert.False(
            group.Bounds.X < area.Right && area.X < group.Bounds.Right && group.Bounds.Y < area.Bottom && area.Y < group.Bounds.Bottom,
            group.Id + " overlaps the pinned panel."));
    }

    [Fact]
    public void UnequalPanels_WrapWhenTheEqualColumnRowWouldOverflow() {
        // One wide and one narrow panel: their own widths fit 1,000 px, but an equal-column row would not.
        var chart = TopologyChart.Create().WithId("unequal").WithViewport(1000, 600, 24)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .WithRenderOptions(new TopologyRenderOptions { ReadableDenseLayout = true, NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeLegend = false });
        chart.AddAutoGroup("wide", "Wide").AddAutoGroup("narrow", "Narrow");
        for (var i = 0; i < 4; i++) chart.AddAutoNode("w" + i, "Wide " + i, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "wide", width: 64, height: 40);
        chart.AddAutoNode("n0", "Narrow", TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "narrow", width: 64, height: 40);
        chart.Groups[0].Width = 700;
        var (prepared, svg) = Render(chart, save: false);

        Assert.True(RenderedText.Measure(svg, 1000).Width <= 1000.5);
        Assert.Equal(2, prepared.Analyze().Groups.Select(group => Math.Round(group.Bounds.Y)).Distinct().Count());
    }

    [Fact]
    public void LayoutsWithoutReadableDensity_KeepTheirCaptionSizes() {
        var chart = Build("controller-detail");
        var options = chart.DefaultRenderOptions!.Clone();
        options.ReadableDenseLayout = false;
        var text = RenderedText.Measure(Render(chart.WithRenderOptions(options), save: false).Svg, double.MaxValue);

        Assert.True(text.Smallest < 11, text.ToString());
    }

    internal static TopologyChart Build(string fixture) => fixture switch {
        "critical-groups" => DirectoryScaleTopologyFixture.CriticalGroups(),
        "site-overview" => DirectoryScaleTopologyFixture.SiteOverview(),
        "controller-detail" => DirectoryScaleTopologyFixture.ControllerDetail(),
        _ => throw new ArgumentOutOfRangeException(nameof(fixture), fixture, "Unknown fixture.")
    };

    private static (PreparedTopology Prepared, string Svg) Render(TopologyChart chart, bool save = true) {
        var prepared = chart.Prepare();
        var svg = prepared.ToSvg();
        var directory = Environment.GetEnvironmentVariable("CFX_TOPOLOGY_FIXTURE_OUTPUT");
        if (save && !string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, chart.Id + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(directory, chart.Id + ".png"), prepared.ToPng());
        }
        return (prepared, svg);
    }
}

/// <summary>Text sizes of an SVG shown no wider than a host width, the way <c>max-width:100%</c> scales it.</summary>
internal sealed record RenderedText(double Width, double Height, double DisplayScale, int Labels, int BelowMinimum, double Smallest, double Minimum) {
    private static readonly Regex Root = new("<svg[^>]*?\\swidth=\"(?<w>[0-9.]+)\"[^>]*?\\sheight=\"(?<h>[0-9.]+)\"", RegexOptions.CultureInvariant);
    private static readonly Regex Text = new("<text\\b[^>]*?\\sfont-size=\"(?<size>[0-9.]+)\"", RegexOptions.CultureInvariant);

    internal static RenderedText Measure(string svg, double hostWidth, double minimum = 11) {
        var root = Root.Match(svg);
        var width = double.Parse(root.Groups["w"].Value, CultureInfo.InvariantCulture);
        var height = double.Parse(root.Groups["h"].Value, CultureInfo.InvariantCulture);
        var scale = Math.Min(1, hostWidth / width);
        var sizes = Text.Matches(svg).Select(match => double.Parse(match.Groups["size"].Value, CultureInfo.InvariantCulture) * scale).ToArray();
        return new RenderedText(width, height, scale, sizes.Length, sizes.Count(size => size < minimum - 1e-6), sizes.DefaultIfEmpty(0).Min(), minimum);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"svg {Width:0}x{Height:0}, display scale {DisplayScale:0.###}, {Labels} labels, {BelowMinimum} below {Minimum:0.#} px, smallest {Smallest:0.##} px");
}
