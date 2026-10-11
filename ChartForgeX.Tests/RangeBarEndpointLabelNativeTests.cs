using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarEndpointLabelNativeTests {
    [Theory]
    [InlineData("endpoint-labels", false)]
    [InlineData("compact-endpoint-labels", true)]
    [InlineData("horizontal-endpoint-labels", false)]
    [InlineData("compact-horizontal-endpoint-labels", true)]
    public void ExecutableGalleryModelsPaintEndpointInkAndRetainDescendingCustomHeadings(string variant, bool dark) {
        var compact = variant.StartsWith("compact", StringComparison.Ordinal);
        var chart = V2GalleryModels.Create(ChartSeriesKind.RangeBar, variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(compact ? 360 : 800, compact ? 360 : 440).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var prepared = RangeBarEndpointLabelTests.Prepare(chart);
        var labels = RangeBarEndpointLabelTests.Labels(prepared);
        var point = RangeBarEndpointLabelTests.Points(prepared).Single(group => group.Metadata["data-cfx-series"] == "1" && group.Metadata["data-cfx-point"] == "1");
        Assert.Equal("Recorded express delivery", point.Metadata["data-cfx-label"]);
        Assert.Equal("32 S", point.Metadata["data-cfx-start-label"]); Assert.Equal("12 E", point.Metadata["data-cfx-end-label"]);
        Assert.Equal("32", point.Metadata["data-cfx-start"]); Assert.Equal("12", point.Metadata["data-cfx-end"]);
        Assert.Equal(6, RangeBarEndpointLabelTests.Points(prepared).Length);
        Assert.Equal(12, labels.Length);
        Assert.Contains(labels, label => label.Id!.EndsWith("-start-label", StringComparison.Ordinal));
        Assert.Contains(labels, label => label.Id!.EndsWith("-end-label", StringComparison.Ordinal));
        var rgba = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Equal(compact ? 360 : 800, rgba.Width); Assert.Equal(compact ? 360 : 440, rgba.Height);
        var bounds = labels.Select(RangeBarEndpointLabelTests.Bounds).ToArray();
        Assert.All(bounds, box => Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, rgba.Width, rgba.Height), box)));
        var hidden = RangeBarEndpointLabelTests.Prepare(chart.WithDataLabels(false)).ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Contains(bounds, box => HasChangedInk(box, rgba, hidden));
        Assert.NotEmpty(prepared.ToPng());
    }

    private static bool HasChangedInk(ChartRect box, ChartForgeX.Raster.RgbaImage visible, ChartForgeX.Raster.RgbaImage hidden) {
        for (var y = Math.Max(0, (int)Math.Floor(box.Top)); y < Math.Min(visible.Height, (int)Math.Ceiling(box.Bottom)); y++)
            for (var x = Math.Max(0, (int)Math.Floor(box.Left)); x < Math.Min(visible.Width, (int)Math.Ceiling(box.Right)); x++) {
                var pixel = (y * visible.Width + x) * 4;
                for (var channel = 0; channel < 4; channel++) if (visible.Pixels[pixel + channel] != hidden.Pixels[pixel + channel]) return true;
            }
        return false;
    }
}
