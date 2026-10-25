using System;
using ChartForgeX.Raster;

internal static partial class Program {
    private static void AnimationContainers() {
        var image = new RgbaImage(2, 1, new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 });
        var frame = new RasterAnimationFrame(image, TimeSpan.FromSeconds(1));
        var source = new RasterAnimationSource(2, 1, 1, (_, _) => frame);
        foreach (var format in new[] { RasterAnimationFormat.Gif, RasterAnimationFormat.Apng }) {
            var bytes = RasterAnimationEncoder.Encode(source, format,
                new RasterAnimationOptions { PlayCount = 1 });
            PackageAssertions.Require(bytes.Length > 32, "Core-only animation output is missing.");
        }
        PackageAssertions.Require(typeof(RasterAnimationEncoder).Assembly == typeof(ChartForgeX.Core.Chart).Assembly,
            "Animation encoding requires an adapter assembly.");
    }
}
