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

GIF uses a shared adaptive palette and error diffusion. It rounds frame durations to the nearest 10 milliseconds, with a 10 millisecond minimum and 655.35 second maximum. Alpha below 128 is transparent; other pixels are opaque. APNG preserves RGBA pixels and exact durations when its 16-bit rational timing fields can represent them. Other durations round to a supported fraction; the maximum is 65,535 seconds.

Both methods accept a `CancellationToken`. `WriteTo` leaves the destination stream open and validates frame dimensions, durations, playback count, and the memory budget before writing. Cancellation or an I/O failure during encoding can leave partial output, so callers that need atomic files should write to a temporary file and replace the destination after success. Encoder working buffers and returned output share a 256 MiB ceiling, excluding caller-owned input pixels; stream output avoids retaining the complete encoded animation.
