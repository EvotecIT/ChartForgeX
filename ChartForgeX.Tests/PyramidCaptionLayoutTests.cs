using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Dense pyramid rails retain useful complete captions without painting leaders to empty slots.</summary>
public sealed class PyramidCaptionLayoutTests {
    [Theory]
    [InlineData(390, 280, false, false, VisualThemeMode.Light)]
    [InlineData(390, 280, false, true, VisualThemeMode.Dark)]
    [InlineData(760, 440, false, false, VisualThemeMode.Dark)]
    [InlineData(390, 280, true, false, VisualThemeMode.Light)]
    [InlineData(760, 440, true, true, VisualThemeMode.Dark)]
    public void DenseStageCaptionsKeepCompleteNamesValuesSourceFactsAndOnlyUsefulLeaders(
        int width, int height, bool horizontal, bool reversed, VisualThemeMode mode) {
        var names = Enumerable.Range(1, 16).Select(index => "Regional service " + index).ToArray();
        var chart = Chart.Create().WithSize(width, height).WithTitle("Allocation by service")
            .WithXLabels(names).WithDataLabels().ConfigureDataLabelStyle(style => style.FontSize = 13.5)
            .ConfigurePyramid(options => { options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical; options.Reversed = reversed; })
            .AddPyramid("Allocation", Enumerable.Range(1, 16).Select(index => new ChartPoint(index, 17 - index)));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), themeMode: mode,
            frame: new VisualFrame(chart.Title));
        var prepared = chart.Prepare(context);
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "pyramid-label").ToArray();
        Assert.InRange(captions.Length, 1, 16);
        Assert.All(captions, caption => {
            Assert.Equal(13.5, caption.Text.Size);
            var index = int.Parse(caption.Id!.Split('-')[3], CultureInfo.InvariantCulture);
            Assert.Equal(names[index] + ": " + (16 - index), string.Join(" ", caption.Text.Lines.Select(line => line.Text)));
        });
        var bounds = captions.Select(caption => prepared.Regions.Single(region => region.Id == caption.Id).Bounds).ToArray();
        Assert.All(bounds, box => { Assert.InRange(box.Left, 0, width); Assert.InRange(box.Right, 0, width); Assert.InRange(box.Top, 0, height); Assert.InRange(box.Bottom, 0, height); });
        for (var first = 0; first < bounds.Length; first++) for (var second = first + 1; second < bounds.Length; second++)
            Assert.False(bounds[first].Left < bounds[second].Right && bounds[first].Right > bounds[second].Left
                && bounds[first].Top < bounds[second].Bottom && bounds[first].Bottom > bounds[second].Top);
        var document = XDocument.Parse(prepared.ToSvg());
        var stages = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "pyramid-stage").ToArray();
        Assert.Equal(16, stages.Length);
        Assert.Equal(names, stages.Select(stage => (string)stage.Attribute("data-cfx-label")!));
        Assert.Equal(Enumerable.Range(1, 16).Select(index => (17 - index).ToString(CultureInfo.InvariantCulture)),
            stages.Select(stage => (string)stage.Attribute("data-cfx-value")!));
        Assert.All(stages, stage => {
            if (!stage.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "pyramid-label"))
                Assert.DoesNotContain(stage.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "data-label-connector");
        });
        Assert.Equal(16, prepared.Regions.Count(region => region.Role == "pyramid-label"));
        if (captions.Length < 16) Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "pyramid.label-hidden");
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"pyramid-dense-{width}-{height}-{horizontal}-{reversed}-{mode}");
    }
}
