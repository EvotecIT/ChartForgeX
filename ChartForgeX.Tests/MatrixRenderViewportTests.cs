using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Host-sized matrices use the resolved frame viewport for rotated captions in both static painters.</summary>
public sealed class MatrixRenderViewportTests {
    private static readonly string[] Captions = { "AuthenticationPolicy", "AuthorizationPolicy", "ConfigurationPolicy", "ConnectivityPolicy",
        "ObservabilityPolicy", "SynchronizationPolicy", "InfrastructurePolicy", "RecoveryPolicy" };

    [Theory]
    [InlineData(false, 240, 160, 900, 500, null, VisualThemeMode.Light)]
    [InlineData(false, 900, 500, 320, 280, null, VisualThemeMode.Dark)]
    [InlineData(true, 240, 160, 900, 500, null, VisualThemeMode.Dark)]
    [InlineData(true, 900, 500, 320, 280, null, VisualThemeMode.Light)]
    [InlineData(false, 240, 160, 900, 500, -45d, VisualThemeMode.Dark)]
    [InlineData(false, 900, 500, 320, 280, 45d, VisualThemeMode.Light)]
    [InlineData(true, 240, 160, 900, 500, 45d, VisualThemeMode.Light)]
    [InlineData(true, 900, 500, 320, 280, -45d, VisualThemeMode.Dark)]
    public void RotatedCaptionsFollowContextSizeInsteadOfAuthoredSize(bool hexagonal, int modelWidth, int modelHeight,
        int width, int height, double? authoredAngle, VisualThemeMode mode) {
        var chart = Create(hexagonal, modelWidth, modelHeight, authoredAngle);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height), new ChartPadding(31, 17, 43, 29)),
            VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 13.5)), mode,
            new VisualFrame("Service health", "", showLegend: false),
            FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));
        var actual = chart.Prepare(context);
        var expected = Create(hexagonal, width, height, authoredAngle).Prepare(context);
        var capture = $"matrix-viewport-{hexagonal}-{width}-{authoredAngle?.ToString() ?? "auto"}-{mode}";
        ScatterBubbleColorRenderingTests.Capture(actual, capture);
        ScatterBubbleColorRenderingTests.Capture(expected, capture + "-reference");

        var actualLabels = Labels(actual);
        var expectedLabels = Labels(expected);
        Assert.NotEmpty(actualLabels);
        Assert.Equal(expectedLabels.Select(Text), actualLabels.Select(Text));
        Assert.Equal(expected.Regions.Where(region => region.Role == "heatmap-column-label").Select(region => region.Bounds),
            actual.Regions.Where(region => region.Role == "heatmap-column-label").Select(region => region.Bounds));
        Assert.Equal(Captions, actual.Regions.Where(region => region.Role == "heatmap-column-label").Select(region => region.Label));
        if (width == 900) Assert.Equal(Captions, actualLabels.Select(Text));

        var document = XDocument.Parse(actual.ToSvg());
        var rotations = document.Descendants().Select(element => (string?)element.Attribute("transform")).Where(value => value?.Contains("rotate(") == true);
        Assert.Contains(rotations, value => value!.StartsWith("rotate(" + (authoredAngle ?? -45).ToString(System.Globalization.CultureInfo.InvariantCulture) + " ", StringComparison.Ordinal));
        Assert.Equal(actualLabels.Select(Text), document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "heatmap-column-label")
            .Select(element => string.Join(" ", element.Descendants().Where(child => child.Name.LocalName == "text").Select(child => child.Value))));
        Assert.Equal(expected.ToSvg(), actual.ToSvg());
        Assert.Equal(expected.ToPng(new VisualRenderOptions(supersampling: 1)), actual.ToPng(new VisualRenderOptions(supersampling: 1)));

        var image = actual.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var (text, corners) in Geometry(actual)) {
            Assert.All(corners, point => {
                Assert.InRange(point.X, 31 - .001, width - 43 + .001);
                Assert.InRange(point.Y, 17 - .001, height - 29 + .001);
            });
            Assert.True(HasInk(actual, image, text, corners), "The native PNG must paint the rotated caption inside its measured SVG footprint: " + Text(text));
        }
        Assert.Equal(modelWidth, chart.Options.Size.Width);
        Assert.Equal(modelHeight, chart.Options.Size.Height);
    }

    private static Chart Create(bool hexagonal, int width, int height, double? angle) {
        var chart = Chart.Create().WithSize(width, height).WithXLabels(Captions).WithDataLabels(false);
        if (hexagonal) chart.AddHexbinHeatmapRow("Service", Enumerable.Range(1, Captions.Length).Select(index => (double)index).ToArray());
        else chart.AddHeatmapRow("Service", Enumerable.Range(1, Captions.Length).Select(index => (double)index).ToArray());
        if (angle.HasValue) chart.Options.XAxisLabelAngle = angle.Value;
        return chart;
    }

    private static VisualSceneText[] Labels(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == "heatmap-column-label").ToArray();
    private static string Text(VisualSceneText text) => string.Join(" ", text.Text.Lines.Select(line => line.Text));

    private static IEnumerable<(VisualSceneText Text, ChartPoint[] Corners)> Geometry(PreparedVisual prepared) {
        var stack = new Stack<VisualSceneTransform>(); var transform = VisualSceneTransform.Identity;
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) {
                stack.Push(transform);
                if (group.Rotation.HasValue) transform = transform.Rotate(group.Rotation.Value);
                if (group.Translation.HasValue) transform = transform.Translate(group.Translation.Value);
            } else if (node is VisualSceneEndGroup) transform = stack.Pop();
            else if (node is VisualSceneText text && text.Role == "heatmap-column-label") {
                var left = text.LineLeft(text.Text.Lines[0]); var top = text.Baseline - text.Text.Ascent;
                var right = left + text.Text.Metrics.Width; var bottom = top + text.Text.Metrics.Height;
                yield return (text, new[] { new ChartPoint(left, top), new ChartPoint(right, top), new ChartPoint(right, bottom), new ChartPoint(left, bottom) }.Select(transform.Apply).ToArray());
            }
        }
    }

    private static bool HasInk(PreparedVisual prepared, ChartForgeX.Raster.RgbaImage image, VisualSceneText text, ChartPoint[] corners) {
        // Isolate this run's contribution without assuming a rotated glyph contains an opaque foreground pixel.
        var scene = prepared.Scene;
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => !ReferenceEquals(node, text)).ToArray(), scene.Diagnostics, scene.Regions), supersampling: 1);
        var left = Math.Max(0, (int)Math.Floor(corners.Min(point => point.X)));
        var top = Math.Max(0, (int)Math.Floor(corners.Min(point => point.Y)));
        var right = Math.Min(image.Width, (int)Math.Ceiling(corners.Max(point => point.X)));
        var bottom = Math.Min(image.Height, (int)Math.Ceiling(corners.Max(point => point.Y)));
        for (var y = top; y < bottom; y++)
            for (var x = left; x < right; x++) {
                var offset = (y * image.Width + x) * 4;
                for (var channel = 0; channel < 4; channel++)
                    if (image.Pixels[offset + channel] != without.Pixels[offset + channel]) return true;
            }
        return false;
    }
}
