using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Svg;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartGridPaginationTests {
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void PagesPreserveOrderedFacetsAndGlobalAxisBounds(int capacity) {
        var rows = ChartDataset<int>.From(Enumerable.Range(0, 1001));
        var grid = ChartGrid.FromFacets(rows, value => value, (key, _) => Chart.Create()
            .WithTitle("Site " + key).WithSize(320, 200)
            .AddLine("Value", new[] { new ChartPoint(0, key), new ChartPoint(1, key + 1) }))
            .WithSharedAxes();
        var pages = grid.Paginate(capacity);
        Assert.Equal((1000L / capacity) + 1, pages.Count);
        var first = 0;
        foreach (var page in pages) {
            Assert.Equal(first, page.FirstChartIndex);
            Assert.Equal(1001, page.TotalCharts);
            Assert.Equal(pages.Count, page.TotalPages);
            Assert.InRange(page.ChartCount, 1, capacity);
            Assert.Equal(page.ChartCount, page.Grid.Charts.Count);
            for (var i = 0; i < page.ChartCount; i++) {
                Assert.Same(grid.Charts[first + i], page.Grid.Charts[i]);
                Assert.Equal(grid.Charts[0].Options.YAxisMaximum, page.Grid.Charts[i].Options.YAxisMaximum);
            }
            first += page.ChartCount;
        }
        Assert.Equal(1001, first);
        Assert.Equal(Enumerable.Range(1, pages.Count), pages.Select(page => page.Number));
    }

    [Fact]
    public void PagesCopySettingsAndStylesWithoutSharingGridMutation() {
        var grid = ChartGrid.Create().WithTitle("Fleet").WithSubtitle("Daily")
            .WithColumns(3).WithGap(12).WithPadding(20).WithPngOutputScale(2)
            .WithPanelSize(320, 200).WithPanelFit(VisualPanelFit.Contain)
            .WithTitleStyle(style => style.WithFontSize(21).WithItalic().WithUnderline(TextDecorationStyle.Double))
            .WithSubtitleStyle(style => style.WithWeight("600").WithSuperscript().WithStrikethrough());
        for (var i = 0; i < 7; i++) grid.Add(Chart.Create().WithSize(320, 200).AddLine("Value", new[] { new ChartPoint(0, i), new ChartPoint(1, i + 1) }), i == 6 ? 2 : 1, i == 6 ? 2 : 1);
        var pages = grid.Paginate(6);
        var last = pages[1].Grid;
        Assert.Equal("Fleet", last.Title);
        Assert.Equal("Daily", last.Subtitle);
        Assert.Equal(3, last.Columns);
        Assert.True(last.PreserveEmptyColumns);
        Assert.Equal(12, last.Gap);
        Assert.Equal(20, last.Padding);
        Assert.Equal(2, last.PngOutputScale);
        Assert.Equal(grid.PanelSize, last.PanelSize);
        Assert.Equal(grid.PanelFit, last.PanelFit);
        Assert.Same(grid.Charts[0].Options.Theme, last.Theme);
        Assert.Equal(2, last.PanelSpans[0].ColumnSpan);
        Assert.Equal(2, last.PanelSpans[0].RowSpan);
        Assert.Equal(21, last.TitleStyle.FontSize);
        Assert.True(last.TitleStyle.Italic);
        Assert.Equal(TextDecorationStyle.Double, last.TitleStyle.UnderlineStyle);
        Assert.Equal("600", last.SubtitleStyle.FontWeight);
        Assert.Equal(TextBaseline.Superscript, last.SubtitleStyle.Baseline);
        Assert.True(last.SubtitleStyle.Strikethrough);
        last.TitleStyle.FontSize = 30;
        last.WithPanelSpan(0, 1).WithTitle("Page two");
        Assert.Equal(21, grid.TitleStyle.FontSize);
        Assert.Equal(21, pages[0].Grid.TitleStyle.FontSize);
        Assert.Equal(2, grid.PanelSpans[6].ColumnSpan);
        Assert.Equal("Fleet", grid.Title);
        Assert.NotEmpty(new PngChartGridRenderer().Render(last));
        var svg = XDocument.Parse(new SvgChartGridRenderer().Render(last));
        Assert.NotNull(svg.Root);
        Assert.Equal("1024", svg.Root!.Attribute("width")!.Value);
    }

    [Fact]
    public void InvalidCapacityAndSpansDoNotCorruptSourceGrid() {
        var grid = ChartGrid.Create();
        Assert.Empty(grid.Paginate());
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Paginate(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Paginate(-1));
        var chart = Chart.Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.Add(chart, 0));
        Assert.Empty(grid.Charts);
        Assert.Empty(grid.PanelSpans);
        grid.Add(chart);
        Assert.Single(grid.Paginate());
    }
}
