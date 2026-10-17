using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartLinkedValueEndpointContractsTests {
    [Theory]
    [InlineData("rangebar", false, false)]
    [InlineData("rangebar", true, false)]
    [InlineData("rangebar", false, true)]
    [InlineData("waterfall", false, false)]
    [InlineData("waterfall", true, false)]
    [InlineData("waterfall", false, true)]
    [InlineData("dumbbell", false, false)]
    [InlineData("dumbbell", false, true)]
    public void ComparisonBoundsUseTheSeriesValueAxisWithoutCallingTheCategoryFormatter(string family, bool horizontal, bool secondary) {
        var calls = new Dictionary<double, int>();
        var chart = CreateChart(family, horizontal, secondary,
            value => calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1);
        var prepared = chart.Prepare(new VisualRenderContext());
        var points = EndpointMarks(prepared.ToSvg());
        Assert.Equal(2, points.Length);
        var expected = family == "waterfall" ? new[] { (0d, 10.25), (10.25, 24.75) } : new[] { (10.25, 24.75), (12.5, 30.25) };
        for (var index = 0; index < points.Length; index++) {
            var point = points[index];
            var start = double.Parse(point.Attribute("data-cfx-start")!.Value, CultureInfo.InvariantCulture);
            var end = double.Parse(point.Attribute("data-cfx-end")!.Value, CultureInfo.InvariantCulture);
            Assert.Equal(expected[index].Item1, start);
            Assert.Equal(expected[index].Item2, end);
            Assert.Equal(ValueCaption(start), point.Attribute("data-cfx-formatted-start")!.Value);
            Assert.Equal(ValueCaption(end), point.Attribute("data-cfx-formatted-end")!.Value);
            Assert.Equal(ValueCaption(start) + " – " + ValueCaption(end), point.Attribute("data-cfx-range-label")!.Value);
            Assert.StartsWith("Category ", point.Attribute("data-cfx-formatted-x")!.Value, StringComparison.Ordinal);
        }
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        var valueProjection = horizontal ? projection.X : Assert.IsType<ChartDataAxisProjection>(projection.Y);
        Assert.NotEmpty(valueProjection.DisplayTicks);
        Assert.All(valueProjection.DisplayTicks, tick => Assert.Equal(ValueCaption(tick.Value), tick.Text));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        var countBeforeExport = calls.Values.Sum();
        prepared.ToSvg();
        Assert.Equal(countBeforeExport, calls.Values.Sum());
    }

    [Fact]
    public void ExplicitlyEmptyValueBoundCaptionsDoNotCreateASeparatorOnlyRangeLabel() {
        var chart = CreateChart("rangebar", horizontal: false, secondary: false);
        chart.Options.YAxis.LabelFormatter = _ => string.Empty;
        var points = EndpointMarks(chart.ToSvg());
        Assert.All(points, point => {
            Assert.Equal(string.Empty, point.Attribute("data-cfx-formatted-start")!.Value);
            Assert.Equal(string.Empty, point.Attribute("data-cfx-formatted-end")!.Value);
            Assert.Equal(string.Empty, point.Attribute("data-cfx-range-label")!.Value);
        });
    }

    [Theory]
    [InlineData("rangebar")]
    [InlineData("waterfall")]
    [InlineData("dumbbell")]
    public void MixedValueAxesKeepEndpointCaptionsWithoutPublishingAnAmbiguousValueProjection(string family) {
        var chart = CreateChart(family, horizontal: false, secondary: true);
        // Waterfall is an exclusive family; a supported primary reference still
        // makes its screen-space Y ambiguous beside secondary observations.
        if (family == "waterfall") chart.AddHorizontalLine(1, "Primary reference");
        else chart.AddLine("Primary", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var prepared = chart.Prepare(new VisualRenderContext());
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        Assert.Null(projection.Y);
        Assert.NotEmpty(projection.X.DisplayTicks);
        var points = EndpointMarks(prepared.ToSvg());
        Assert.Equal(2, points.Length);
        Assert.All(points, point => {
            var start = double.Parse(point.Attribute("data-cfx-start")!.Value, CultureInfo.InvariantCulture);
            var end = double.Parse(point.Attribute("data-cfx-end")!.Value, CultureInfo.InvariantCulture);
            Assert.Equal(ValueCaption(start), point.Attribute("data-cfx-formatted-start")!.Value);
            Assert.Equal(ValueCaption(end), point.Attribute("data-cfx-formatted-end")!.Value);
        });
    }

    // Reused by actual browser acceptance: a category formatter cannot consume a value-domain endpoint.
    internal static Chart CreateChart(string family, bool horizontal, bool secondary, Action<double>? onValueFormat = null) {
        var chart = Chart.Create().WithSize(800, 400).WithTitle("Value endpoint formatting").WithDataLabels(false);
        if (family == "rangebar") chart.AddRangeBar("Intervals", new[] { new ChartInterval(0, 10.25, 24.75), new ChartInterval(1, 12.5, 30.25) })
            .ConfigureRangeBar(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        else if (family == "waterfall") chart.AddWaterfall("Net change", new[] {
            ChartWaterfallItem.OpeningBalance(0, 10.25), ChartWaterfallItem.Delta(1, 14.5) })
            .ConfigureWaterfall(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        else if (family == "dumbbell") chart.AddDumbbell("Comparison", new[] { new ChartDumbbell(0, 10.25, 24.75), new ChartDumbbell(1, 12.5, 30.25) });
        else throw new ArgumentOutOfRangeException(nameof(family));
        if (secondary) chart.Series[0].UseSecondaryYAxis();
        chart.Series[0].WithObservationIds(new[] { new ChartObservationId("value-first"), new ChartObservationId("value-second") });
        var category = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        category.LabelFormatter = value => {
            if (Math.Abs(value) > 4) throw new InvalidOperationException("A value endpoint was passed to the category formatter.");
            return "Category " + value.ToString("0.##", CultureInfo.InvariantCulture);
        };
        // A distinct primary format detects accidentally ignoring a secondary series' owning axis.
        if (secondary) chart.Options.YAxis.ValueFormat = ChartValueFormat.Number("0 'wrong-primary-axis'");
        var valueAxis = horizontal ? chart.Options.XAxis : secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        valueAxis.LabelFormatter = value => { onValueFormat?.Invoke(value); return ValueCaption(value); };
        return chart;
    }

    private static string ValueCaption(double value) => value.ToString("0.00", CultureInfo.GetCultureInfo("de-DE")) + " ms";

    private static XElement[] EndpointMarks(string svg) => XDocument.Parse(svg).Descendants()
        .Where(node => node.Attribute("data-cfx-point") != null && node.Attribute("data-cfx-start") != null && node.Attribute("data-cfx-end") != null).ToArray();
}
