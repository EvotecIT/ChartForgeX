using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class StageConeCaptionContrastTests {
    [Theory]
    [InlineData(ChartOrientation.Vertical, 390, VisualThemeMode.Light)]
    [InlineData(ChartOrientation.Vertical, 800, VisualThemeMode.Dark)]
    [InlineData(ChartOrientation.Horizontal, 390, VisualThemeMode.Dark)]
    [InlineData(ChartOrientation.Horizontal, 800, VisualThemeMode.Light)]
    public void AutomaticFirstConeValueContrastsWithItsActualUnpaintedFrameRatherThanAnOpaqueStage(ChartOrientation orientation, int width, VisualThemeMode mode) {
        var chart = StagePresentationOptionsTests.Create(false, ChartFunnelForm.Cone, orientation, width, 640).WithTheme(VisualTheme.Graphite(), mode);
        chart.ConfigureFunnel(options => {
            options.LabelContent = ChartStageLabelContent.Separate; options.CategoryLabel.Visible = false;
            options.RetentionLabel.Visible = false; options.DropOffLabel.Visible = false; options.ValueLabel.Style.FontSize = 13;
        });
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"causal-cone-first-value-{orientation}-{width}-{mode}");
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Id == "series-0-point-0-value-label");
        var bounds = prepared.Regions.Single(region => region.Id == value.Id).Bounds;
        var scene = prepared.Scene;
        var withoutText = new VisualScene(scene.Size, scene.Nodes.Where(node => node is not VisualSceneText).ToArray(), scene.Diagnostics, scene.Regions, scene.DataProjection);
        var background = VisualSceneRasterRenderer.Render(withoutText);
        var x = (int)Math.Floor(bounds.Left + bounds.Width / 2); var y = (int)Math.Floor(bounds.Top + bounds.Height / 2);
        var offset = (y * background.Width + x) * 4;
        var surface = ChartColor.FromRgba(background.Pixels[offset], background.Pixels[offset + 1], background.Pixels[offset + 2], background.Pixels[offset + 3]);
        Assert.Equal(255, surface.A);
        var ratio = ChartColorMath.ContrastRatio(value.Color, surface);
        Assert.True(ratio >= 4.5, $"Automatic Cone value {value.Color.ToCss()} has only {ratio.ToString("F3", CultureInfo.InvariantCulture)}:1 over actual native {surface.ToCss()}.");
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(value.Color.ToCss(), svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-source-id") == value.Id)
            .Descendants().Single(node => node.Name.LocalName == "text").Attribute("fill")!.Value);
        Assert.Equal(100, chart.Series[0].Points[0].Y); Assert.NotEmpty(prepared.ToPng());
    }
}
