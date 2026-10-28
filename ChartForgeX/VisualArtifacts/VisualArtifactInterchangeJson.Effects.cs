using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void DropShadow(VisualArtifactInterchangeJsonWriter writer, string name, VisualDropShadow? shadow) {
        if (shadow == null) return;
        writer.Property(name);
        writer.StartObject();
        String(writer, "color", shadow.Color.ToCss());
        Number(writer, "offsetX", shadow.OffsetX);
        Number(writer, "offsetY", shadow.OffsetY);
        Number(writer, "sigmaX", shadow.SigmaX);
        Number(writer, "sigmaY", shadow.SigmaY);
        Number(writer, "spread", shadow.Spread);
        Boolean(writer, "orderedDither", shadow.OrderedDither);
        if (shadow.Opacity != 1) Number(writer, "opacity", shadow.Opacity);
        if (shadow.RelativeRegion is ChartRect region) {
            writer.Property("relativeRegion");
            writer.StartObject();
            Number(writer, "x", region.X);
            Number(writer, "y", region.Y);
            Number(writer, "width", region.Width);
            Number(writer, "height", region.Height);
            writer.EndObject();
        }
        writer.EndObject();
    }

    private static VisualDropShadow? ReadDropShadow(Dictionary<string, GeoJsonValue> owner, string name) {
        var item = OptionalObject(owner, name);
        if (item == null) return null;
        RequireEffectMembers(item, new[] {
            "color", "offsetX", "offsetY", "sigmaX", "sigmaY", "spread",
            "orderedDither", "opacity", "relativeRegion"
        });
        var raw = RequiredString(item, "color");
        if (!ChartColor.TryParse(raw, out var color)) {
            throw new ArgumentException("A shadow color must be a supported native literal.");
        }
        ChartRect? region = null;
        if (OptionalObject(item, "relativeRegion") is { } box) {
            RequireEffectMembers(box, new[] { "x", "y", "width", "height" });
            region = new ChartRect(RequiredNumber(box, "x"), RequiredNumber(box, "y"),
                RequiredNumber(box, "width"), RequiredNumber(box, "height"));
        }
        return new VisualDropShadow(color, EffectInteger(item, "offsetX"), EffectInteger(item, "offsetY"),
            RequiredNumber(item, "sigmaX"), RequiredNumber(item, "sigmaY"), EffectInteger(item, "spread"),
            OptionalBool(item, "orderedDither") ?? true, OptionalNumber(item, "opacity") ?? 1, region);
    }

    private static int EffectInteger(Dictionary<string, GeoJsonValue> item, string name) {
        var value = RequiredNumber(item, name);
        if (double.IsNaN(value) || double.IsInfinity(value) || value < int.MinValue ||
            value > int.MaxValue || value != Math.Truncate(value)) {
            throw new ArgumentException("A shadow offset or spread must be an integer.");
        }
        return (int)value;
    }

    private static void RequireEffectMembers(Dictionary<string, GeoJsonValue> values, string[] names, string role = "native shadow") {
        foreach (var name in values.Keys) {
            if (Array.IndexOf(names, name) < 0) {
                throw new ArgumentException("Unknown " + role + " member: " + name);
            }
        }
    }
}
