using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadialFiniteFractionTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.PolarArea)]
    public void OverflowingTotalsRetainFinitePartsAndTheSameNativeProportions(ChartSeriesKind kind) {
        var chart = Create(kind, 1e308, 1e308);
        chart.Options.ShowLegend = false;
        chart.Options.ShowDataLabels = false;
        chart.Options.ShowDonutCenterLabel = false;
        chart.Options.ValueFormatter = value => {
            Assert.True(ChartMath.IsFinite(value));
            return value.ToString("R", CultureInfo.InvariantCulture);
        };
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var role = kind == ChartSeriesKind.PolarArea ? "polar-area-point-source" : "radial-point";
        var parts = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
        Assert.Equal(2, parts.Length);
        Assert.All(parts, part => {
            Assert.Equal(1e308, double.Parse((string)part.Attribute("data-cfx-value")!, CultureInfo.InvariantCulture));
            Assert.Equal(.5, double.Parse((string)part.Attribute("data-cfx-percent")!, CultureInfo.InvariantCulture));
            Assert.Equal("true", (string?)part.Attribute("data-cfx-total-overflow"));
        });
        Assert.Contains(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == (kind == ChartSeriesKind.PolarArea ? "polar-area.total-overflow" : "radial.total-overflow"));
        var ordinary = Create(kind, 1, 1);
        ordinary.Options.ShowLegend = false;
        ordinary.Options.ShowDataLabels = false;
        ordinary.Options.ShowDonutCenterLabel = false;
        Assert.Equal(ordinary.ToRgbaImage().Pixels, prepared.ToRgba().Pixels);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    public void OverflowingOtherCapturesContributorsAndLowerBoundWithoutInventingAValue(ChartSeriesKind kind) {
        var chart = Create(kind, 1e308, 1e308, 1e308).WithDataLabels();
        chart.Options.MaximumPieSlices = 2;
        var callbacks = new List<ChartPieSliceLabelContext>();
        chart.Options.ValueFormatter = value => {
            Assert.True(ChartMath.IsFinite(value));
            return value.ToString("R", CultureInfo.InvariantCulture);
        };
        chart.WithPieSliceLabelFormatter(context => { Assert.True(ChartMath.IsFinite(context.Value)); callbacks.Add(context); return context.Label; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var aggregate = document.Descendants().Where(element =>
            (string?)element.Attribute("data-cfx-role") is "radial-point" or "legend-entry"
            && (string?)element.Attribute("data-cfx-point") == "-1").ToArray();
        Assert.Equal(2, aggregate.Length);
        const string bound = "> 1.7976931348623157E+308";
        Assert.All(aggregate, target => {
            Assert.Null(target.Attribute("data-cfx-value"));
            Assert.Equal("true", (string?)target.Attribute("data-cfx-value-overflow"));
            Assert.Equal(bound, (string?)target.Attribute("data-cfx-formatted-value"));
            Assert.Equal("1,2", (string?)target.Attribute("data-cfx-source-points"));
            Assert.Equal(new[] { 1e308, 1e308 }, ((string)target.Attribute("data-cfx-source-values")!).Split(',').Select(value => double.Parse(value, CultureInfo.InvariantCulture)));
            Assert.Equal(2d / 3, double.Parse((string)target.Attribute("data-cfx-percent")!, CultureInfo.InvariantCulture), 14);
            Assert.Null(target.Attribute("data-cfx-observation-id"));
        });
        Assert.NotEmpty(callbacks);
        Assert.DoesNotContain(callbacks, context => context.PointIndex == -1);
        Assert.Contains(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == "radial.aggregate-overflow");
        Assert.True(prepared.ToPng().Length > 64);
        if (kind == ChartSeriesKind.Donut) {
            Assert.Equal(bound, Center(document).Attribute("data-cfx-center-value")!.Value);
            Assert.Equal("> 1.79E308", document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "donut-total-label").Value);
            chart.WithDonutCenterText("Observed total", "Capacity");
            Assert.Equal("Observed total", Center(XDocument.Parse(chart.ToSvg())).Attribute("data-cfx-center-value")!.Value);
        }
    }

    [Theory]
    [InlineData(500, 340, .35)]
    [InlineData(500, 340, .62)]
    [InlineData(320, 240, .35)]
    [InlineData(320, 240, .62)]
    public void OverflowCenterRetainsItsExponentAtSupportedHoleSizes(int width, int height, double ratio) {
        var chart = Create(ChartSeriesKind.Donut, 1e308, 1e308).WithSize(width, height).WithLegend(false);
        chart.Options.DonutInnerRadiusRatio = ratio;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var text = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "donut-total-label").Value;
        Assert.StartsWith(">", text);
        Assert.EndsWith("E308", text);
        Assert.DoesNotContain("…", text);
        Assert.Equal("> 1.7976931348623157E+308", Center(document).Attribute("data-cfx-center-value")!.Value);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void AutomaticExtremeCaptionsStayBoundedWhileAuthoredFormatsRemainAuthoritative() {
        var options = Chart.Create().Options;
        foreach (var value in new[] { 1e308, -1e308, double.MaxValue }) {
            var text = options.ValueFormat.Format(value);
            Assert.InRange(text.Length, 1, 24);
            Assert.Contains("E", text);
            Assert.InRange(Math.Abs(double.Parse(text, CultureInfo.InvariantCulture) / value - 1), 0, .00001);
        }
        Assert.Equal("12.3k", options.ValueFormat.Format(12345));
        Assert.True(ChartValueFormat.Number("0.##").Format(1e308).Length > 24);
        var authored = new string('X', 100);
        Assert.Equal(authored, ChartValueFormat.Custom(_ => authored).Format(1e308));
    }

    private static Chart Create(ChartSeriesKind kind, params double[] values) {
        var points = values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
        var chart = Chart.Create().WithSize(500, 340);
        if (kind == ChartSeriesKind.Pie) return chart.AddPie("Parts", points);
        if (kind == ChartSeriesKind.Donut) return chart.AddDonut("Parts", points);
        return chart.AddPolarArea("Parts", points);
    }

    private static XElement Center(XDocument document) => document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "donut-center");
}
