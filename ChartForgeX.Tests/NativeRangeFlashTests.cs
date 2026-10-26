using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRangeFlashTests {
    [Theory]
    [InlineData(false, "lower")] [InlineData(false, "upper")] [InlineData(false, "tuples")] [InlineData(false, "series")]
    [InlineData(true, "lower")] [InlineData(true, "upper")] [InlineData(true, "tuples")] [InlineData(true, "series")]
    public void NativeFlashUsesWholeChangedSeriesAndBothChangedTupleMarkersWithExactAcceptedGeometry(bool area, string change) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeFlashFixture.Section(renderer, area, false, change);
        var to = NativeRangeFlashFixture.Section(renderer, area, true, change); var peak = ChartColor.FromHex("#FFC000");
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, peak);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 5, peak));
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "range-flash-native-" + area + "-" + change);
        Assert.Same(to.PreparedVisual, frames[0]); Assert.Same(to.PreparedVisual, frames[^1]);
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[0].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].ToPng());
        Assert.NotEqual(to.PreparedVisual.ToPng(), frames[2].ToPng()); AssertOwnership(to.PreparedVisual, frames[2], change, peak);
        var owners = NativeRangeFlashFixture.Owned(to.PreparedVisual);
        Assert.Contains(owners, row => row.Node.Role == "data-label");
        // Two real two-observation runs retain separate fills and both boundaries; no synthetic segment joins.
        Assert.Equal(2, owners.Count(row => row.Series == "delivery" && row.Node.Role == (area ? "range-area" : "range-band")));
        foreach (var role in new[] { "range-upper", "range-lower", "range-upper-halo", "range-lower-highlight" })
            Assert.Equal(2, owners.Count(row => row.Series == "delivery" && row.Node.Role == role));
        Assert.Equal(area ? 2 : 0, owners.Count(row => row.Series == "delivery" && row.Node.Role == "range-midline"));
        Assert.Contains(owners, row => row.Series == "delivery" && row.Node.Role == (area ? "range-area-pattern" : "range-band-pattern"));
        foreach (var key in new[] { "current", "later", "disconnected", "tail" })
            Assert.Equal(2, owners.Count(row => row.Series == "delivery" && row.Observation == key && row.Node.Role == "range-marker"));
        Assert.Contains(owners, row => row.Series == "delivery" && row.Observation == "current" && row.Node.Role == "range-marker-pattern");
        using var json = JsonDocument.Parse(payload); var target = NativeRawValueSourceMotionTests.Metadata(to).GetProperty("nativeValueSource");
        Assert.Equal("cartesian-component-value", json.RootElement.GetProperty("sourceKind").GetString());
        Assert.Equal(16, json.RootElement.GetProperty("readings").GetArrayLength());
        foreach (var frame in json.RootElement.GetProperty("drawings").GetProperty("wide").GetProperty("frames").EnumerateArray()) {
            var source = frame.GetProperty("nativeValueSource"); Assert.Equal(target.GetProperty("sourceKey").GetString(), source.GetProperty("sourceKey").GetString());
            Assert.Equal(target.GetProperty("readings").EnumerateArray().Select(row => row.GetProperty("value").GetDouble()),
                source.GetProperty("readings").EnumerateArray().Select(row => row.GetProperty("value").GetDouble()));
        }
    }

    internal static void AssertOwnership(PreparedVisual target, PreparedVisual actual, string change, ChartColor peak) {
        var owners = NativeRangeFlashFixture.Owned(target); Assert.Equal(owners.Count, actual.Scene.Nodes.Count); var painted = 0;
        for (var index = 0; index < owners.Count; index++) {
            var row = owners[index]; var node = row.Node; var observed = actual.Scene.Nodes[index];
            var seriesChanged = row.Series == "delivery" || change == "series" && row.Series == "comparison";
            var expected = node is VisualSceneMark && (row.Point ? NativeRangeFlashFixture.Changed(row.Series, row.Observation, change)
                : seriesChanged && node.Role?.StartsWith("range-", StringComparison.Ordinal) == true);
            if (!expected) { Assert.Same(node, observed); continue; }
            painted++; Assert.NotSame(node, observed); var before = Assert.IsAssignableFrom<VisualSceneMark>(node); var after = Assert.IsAssignableFrom<VisualSceneMark>(observed);
            Geometry(before, after); Assert.Equal(before.Fill.HasValue, after.Fill.HasValue); Assert.Equal(before.Stroke.HasValue, after.Stroke.HasValue);
            if (before.Fill.HasValue) Assert.Equal(peak, after.Fill!.Value); if (before.Stroke.HasValue) Assert.Equal(peak, after.Stroke!.Value);
        }
        Assert.True(painted > 0); Assert.Same(target.Scene.DataProjection, actual.Scene.DataProjection);
        Assert.Equal(target.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)), actual.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)));
        Assert.Equal(target.Accessibility.Description, actual.Accessibility.Description);
        Assert.True(VisualSeriesVisibilityCapture.SameAxis(target.XProjection, actual.XProjection)); Assert.True(VisualSeriesVisibilityCapture.SameAxis(target.YProjection, actual.YProjection));
    }

    private static void Geometry(VisualSceneMark before, VisualSceneMark after) {
        Assert.Equal(before.GetType(), after.GetType()); Assert.Equal((before.Role, before.Id, before.Opacity, before.StrokeWidth), (after.Role, after.Id, after.Opacity, after.StrokeWidth));
        switch (before) {
            case VisualScenePath path:
                var actualPath = Assert.IsType<VisualScenePath>(after); Assert.Equal(path.Commands, actualPath.Commands);
                Assert.Equal((path.Close, path.Cap, path.Join, path.DashOffset), (actualPath.Close, actualPath.Cap, actualPath.Join, actualPath.DashOffset)); Assert.Equal(path.Dash, actualPath.Dash); break;
            case VisualSceneLine line:
                var actualLine = Assert.IsType<VisualSceneLine>(after); Assert.Equal((line.Start, line.End, line.DashOffset), (actualLine.Start, actualLine.End, actualLine.DashOffset)); Assert.Equal(line.Dash, actualLine.Dash); break;
            case VisualSceneEllipse ellipse:
                var actualEllipse = Assert.IsType<VisualSceneEllipse>(after); Assert.Equal((ellipse.Cx, ellipse.Cy, ellipse.Rx, ellipse.Ry), (actualEllipse.Cx, actualEllipse.Cy, actualEllipse.Rx, actualEllipse.Ry)); break;
            default: throw new InvalidOperationException("The interval fixture must use actual path, hatch or endpoint marker ink.");
        }
    }
}
