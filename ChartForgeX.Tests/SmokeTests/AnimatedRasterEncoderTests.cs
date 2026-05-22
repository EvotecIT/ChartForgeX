using System;
using System.IO;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AnimatedRasterEncoderDispatchesFormats() {
        var frames = AnimatedRasterFrames.Create(new[] {
            SolidFrame(8, 8, 12, 18, 24),
            SolidFrame(8, 8, 37, 99, 235)
        }, 5, loop: false, formatName: "test");

        var gif = AnimatedRasterEncoder.Encode(AnimatedRasterFormat.Gif, frames);
        var apng = AnimatedRasterEncoder.Encode(AnimatedRasterFormat.Apng, frames);
        Assert(gif[0] == (byte)'G' && gif[1] == (byte)'I' && gif[2] == (byte)'F', "Animated raster encoder should dispatch GIF output through the GIF writer.");
        Assert(apng[0] == 137 && apng[1] == 80 && apng[2] == 78 && apng[3] == 71, "Animated raster encoder should dispatch APNG output through the APNG writer.");
        Assert(AnimatedRasterFormatExtensions.TryFromFileExtension(".gif", out var gifFormat) && gifFormat == AnimatedRasterFormat.Gif, "Animated raster format metadata should resolve GIF extensions.");
        Assert(AnimatedRasterFormatExtensions.TryFromFileExtension(" .APNG ", out var apngFormat) && apngFormat == AnimatedRasterFormat.Apng, "Animated raster format metadata should resolve APNG extensions case-insensitively.");
        Assert(!AnimatedRasterFormatExtensions.TryFromFileExtension(".webp", out _), "Animated raster format metadata should reject unsupported extensions until an encoder exists.");
        Assert(AnimatedRasterFormat.Gif.GetDisplayName() == "GIF" && AnimatedRasterFormat.Apng.GetDisplayName() == "APNG", "Animated raster format metadata should keep reusable display names centralized.");
        AssertThrows<ArgumentNullException>(() => AnimatedRasterEncoder.Write(null!, AnimatedRasterFormat.Gif, frames), "Animated raster encoder should reject null streams.");
        AssertThrows<ArgumentNullException>(() => AnimatedRasterEncoder.Write(Stream.Null, AnimatedRasterFormat.Gif, null!), "Animated raster encoder should reject null frame collections.");
        AssertThrows<ArgumentOutOfRangeException>(() => AnimatedRasterEncoder.Write(Stream.Null, (AnimatedRasterFormat)99, frames), "Animated raster encoder should reject unsupported formats.");
    }
}
