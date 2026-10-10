# Encoding RGBA animation frames

`RasterAnimationEncoder` writes complete RGBA canvases as GIF or animated PNG. Use it for frames produced by `ImageComposition`, chart renderers, or another image library. It shares the same palette, frame optimization, and encoding engines as visual stories and terminal presentations.

Install `ChartForgeX.Stories` to use the encoder, frame, format and playback types in the `ChartForgeX.Raster` namespace. `RgbaImage` belongs to Core. The example below also uses `ChartForgeX.Visuals` for image composition.

```csharp
using System;
using System.IO;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

var first = ImageComposition.Create(320, 180, ChartColor.White)
    .FillCircle(80, 90, 30, ChartColor.FromHex("#2563eb"))
    .ToImage();
var second = ImageComposition.Create(320, 180, ChartColor.White)
    .FillCircle(240, 90, 30, ChartColor.FromHex("#f97316"))
    .ToImage();

var frames = new[] {
    new RasterAnimationFrame(first, TimeSpan.FromMilliseconds(250)),
    new RasterAnimationFrame(second, TimeSpan.FromMilliseconds(750))
};
var playback = new RasterAnimationOptions { PlayCount = 3 };

File.WriteAllBytes("animation.gif",
    RasterAnimationEncoder.Encode(frames, RasterAnimationFormat.Gif, playback));
using var output = File.Create("animation.apng");
RasterAnimationEncoder.WriteTo(output, frames, RasterAnimationFormat.Apng, playback);
```

`PlayCount` describes total plays: `0` repeats indefinitely, `1` plays once, and `3` plays three times. GIF supports at most 65,536 total plays; APNG supports up to `int.MaxValue`. Omitted options repeat indefinitely.

Every frame describes the entire canvas and must have the same dimensions. Resize or place smaller images on a shared canvas before constructing the frame list. The encoder crops unchanged areas internally, while keeping the decoded result equivalent to each complete input frame. A single frame is supported. The frame list and RGBA buffers remain owned by the caller and must stay unchanged until encoding returns.

GIF uses a shared adaptive palette and error diffusion. It preserves an explicit zero delay. Positive frame boundaries round to the nearest 10 milliseconds without accumulating cadence drift, with a 10 millisecond minimum and 655.35 second maximum. Frames shorter than the minimum use 10 milliseconds without shortening later frames to compensate. Alpha below 128 is transparent; other pixels are opaque. APNG preserves RGBA pixels and zero durations, and retains exact positive durations when its 16-bit rational timing fields can represent them. Other durations round to a supported fraction; the maximum is 65,535 seconds. Negative or over-format-bound durations are rejected before output. Playback applications may apply their own display minimum to an encoded zero delay.

The equally timed image-array `ToGif` and `ToApng` conveniences follow the same duration rules. Their centisecond delay is explicit: zero stays zero, and invalid negative or excessive values raise an error instead of being silently clamped. Story frame-rate and transition policies retain their positive timing requirements.

For APNG, `RasterAnimationOptions.PngCompressionLevel` uses the same profiles as static PNG output: 0 stores uncompressed deflate blocks, 1 through 3 select the fastest profile, and 4 through 9 select the optimal profile. The default is 6. This controls compression effort and size, while retaining pixels and frame timing; the levels do not represent nine distinct deflate implementations.

Both methods accept a `CancellationToken`. `WriteTo` leaves the destination stream open and validates frame dimensions, durations, playback count, and the memory budget before writing. Cancellation or an I/O failure during encoding can leave partial output, so callers that need atomic files should write to a temporary file and replace the destination after success. Encoder working buffers and returned output share a 256 MiB ceiling, excluding caller-owned input pixels; stream output avoids retaining the complete encoded animation.

For long animations, supply a `RasterAnimationSource` with known dimensions, count and a deterministic frame callback. The encoder releases pixels between requests instead of retaining the full frame list. GIF reads each index twice to build and use a shared palette; APNG reads it once. `GetFrame` validates each produced image and its duration. Producer failure can leave partial stream output.

`RasterAnimationSource.AdditionalWorkingBytes` declares retained assets and producer working buffers beyond the returned frame. Those bytes share the 256 MiB budget with frame and encoder buffers. `PreparedVisualStory.FrameSource(format, options)` supplies this estimate and the selected container's Stories timing/readability policy; generic producers should supply their own conservative estimate. See [story playback](stories.md) for streaming directly from a prepared story.
