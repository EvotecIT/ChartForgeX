# Export API

ChartForgeX keeps export APIs grouped by output intent:

| API shape | Purpose |
| --- | --- |
| `ToSvg`, `ToHtmlFragment`, `ToHtmlPage`, `ToPng` | Return the main report-grade formats directly. |
| `SaveSvg`, `SaveHtml`, `SavePng` | Save the main formats explicitly. |
| `ToBmp`, `ToPpm`, `ToTiff`, `ToRasterImage` | Return opaque flattened raster formats from the shared raster buffer. |
| `WriteBmp`, `WritePpm`, `WriteTiff`, `WriteRasterImage` | Stream opaque raster formats without forcing callers to allocate the final byte array themselves. |
| `SaveBmp`, `SavePpm`, `SaveTiff`, `SaveRasterImage` | Save opaque raster formats explicitly or by raster extension. |
| `SaveImage`, `Save` | Convenience methods that infer the output format from the file extension. |

## Extension Inference

`SaveImage` and `Save` infer the format from the output path:

- `.svg` uses `SaveSvg`
- `.html` and `.htm` use `SaveHtml`
- `.png` uses `SavePng`
- `.bmp`, `.ppm`, `.tiff`, and `.tif` use `SaveRasterImage`

Unsupported extensions fail with an `ArgumentException`; empty extensions fail before a file is opened.

`RasterImageFormatExtensions.TryFromFileExtension(...)` is the non-throwing helper for opaque raster formats only. It intentionally does not return SVG, HTML, or PNG because those are first-class renderer outputs rather than flattened opaque raster encoders.

## Raster Options

`RasterImageOptions` applies to opaque raster formats, where transparent pixels must be flattened against a background color. PNG export stays on the PNG renderer path and keeps the existing chart/grid PNG options such as output scale.

## File Safety

File save helpers render and validate the output before opening the destination file for generic raster saves. Failed renders should not truncate an existing destination file.
