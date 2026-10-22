using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ExtremeAxisTickLayoutTests {
    [Theory]
    [InlineData("x", false)]
    [InlineData("x", true)]
    [InlineData("primary-y", false)]
    [InlineData("primary-y", true)]
    [InlineData("secondary-y", false)]
    [InlineData("secondary-y", true)]
    [InlineData("horizontal-value", false)]
    [InlineData("horizontal-value", true)]
    public void FiniteSignedDomainsRetainUsableInteriorTicksAtTheirPaintedCoordinates(string axisRole, bool reversed) {
        const double minimum = -1e308, maximum = 1e308;
        var chart = Chart.Create().WithSize(1000, 600).WithHeader(false).WithLegend(false)
            .WithPadding(20, 20, 20, 20)
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        ChartAxis axis;
        var horizontal = axisRole is "x" or "horizontal-value";
        if (axisRole == "horizontal-value") {
            chart.AddHorizontalBar("Values", new[] { new ChartPoint(1, minimum), new ChartPoint(2, maximum) });
            axis = chart.Options.XAxis;
        } else if (axisRole == "x") {
            chart.AddLine("Values", new[] { new ChartPoint(minimum, 1), new ChartPoint(0, 2), new ChartPoint(maximum, 3) });
            axis = chart.Options.XAxis;
        } else {
            chart.AddLine("Values", new[] { new ChartPoint(1, minimum), new ChartPoint(2, 0), new ChartPoint(3, maximum) });
            axis = axisRole == "secondary-y" ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            if (axisRole == "secondary-y") { chart.Series[0].UseSecondaryValueAxis(); chart.Options.YAxis.Visible = false; }
        }
        if (axisRole != "horizontal-value") chart.Series[0].Markers.Enabled = false;
        axis.WithBounds(minimum, maximum); axis.TickCount = 5; axis.Reversed = reversed;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "extreme-axis-" + axisRole + (reversed ? "-reversed" : "-forward"));
        var role = horizontal ? "axis-x-label" : axisRole == "secondary-y" ? "axis-secondary-y-label" : "axis-y-label";
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();
        var facts = prepared.Regions.Where(region => region.Role == role).ToArray();
        Assert.Equal(5, captions.Length); Assert.Equal(5, facts.Length);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.axis-label-overflow");
        var expected = new[] { minimum, -5e307, 0, 5e307, maximum };
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == role.Replace("-label", ""));
        var svg = XDocument.Parse(prepared.ToSvg());
        var svgCaptions = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
        Assert.Equal(captions.Select(node => node.Text.Lines.Single().Text), svgCaptions.Select(node => node.Value));
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        for (var index = 0; index < expected.Length; index++) {
            var fact = facts[index]; var caption = captions[index];
            var split = fact.Label!.LastIndexOf(" (", StringComparison.Ordinal);
            Assert.Equal(expected[index], double.Parse(fact.Label[(split + 2)..^1], CultureInfo.InvariantCulture));
            var ratio = index / 4d; if (reversed) ratio = 1 - ratio;
            var position = horizontal ? track.Start.X + (track.End.X - track.Start.X) * ratio
                : track.End.Y - (track.End.Y - track.Start.Y) * ratio;
            var actual = horizontal ? caption.X + caption.Text.Metrics.Width / 2
                : caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height / 2;
            Assert.True(double.IsFinite(actual));
            Assert.InRange(Math.Abs(actual - position), 0, .5);
            Assert.Equal(position, horizontal ? fact.Bounds.Left : fact.Bounds.Top, 7);
            var bounds = new ChartRect(caption.X, caption.Baseline - caption.Text.Ascent, caption.Text.Metrics.Width, caption.Text.Metrics.Height);
            Assert.True(HasTextInk(prepared, image, caption, bounds), "Every retained tick caption must paint at its prepared SVG position.");
        }
    }

    private static bool HasTextInk(PreparedVisual prepared, ChartForgeX.Raster.RgbaImage image, VisualSceneText caption, ChartRect bounds) {
        var scene = prepared.Scene;
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => !ReferenceEquals(node, caption)).ToArray(), scene.Diagnostics, scene.Regions), supersampling: 1);
        var left = Math.Max(0, (int)Math.Floor(bounds.Left)); var right = Math.Min(image.Width, (int)Math.Ceiling(bounds.Right));
        var top = Math.Max(0, (int)Math.Floor(bounds.Top)); var bottom = Math.Min(image.Height, (int)Math.Ceiling(bounds.Bottom));
        for (var y = top; y < bottom; y++) for (var x = left; x < right; x++) {
            var offset = (y * image.Width + x) * 4;
            for (var channel = 0; channel < 4; channel++)
                if (image.Pixels[offset + channel] != without.Pixels[offset + channel]) return true;
        }
        return false;
    }
}
