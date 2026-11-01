using System;
using System.Linq;
using System.Security.Cryptography;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>Retains complete source paint facts, including fills on currently hidden observations.</summary>
    private static void VisibilitySourceFills(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource.Series source) {
        if (source.Fill is { } fill) {
            writer.Property("fill"); VisibilitySourceFill(writer, fill);
        }
        if (source.PointFills.Count > 0) {
            writer.Property("pointFills"); writer.StartArray();
            foreach (var pair in source.PointFills.OrderBy(pair => pair.Key)) {
                writer.StartObject(); Number(writer, "index", pair.Key);
                writer.Property("fill"); VisibilitySourceFill(writer, pair.Value); writer.EndObject();
            }
            writer.EndArray();
        }
        if (!source.ItemStyles.Any(style => style?.Fill != null)) return;
        writer.Property("itemFills"); writer.StartArray();
        foreach (var style in source.ItemStyles) {
            if (style?.Fill is { } itemFill) VisibilitySourceFill(writer, itemFill);
            else writer.String(null);
        }
        writer.EndArray();
    }

    private static void VisibilitySourceFill(VisualArtifactInterchangeJsonWriter writer, ChartFill fill) {
        writer.StartObject(); String(writer, "kind", fill.Kind.ToString());
        if (fill.ImageData is { } data) {
            var image = data.Image;
            Number(writer, "width", image.Width); Number(writer, "height", image.Height);
            // Canonical identity needs the exact private pixels, without expanding them into a second image payload.
            using var sha = SHA256.Create();
            String(writer, "rgbaSha256", BitConverter.ToString(sha.ComputeHash(image.Pixels)).Replace("-", string.Empty).ToLowerInvariant());
            if (fill.Kind == ChartFillKind.Pattern) { Number(writer, "tileWidth", fill.TileWidth); Number(writer, "tileHeight", fill.TileHeight); }
        } else {
            Number(writer, "startX", fill.Start.X); Number(writer, "startY", fill.Start.Y);
            if (fill.Kind == ChartFillKind.RadialGradient) Number(writer, "radius", fill.Radius);
            else { Number(writer, "endX", fill.End.X); Number(writer, "endY", fill.End.Y); }
            writer.Property("stops"); writer.StartArray();
            foreach (var stop in fill.Stops) {
                writer.StartObject(); Number(writer, "offset", stop.Offset); String(writer, "color", WaterfallColor(stop.Color)!);
                OptionalString(writer, "cssVariable", stop.Color.CssVariable); writer.EndObject();
            }
            writer.EndArray();
        }
        writer.EndObject();
    }
}
