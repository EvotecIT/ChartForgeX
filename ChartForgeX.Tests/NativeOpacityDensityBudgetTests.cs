using System;
using System.IO;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeOpacityDensityBudgetTests {
    [Fact]
    public void RealReturnedDensityArrayParticipatesInExistingCumulativeOpacityPreflight() {
        // The ordinary legend/group owner emits these same retained native isolation scopes.
        // Small ink keeps geometry work bounded while the real public export must still
        // reserve both complete group canvases and the actual returned output-density array.
        var builder = new VisualSceneBuilder(new VisualSize(2115, 2115), FontSpec.SystemSans());
        using (builder.PushGroup("outer", "density-object", opacity: .5)) {
            using (builder.PushGroup("inner", "density-object", opacity: .5)) {
                builder.Rect(new ChartRect(1, 1, 1, 1), ChartColor.Black);
            }
        }
        var prepared = new PreparedVisual(builder.Build());
        var svg = prepared.ToSvg("density-budget");
        var control = prepared.ToRgba(new VisualRenderOptions(scale: 1, supersampling: 1));
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "opacity-density-budget-before-assert.svg"), svg);
            File.WriteAllBytes(Path.Combine(output, "opacity-density-budget-control.png"), PngWriter.WriteRgba(control));
        }
        Assert.Equal(2115, control.Width);
        Assert.Equal(64, control.Pixels[(control.Width + 1) * 4 + 3]);
        // Density2 needs more than the established256MiB cumulative pixel-array ceiling;
        // its actual source/return array cannot be debited as a density1 image. The public
        // pixel cap and work ceiling still admit this pose, so this is the live-array guard.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            prepared.ToRgba(new VisualRenderOptions(scale: 2, supersampling: 1)));
        Assert.Equal(svg, prepared.ToSvg("density-budget"));
    }
}
