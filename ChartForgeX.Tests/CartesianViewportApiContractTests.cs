using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianViewportApiContractTests {
    [Fact]
    public void PlainNullResetsBothSurfacesAndKeepsTheRenderableContract() {
        var chart = Model();
        var context = VisualExportRequest.ForChart(chart).Context;
        IVisualRenderable renderable = chart;
        Func<VisualRenderContext, PreparedVisual> prepare = chart.Prepare;
        var full = prepare(context);
        var renderer = new HtmlInteractiveChartRenderer();
        var configurations = 0;
        void Configure(HtmlChartInteractionOptions options) {
            configurations++;
            options.IdScope = "viewport-contract";
            options.IncludeContextMenu = true;
        }
        var initial = renderer.PrepareSection(chart, null, Configure);
        var window = new ChartCartesianViewport(new ChartAxisWindow(2, 8), new ChartAxisWindow(20, 80));
        Assert.Equal(2, chart.Prepare(context, window).XProjection!.Minimum);
        Assert.Equal(20, renderer.PrepareSection(chart, window, Configure).PreparedVisual.YProjection!.Minimum);

        // These untyped null arguments must compile and mean the same reset on both public surfaces.
        var reset = chart.Prepare(context, null);
        var section = renderer.PrepareSection(chart, null, Configure);
        Assert.Equal(full.ToSvg(), reset.ToSvg());
        Assert.Equal(full.ToPng(), reset.ToPng());
        Assert.Equal(full.ToSvg(), renderable.Prepare(context).ToSvg());
        Assert.Equal(full.ToSvg(), section.PreparedVisual.ToSvg());
        Assert.Equal(initial.Html, section.Html);
        Assert.Equal(3, configurations);
        Assert.Equal(0, reset.XProjection!.Minimum);
        Assert.Equal(10, reset.XProjection.Maximum);
        Assert.Equal(0, reset.YProjection!.Minimum);
        Assert.Equal(100, reset.YProjection.Maximum);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    [InlineData("xy")]
    public void OneCartesianInputKeepsExactWindowsAndAllSourceFactsAcrossBothSurfaces(string axes) {
        var chart = Model();
        var context = VisualExportRequest.ForChart(chart).Context;
        var viewport = new ChartCartesianViewport(
            axes.Contains('x') ? new ChartAxisWindow(2, 8) : null,
            axes.Contains('y') ? new ChartAxisWindow(20, 80) : null);
        var native = chart.Prepare(context, viewport);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart, viewport);
        Assert.Equal(native.ToSvg(), section.PreparedVisual.ToSvg());
        Assert.Equal(viewport.X?.Minimum ?? 0, native.XProjection!.Minimum);
        Assert.Equal(viewport.X?.Maximum ?? 10, native.XProjection.Maximum);
        Assert.Equal(viewport.Y?.Minimum ?? 0, native.YProjection!.Minimum);
        Assert.Equal(viewport.Y?.Maximum ?? 100, native.YProjection.Maximum);
        Assert.Equal(0, native.XProjection.FullMinimum);
        Assert.Equal(10, native.XProjection.FullMaximum);
        Assert.Equal(0, native.YProjection.FullMinimum);
        Assert.Equal(100, native.YProjection.FullMaximum);
        var facts = XDocument.Parse(native.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "0", "5", "10" }, facts.Select(node => (string?)node.Attribute("data-cfx-x")));
        Assert.Equal(new[] { "0", "50", "100" }, facts.Select(node => (string?)node.Attribute("data-cfx-y")));
        Assert.Equal(0, chart.Options.XAxis.Minimum);
        Assert.Equal(10, chart.Options.XAxis.Maximum);
        Assert.Equal(0, chart.Options.YAxis.Minimum);
        Assert.Equal(100, chart.Options.YAxis.Maximum);
        Assert.Single(chart.Series);
        Assert.Equal(3, chart.Series[0].Points.Count);
    }

    [Fact]
    public void PreparedWindowsRemainDetachedAfterTheAuthoringModelChanges() {
        var chart = Model();
        var viewport = new ChartCartesianViewport(new ChartAxisWindow(2, 8), new ChartAxisWindow(20, 80));
        var native = chart.Prepare(VisualExportRequest.ForChart(chart).Context, viewport);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart, viewport);
        var svg = native.ToSvg();
        var png = native.ToPng();
        var html = section.Html;
        chart.WithXAxisBounds(-100, 100).WithYAxisBounds(-1000, 1000).WithTitle("Changed");
        chart.Options.XAxis.Reversed = true;
        chart.Options.YAxis.Reversed = true;
        chart.AddLine("Later", new[] { new ChartPoint(-100, -1000), new ChartPoint(100, 1000) });
        Assert.Equal(svg, native.ToSvg());
        Assert.Equal(png, native.ToPng());
        Assert.Equal(svg, section.PreparedVisual.ToSvg());
        Assert.Equal(html, section.Html);
        Assert.Equal(2, native.XProjection!.Minimum);
        Assert.Equal(8, native.XProjection.Maximum);
        Assert.Equal(20, native.YProjection!.Minimum);
        Assert.Equal(80, native.YProjection.Maximum);
        Assert.Equal(5, native.XProjection.Unproject(native.XProjection.Project(5)), 9);
        Assert.Equal(50, native.YProjection.Unproject(native.YProjection.Project(50)), 9);
    }

    [Fact]
    public void FullDomainNullDoesNotImposeCartesianEligibilityOnOtherChartFamilies() {
        var chart = Chart.Create().WithHeader(false).WithLegend(false)
            .AddPie("Categories", new[] { new ChartPoint(1, 2), new ChartPoint(3, 4) });
        var context = VisualExportRequest.ForChart(chart).Context;
        var renderer = new HtmlInteractiveChartRenderer();
        Assert.Equal(chart.Prepare(context).ToSvg(), chart.Prepare(context, null).ToSvg());
        Assert.Equal(renderer.PrepareSection(chart).Html, renderer.PrepareSection(chart, null).Html);
        Assert.Null(chart.Prepare(context, null).XProjection);
        var explicitWindow = new ChartCartesianViewport(x: new ChartAxisWindow(1, 2));
        Assert.Throws<NotSupportedException>(() => chart.Prepare(context, explicitWindow));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareSection(chart, explicitWindow));
    }

    [Fact]
    public void SecondaryAxisChartsKeepIndependentPrimaryAndSecondaryPhysicalWindows() {
        var chart = Model().AddLine("Secondary", new[] { new ChartPoint(0, 1000), new ChartPoint(10, 2000) });
        chart.Series[1].UseSecondaryValueAxis();
        var context = VisualExportRequest.ForChart(chart).Context;
        var renderer = new HtmlInteractiveChartRenderer();
        var x = new ChartCartesianViewport(x: new ChartAxisWindow(2, 8));
        foreach (var prepared in new[] { chart.Prepare(context, x), renderer.PrepareSection(chart, x).PreparedVisual }) {
            Assert.Equal(2, prepared.XProjection!.Minimum);
            Assert.Equal(8, prepared.XProjection.Maximum);
            Assert.NotNull(prepared.YProjection); Assert.NotNull(prepared.SecondaryYProjection);
        }
        foreach (var viewport in new[] { new ChartCartesianViewport(y: new ChartAxisWindow(20, 80)),
            new ChartCartesianViewport(new ChartAxisWindow(2, 8), new ChartAxisWindow(20, 80)) }) {
            var native = chart.Prepare(context, viewport);
            var html = renderer.PrepareSection(chart, viewport).PreparedVisual;
            Assert.Equal(20, native.YProjection!.Minimum); Assert.Equal(80, native.YProjection.Maximum);
            Assert.Equal(native.ToSvg(), html.ToSvg());
            Assert.Equal(native.SecondaryYProjection!.FullMinimum, native.SecondaryYProjection.Minimum);
            Assert.Equal(native.SecondaryYProjection.FullMaximum, native.SecondaryYProjection.Maximum);
        }
        Assert.Equal(chart.Prepare(context).ToSvg(), chart.Prepare(context, null).ToSvg());
    }

    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    public void ExplicitOutOfDomainWindowsKeepTheNativeValidationFailure(string axis) {
        var chart = Model();
        var context = VisualExportRequest.ForChart(chart).Context;
        var viewport = axis == "x" ? new ChartCartesianViewport(x: new ChartAxisWindow(-1, 8))
            : new ChartCartesianViewport(y: new ChartAxisWindow(20, 101));
        var native = Assert.Throws<ArgumentOutOfRangeException>(() => chart.Prepare(context, viewport));
        var html = Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlInteractiveChartRenderer().PrepareSection(chart, viewport));
        Assert.Equal(native.ParamName, html.ParamName);
        Assert.Equal(native.Message, html.Message);
        Assert.Equal(0, chart.Options.XAxis.Minimum);
        Assert.Equal(100, chart.Options.YAxis.Maximum);
    }

    [Fact]
    public void MissingPreparationOwnersStillFailWithTheirArgumentName() {
        var chart = Model();
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => chart.Prepare(null!, null)).ParamName);
        Assert.Equal("chart", Assert.Throws<ArgumentNullException>(() => new HtmlInteractiveChartRenderer().PrepareSection(null!, null)).ParamName);
    }

    private static Chart Model() => Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false)
        .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100)
        .AddLine("Observed", new[] { new ChartPoint(0, 0), new ChartPoint(5, 50), new ChartPoint(10, 100) });
}
