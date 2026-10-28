using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void CapturedMask(VisualArtifactInterchangeJsonWriter writer,
        VisualArtifactInterchangeMask mask) {
        writer.StartObject();
        Number(writer, "imageWidth", mask.ImageWidth);
        Number(writer, "imageHeight", mask.ImageHeight);
        Number(writer, "originX", mask.OriginX);
        Number(writer, "originY", mask.OriginY);
        Number(writer, "pixelsPerUnit", mask.PixelsPerUnit);
        String(writer, "mode", mask.Mode.ToString());
        Boolean(writer, "orderedDither", mask.OrderedDither);
        writer.Property("rgbaPixels");
        writer.Base64(mask.Pixels);
        writer.EndObject();
    }

    private static VisualArtifactInterchangeMask ReadCapturedMask(Dictionary<string, GeoJsonValue> item,
        ref long decodedBytes, ref int? firstDensity) {
        RequireEffectMembers(item, new[] { "imageWidth", "imageHeight", "originX", "originY",
            "pixelsPerUnit", "mode", "orderedDither", "rgbaPixels" }, "captured mask");
        var width = RequiredInt(item, "imageWidth");
        var height = RequiredInt(item, "imageHeight");
        // Validate dimensions and density before the first decoded pixel array allocates.
        _ = new RasterEffectRegion(0, 0, width, height);
        var density = RequiredInt(item, "pixelsPerUnit");
        _ = new VisualRenderOptions(scale: density);
        if (firstDensity.HasValue && firstDensity.Value != density)
            throw new ArgumentException("Ordered captured masks must share one native device density.");
        firstDensity ??= density;
        var x = RequiredNumber(item, "originX");
        var y = RequiredNumber(item, "originY");
        _ = RasterEffectGrid.Coordinate(x, density, "originX");
        _ = RasterEffectGrid.Coordinate(y, density, "originY");
        var mode = RequiredEnum<VisualMaskMode>(item, "mode");
        RasterEffectPlan.ValidateMaskMode(mode);
        var dither = OptionalBool(item, "orderedDither") ?? throw new ArgumentException("Missing required mask orderedDither boolean.");
        var required = checked((long)width * height * 4);
        var text = RequiredString(item, "rgbaPixels");
        if (text.Length != VisualArtifactInterchangeValidation.Base64Length(required))
            throw new ArgumentException("Captured mask base64 must have the exact declared RGBA length.");
        decodedBytes = checked(decodedBytes + required);
        // The caller's JSON is already bounded. Count every decoded array and the future immutable snapshot
        // cumulatively before this record's allocation; no compressed image or encoder buffers are involved.
        RasterEffectPreflight.ValidateCapture(null, decodedBytes, decodedBytes,
            checked(decodedBytes * 3), out _, out _);
        var pixels = Convert.FromBase64String(text);
        if (pixels.LongLength != required || !string.Equals(Convert.ToBase64String(pixels), text, StringComparison.Ordinal))
            throw new ArgumentException("Captured mask pixels must use canonical base64 with the exact RGBA byte length.");
        return new VisualArtifactInterchangeMask {
            ImageWidth = width,
            ImageHeight = height,
            OriginX = x,
            OriginY = y,
            PixelsPerUnit = density,
            Mode = mode,
            OrderedDither = dither,
            Pixels = pixels
        };
    }
}
