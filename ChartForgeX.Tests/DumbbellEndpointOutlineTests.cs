using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Endpoint paint inherits the same outline defaults as its start glyph and legend.</summary>
public sealed class DumbbellEndpointOutlineTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredOutlineFallbacksAgreeWithTheLegendAndPreserveExplicitWidths(bool horizontal) {
        foreach (var mode in new[] { "unconfigured", "endpoint-color", "shared-color", "outline-color", "dash-only", "explicit-zero", "explicit-four" }) {
            var chart = Chart.Create().WithSize(600, 360).WithLegend().AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 10, 30) });
            chart.Options.DumbbellOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
            var series = chart.Series[0]; series.Dumbbell.StartMarker.Stroke = ChartColor.Black;
            if (mode == "shared-color") series.Markers.Stroke = ChartColor.Black;
            else if (mode == "outline-color") series.Dumbbell.EndMarker.OutlineStroke = new ChartStrokeStyle(ChartColor.Black);
            else if (mode == "dash-only") series.Dumbbell.EndMarker.OutlineStroke = new ChartStrokeStyle(dash: new[] { 3d, 2d });
            else if (mode != "unconfigured") {
                series.Dumbbell.EndMarker.Stroke = ChartColor.Black;
                if (mode == "explicit-zero") series.Dumbbell.EndMarker.StrokeWidth = 0;
                if (mode == "explicit-four") series.Dumbbell.EndMarker.StrokeWidth = 4;
            }
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            var svg = prepared.ToSvg(); var png = prepared.ToPng();
            var elements = XDocument.Parse(svg).Descendants().ToArray();
            XElement Mark(string role) => Assert.Single(elements, element => (string?)element.Attribute("data-cfx-role") == role);
            var endpoint = Mark("dumbbell-end"); var legend = Mark("legend-dumbbell-end");
            Assert.Equal((string?)legend.Attribute("stroke"), (string?)endpoint.Attribute("stroke"));
            Assert.Equal((string?)legend.Attribute("stroke-width"), (string?)endpoint.Attribute("stroke-width"));
            var hidden = mode is "unconfigured" or "explicit-zero";
            Assert.Equal(hidden ? "none" : mode == "dash-only" ? (string?)endpoint.Attribute("fill") : "#000000", (string?)endpoint.Attribute("stroke"));
            Assert.Equal(hidden ? "0" : mode == "explicit-four" ? "4" : "1", (string?)endpoint.Attribute("stroke-width"));
            Assert.Equal("1", (string?)Mark("dumbbell-start").Attribute("stroke-width"));
            if (mode == "dash-only") Assert.Equal("3 2", (string?)endpoint.Attribute("stroke-dasharray"));
            series.Dumbbell.EndMarker.OutlineStroke = new ChartStrokeStyle(width: 0);
            Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
            if (!hidden) Assert.NotEqual(png, chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToPng());
            if (mode is "endpoint-color" or "dash-only") ComparisonOrientationTests.Save("outline-" + mode + "-" + (horizontal ? "horizontal" : "vertical"), prepared);
        }
    }
}
