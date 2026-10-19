using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects absent matrix observations through row construction, clear/refill and both native painters.</summary>
public sealed class MatrixRowLifecycleTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FixedNumericKeysIdentifyMissingCellsSeparatelyFromObservedZero(bool hexagonal, bool allMissing) {
        var missingColor = ChartColor.FromHex("#9CA3AF");
        var chart = Chart.Create().WithSize(520, 320).WithHeader(false).WithLegend(true)
            .WithHeatmapColorScale(ChartColorScale.Sequential(ChartColor.Black, ChartColor.White)
                .WithValueRange(0, 10).WithNoDataColor(missingColor))
            .ConfigureLabels(labels => labels.NoData = "Not measured");
        Add(chart, hexagonal, "Observed", new double?[] { null, allMissing ? null : 0, null });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var missing = Assert.Single(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "heatmap-scale-empty");
        Assert.Equal("true", (string?)missing.Attribute("data-cfx-empty"));
        Assert.Null(missing.Attribute("data-cfx-value"));
        var key = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "heatmap-scale-missing-swatch");
        Assert.Equal(missingColor, key.Fill);
        Assert.Contains(prepared.Regions, region => region.Role == "heatmap-scale-empty" && region.Label == "Not measured");
        var cells = Cells(prepared);
        if (allMissing) Assert.Empty(cells);
        else Assert.Equal("0", Assert.Single(cells).Attribute("data-cfx-value")?.Value);
        Assert.NotEmpty(prepared.ToPng(Raster()));
    }

    [Theory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public void EntirelyMissingRowsKeepTheirAuthoredColumnsWithoutInventingZeroCells(bool hexagonal, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(520, 320).WithHeader(false).WithLegend(false)
            .WithXLabels("First", "Second", "Third").WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Hidden);
        Add(chart, hexagonal, "Missing", new double?[] { null, null, null });
        var missing = chart.Series[0];
        var before = chart.Prepare(Context(mode));
        Assert.Empty(Cells(before));
        Assert.Equal("3", Matrix(before).Attribute("data-cfx-column-count")?.Value);
        var row = ChartHeatmapRow.CreateMasked("Missing", null, null, null);
        Assert.Equal(3, row.Cells.Count); Assert.Empty(row.Values); Assert.True(row.HasMaskedCells);
        var modelChart = Chart.Create();
        if (hexagonal) modelChart.AddHexbinHeatmapRows(new[] { row }); else modelChart.AddHeatmapRows(new[] { row });
        var fromModel = modelChart.Prepare(Context(mode));
        Assert.Empty(Cells(fromModel)); Assert.Equal("3", Matrix(fromModel).Attribute("data-cfx-column-count")?.Value);
        var emptyChart = Chart.Create();
        AddPoints(emptyChart, hexagonal, "No observations", Array.Empty<ChartPoint>());
        Assert.Empty(Cells(emptyChart.Prepare(Context(mode))));
        Assert.NotEmpty(emptyChart.Prepare(Context(mode)).ToPng(Raster()));
        var frozenSvg = before.ToSvg(); var frozenPng = before.ToPng(Raster());
        Add(chart, hexagonal, "Observed zero", new double?[] { null, 0, null });
        var populated = chart.Prepare(Context(mode));
        var zero = Assert.Single(Cells(populated));
        Assert.Equal("1", zero.Attribute("data-cfx-series")?.Value);
        Assert.Equal("2", zero.Attribute("data-cfx-x")?.Value);
        Assert.Equal("0", zero.Attribute("data-cfx-value")?.Value);
        Assert.Equal("3", Matrix(populated).Attribute("data-cfx-column-count")?.Value);
        Assert.Same(missing, chart.Series[0]); Assert.Equal(0, missing.ObservationCount);
        Assert.Equal(frozenSvg, before.ToSvg()); Assert.Equal(frozenPng, before.ToPng(Raster()));
        AssertPaintedCenter(populated, hexagonal);
        Capture(populated, $"matrix-missing-{hexagonal}-{mode}");
    }

    [Theory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public void ClearingAndRefillingANumericPeerKeepsItsRowAndTheOtherRowsSourceFacts(bool hexagonal, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(520, 320).WithHeader(false).WithLegend(false)
            .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Hidden);
        AddPoints(chart, hexagonal, "Changing", new[] { new ChartPoint(1, 10) });
        AddPoints(chart, hexagonal, "Retained", new[] { new ChartPoint(3, 0) });
        var changing = chart.Series[0]; var retained = chart.Series[1];
        retained.WithObservationId(0, new ChartObservationId("retained-zero"));
        var before = chart.Prepare(Context(mode)); var frozenSvg = before.ToSvg(); var frozenPng = before.ToPng(Raster());
        changing.Points.Clear();
        var cleared = chart.Prepare(Context(mode));
        var cell = Assert.Single(Cells(cleared));
        Assert.Equal("retained-zero", cell.Attribute("data-cfx-observation-id")?.Value);
        Assert.Equal("1", cell.Attribute("data-cfx-series")?.Value);
        Assert.Equal("3", cell.Attribute("data-cfx-x")?.Value);
        Assert.Equal("0", cell.Attribute("data-cfx-value")?.Value);
        Assert.Contains(cleared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "heatmap-row-label" && node.Text.Lines.Any(line => line.Text == "Changing"));
        AssertPaintedCenter(cleared, hexagonal);
        changing.Points.Add(new ChartPoint(1, 20));
        var refilled = chart.Prepare(Context(mode));
        Assert.Equal(2, Cells(refilled).Length);
        Assert.Equal("retained-zero", Cells(refilled).Single(item => item.Attribute("data-cfx-series")?.Value == "1").Attribute("data-cfx-observation-id")?.Value);
        Assert.Same(changing, chart.Series[0]); Assert.Same(retained, chart.Series[1]);
        Assert.Equal(frozenSvg, before.ToSvg()); Assert.Equal(frozenPng, before.ToPng(Raster()));
        retained.Points.Clear(); changing.Points.Clear();
        var empty = chart.Prepare(Context(mode)); Assert.Empty(Cells(empty)); Assert.NotEmpty(empty.ToPng(Raster()));
        changing.Points.Add(new ChartPoint(9, 0));
        Assert.Single(Cells(chart.Prepare(Context(mode))));
        Capture(cleared, $"matrix-cleared-{hexagonal}-{mode}");
        Capture(refilled, $"matrix-refilled-{hexagonal}-{mode}");
    }

    private static void Add(Chart chart, bool hexagonal, string name, double?[] values) {
        if (hexagonal) chart.AddHexbinHeatmapRow(name, values); else chart.AddHeatmapRow(name, values);
    }
    private static void AddPoints(Chart chart, bool hexagonal, string name, ChartPoint[] points) {
        if (hexagonal) chart.AddHexbinHeatmapRow(name, points); else chart.AddHeatmapRow(name, points);
    }
    private static XElement[] Cells(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(node => node.Attribute("data-cfx-role")?.Value is "heatmap-cell" or "hexbin-cell").ToArray();
    private static XElement Matrix(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Single(node => node.Attribute("data-cfx-role")?.Value is "heatmap" or "hexbin-heatmap");
    private static VisualRenderContext Context(VisualThemeMode mode) => new(new VisualLayoutOptions(new VisualSize(520, 320)),
        VisualTheme.Graphite(), mode, frame: new VisualFrame(showLegend: false));
    private static VisualRenderOptions Raster() => new(supersampling: 1);
    private static void AssertPaintedCenter(PreparedVisual prepared, bool hexagonal) {
        var region = Assert.Single(prepared.Regions, item => item.Role == (hexagonal ? "hexbin-cell" : "heatmap-cell"));
        var cell = Assert.Single(Cells(prepared));
        var shape = cell.Descendants().First(node => node.Name.LocalName is "rect" or "path");
        var expected = ChartColor.FromHex(shape.Attribute("fill")!.Value);
        var image = prepared.ToRgba(Raster());
        var x = (int)(region.Bounds.Left + region.Bounds.Width / 2); var y = (int)(region.Bounds.Top + region.Bounds.Height / 2);
        var offset = (y * image.Width + x) * 4;
        Assert.Equal(expected.R, image.Pixels[offset]); Assert.Equal(expected.G, image.Pixels[offset + 1]); Assert.Equal(expected.B, image.Pixels[offset + 2]);
    }
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), prepared.ToPng(Raster()));
    }
}
