using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeOpacityDefaultControls {
    [Theory]
    [InlineData(1, 1)] [InlineData(1, 2)] [InlineData(2, 1)] [InlineData(2, 2)]
    public void CorePrimitiveScenesRetainDefaultSvgIdentityAndPngBytes(int density, int coverage) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Native opacity controls require absolute task-owned output.");
            Directory.CreateDirectory(output);
        }
        for (var sample = 0; sample < 8; sample++) {
            var prepared = new PreparedVisual(Scene(sample)); var normal = prepared.ToSvg(); var fixedSvg = prepared.ToSvg("opacity-control");
            var png = prepared.ToPng(new VisualRenderOptions(scale: density, supersampling: coverage));
            if (!string.IsNullOrWhiteSpace(output)) {
                var stem = Path.Combine(output, "native-opacity-default-" + density + "-" + coverage + "-" + sample);
                File.WriteAllText(stem + ".svg", normal); File.WriteAllText(stem + ".fixed.svg", fixedSvg); File.WriteAllBytes(stem + ".png", png);
            }
            Assert.NotEmpty(png);
            Assert.Equal(normal, prepared.ToSvg()); Assert.Equal(fixedSvg, prepared.ToSvg("opacity-control"));
        }
    }

    internal static VisualScene Scene(int sample) {
        var builder = new VisualSceneBuilder(new VisualSize(32, 24), FontSpec.FromFamily(BrowserProofFonts.Family));
        var red = ChartColor.FromHex("#CC2030"); var blue = ChartColor.FromHex("#2458C2");
        if (sample == 0) builder.Rect(new ChartRect(2, 2, 16, 16), red, blue, 4, role: "sample-rect");
        else if (sample == 1) builder.Ellipse(12, 12, 8, 7, red, blue, 4, "sample-ellipse");
        else if (sample == 2) builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(2, 2), ChartPathCommand.LineTo(24, 3), ChartPathCommand.LineTo(12, 20) }), red, blue, 4, "sample-path", close: true);
        else if (sample == 3) using (builder.PushGroup("pair", "sample-pair")) {
            builder.Rect(new ChartRect(2, 2, 16, 16), red); builder.Rect(new ChartRect(8, 4, 16, 16), blue);
        } else if (sample == 4) using (builder.PushGroup("nested", "sample-nested")) using (builder.PushClip(new ChartRect(4, 4, 12, 12))) {
            builder.Rect(new ChartRect(0, 0, 24, 22), red); builder.Line(0, 4, 28, 18, blue, 4);
        } else if (sample == 5) using (builder.PushTranslation(3, 1)) using (builder.PushRotation(20, 12, 12)) {
            builder.Rect(new ChartRect(2, 2, 16, 16), red, blue, 3);
        } else if (sample == 6) {
            builder.Text("Native", 2, 16, 8, blue, 700); builder.Line(2, 18, 28, 18, red, 2);
        } else {
            var image = new VisualSceneImageData(new RgbaImage(1, 1, new byte[] { 204, 32, 48, 255 }));
            builder.Image(image, new ChartRect(3, 3, 14, 14)); builder.Image(image, new ChartRect(10, 7, 14, 14));
        }
        return builder.Build();
    }
}
