using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartXZoomPrecisionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeTrackPrecisionRejectsFiniteLevelsWithExplicitTicksBeforeNavigation(bool reversed) {
        var chart = Bare(0, 10).AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 5), new ChartPoint(10, 8) });
        chart.Series[0].Markers.Enabled = false; chart.Options.XAxis.Reversed = reversed;
        // Explicit ticks bypass generated tick-count admission; coordinate precision still belongs to Core.
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(0, "0"));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 1e15)));
        Assert.Equal("levels", error.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeTrackPrecisionBudgetDependsOnLogicalExtentAndCoversNegativeTranslatedPositions(bool reversed) {
        var chart = Bare(0, 10).AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 5), new ChartPoint(10, 8) });
        chart.Series[0].Markers.Enabled = false; chart.Options.XAxis.Reversed = reversed;
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(0, "0"));
        var levels = new ChartXZoomLevels(1, Math.Pow(2, 31));
        var bank = chart.PrepareXZoom(Context(), levels);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.PrepareXZoom(Context(840), levels));
        foreach (var pan in new[] { 0d, .173, .5, 1 }) {
            var view = bank.At(1, pan); var projection = view.XProjection!;
            Assert.InRange(projection.Project(projection.Minimum) - (reversed ? bank.PlotBounds.Right : bank.PlotBounds.Left), -.0005, .0005);
            Assert.InRange(projection.Project(projection.Maximum) - (reversed ? bank.PlotBounds.Left : bank.PlotBounds.Right), -.0005, .0005);
            Assert.Equal(reversed ? projection.Maximum : projection.Minimum, projection.Unproject(bank.PlotBounds.Left));
            Assert.Equal(reversed ? projection.Minimum : projection.Maximum, projection.Unproject(bank.PlotBounds.Right));
            for (var i = 0; i < chart.Series[0].Points.Count; i++) {
                var region = view.Regions.Single(item => item.Id == "series-0-point-" + i);
                Assert.InRange(projection.Project(chart.Series[0].Points[i].X) - (region.Bounds.Left + region.Bounds.Width / 2), -.0005, .0005);
            }
        }
        Assert.True(bank.At(1, reversed ? 0 : 1).XProjection!.Project(reversed ? 10 : 0) < 0);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 3)]
    [InlineData(-1, 3)]
    public void PreparationRejectsLevelsWhoseSourceWidthCannotPreserveTheFullSpanPrecision(int sign, double factor) {
        var minimum = sign * 1e15;
        var chart = Bare(minimum, minimum + 1).AddLine("Observed", new[] { new ChartPoint(minimum, 2), new ChartPoint(minimum + 1, 8) });
        chart.Series[0].Markers.Enabled = false;
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, factor)));
        Assert.Equal("levels", error.ParamName);
    }

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(1, true, true)]
    [InlineData(-1, false, true)]
    [InlineData(-1, true, false)]
    public void RepresentedPanBoundsOwnNativePaintProjectionAndRawBoundaryAdmission(int sign, bool reversed, bool step) {
        var minimum = sign * 1e15;
        var chart = Bare(minimum, minimum + 1).WithDataLabels(); chart.Options.XAxis.Reversed = reversed;
        var offsets = new[] { 0d, .125, .25, .375, .5, 1 };
        for (var i = 0; i < offsets.Length; i++) {
            var point = new ChartPoint(minimum + offsets[i], 1 + i);
            if (step) chart.AddStepLine("Point " + i, new[] { point }); else chart.AddLine("Point " + i, new[] { point });
            chart.Series[i].ConfigureMarkers(marker => { marker.Enabled = true; marker.Radius = 4; });
            chart.Series[i].WithPointLabel(0, i == 1 ? "L" : i == 3 ? "R" : "");
            chart.Series[i].DataLabelPlacement = (i < 3) != reversed ? ChartDataLabelPlacement.Right : ChartDataLabelPlacement.Left;
        }
        chart.AddVerticalLine(minimum).AddVerticalLine(minimum + .125).AddVerticalLine(minimum + .375).AddVerticalLine(minimum + .5)
            .AddVerticalBand(minimum, minimum + .125).AddVerticalBand(minimum + .375, minimum + .5);
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 4));
        var view = bank.At(1, .173); var projection = view.XProjection!;
        Assert.Equal(minimum + .125, projection.Minimum); Assert.Equal(minimum + .375, projection.Maximum);
        Assert.Equal(bank.PlotBounds.Left + bank.PlotBounds.Width / 2, projection.Project(minimum + .25), 9);
        Assert.Equal(3, view.Scene.Nodes.Count(node => node.Role == "marker"));
        Assert.Equal(2, view.Scene.Nodes.Count(node => node.Role == "annotation-line"));
        Assert.DoesNotContain(view.Scene.Nodes, node => node.Role == "annotation-band");
        Assert.Equal(new[] { "L", "R" }, view.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label")
            .Select(node => node.Text.Lines.Single().Text));
        foreach (var pan in new[] { 0d, .08, .173, .5, .899, 1 }) {
            var prepared = bank.At(1, pan); var current = prepared.XProjection!;
            Assert.True(current.Maximum > current.Minimum);
            Assert.InRange(current.Minimum, bank.FullViewport.Minimum, bank.FullViewport.Maximum);
            Assert.InRange(current.Maximum, bank.FullViewport.Minimum, bank.FullViewport.Maximum);
            Assert.Equal(.25, current.Maximum - current.Minimum);
            Assert.Equal(reversed ? current.Maximum : current.Minimum, current.Unproject(bank.PlotBounds.Left));
            Assert.Equal(reversed ? current.Minimum : current.Maximum, current.Unproject(bank.PlotBounds.Right));
            for (var i = 0; i < offsets.Length; i++) {
                var region = prepared.Regions.Single(item => item.Id == "series-" + i + "-point-0");
                Assert.Equal(current.Project(minimum + offsets[i]), region.Bounds.Left + region.Bounds.Width / 2, 9);
            }
            AssertPointPaintUsesProjection(prepared);
        }
        Assert.Equal(bank.FullViewport.Minimum, bank.At(1, 0).XProjection!.Minimum);
        Assert.Equal(bank.FullViewport.Maximum, bank.At(1, 1).XProjection!.Maximum);
        Assert.Equal(6, XDocument.Parse(view.ToSvg()).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point"));
        chart.WithDataLabels(false);
        Assert.Equal(chart.Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(projection.Minimum, projection.Maximum))).ToPng(),
            chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 4)).At(1, .173).ToPng());
    }

    private static void AssertPointPaintUsesProjection(PreparedVisual visual) {
        var scopes = new Stack<(double Offset, double? Source)>();
        var offset = 0d; double? source = null;
        foreach (var node in visual.Scene.Nodes) {
            if (node is VisualSceneGroup group) {
                scopes.Push((offset, source)); offset += group.Translation?.X ?? 0;
                if (group.SourceX?.IsPoint == true) source = group.SourceX.Value.Minimum;
            } else if (node is VisualSceneEndGroup) (offset, source) = scopes.Pop();
            else if (source.HasValue && node is VisualSceneEllipse marker && marker.Role == "marker")
                Assert.Equal(visual.XProjection!.Project(source.Value), marker.Cx + offset, 9);
            else if (source.HasValue && node is VisualScenePath path && path.Role == "annotation-line")
                Assert.All(path.Commands, command => Assert.Equal(visual.XProjection!.Project(source.Value), command.X + offset, 9));
        }
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(false, 16)]
    [InlineData(true, 16)]
    [InlineData(false, 100)]
    [InlineData(true, 100)]
    public void OrdinaryFractionalLevelsRetainNativeProjectionAndExactInverseEndpoints(bool reversed, double factor) {
        var chart = Bare(0, 10).AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 5), new ChartPoint(10, 8) });
        chart.Series[0].Markers.Enabled = false; chart.Options.XAxis.Reversed = reversed;
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(0, "0"));
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, factor));
        foreach (var pan in new[] { 0d, .173, .899, 1 }) {
            var view = bank.At(1, pan); var projection = view.XProjection!;
            foreach (var region in view.Regions.Where(region => region.Role == "point")) {
                var index = int.Parse(region.Id.Substring(region.Id.LastIndexOf('-') + 1));
                Assert.Equal(projection.Project(chart.Series[0].Points[index].X), region.Bounds.Left + region.Bounds.Width / 2, 9);
            }
            Assert.Equal(reversed ? projection.Maximum : projection.Minimum, projection.Unproject(bank.PlotBounds.Left));
            Assert.Equal(reversed ? projection.Minimum : projection.Maximum, projection.Unproject(bank.PlotBounds.Right));
        }
    }

    private static Chart Bare(double minimum, double maximum) => Chart.Create().WithAxes(false).WithGrid(false).WithLegend(false)
        .WithXAxisBounds(minimum, maximum).WithYAxisBounds(0, 10);
    private static VisualRenderContext Context(double width = 420) => new(new VisualLayoutOptions(new VisualSize(width, 280)), VisualTheme.Graphite(),
        frame: new VisualFrame(showLegend: false), font: FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));
}
