using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void InnerShadow(VisualArtifactInterchangeJsonWriter writer, VisualInnerShadow? shadow) {
        if (shadow == null) return;
        writer.Property("innerShadow");
        writer.StartObject();
        String(writer, "color", shadow.Color.ToCss());
        Number(writer, "offsetX", shadow.OffsetX);
        Number(writer, "offsetY", shadow.OffsetY);
        Number(writer, "sigmaX", shadow.SigmaX);
        Number(writer, "sigmaY", shadow.SigmaY);
        Number(writer, "spread", shadow.Spread);
        Boolean(writer, "orderedDither", shadow.OrderedDither);
        if (shadow.Opacity != 1) Number(writer, "opacity", shadow.Opacity);
        writer.EndObject();
    }

    private static VisualInnerShadow? ReadInnerShadow(Dictionary<string, GeoJsonValue> owner) {
        var item = OptionalObject(owner, "innerShadow");
        if (item == null) return null;
        RequireEffectMembers(item, new[] {
            "color", "offsetX", "offsetY", "sigmaX", "sigmaY", "spread", "orderedDither", "opacity"
        }, "inner shadow");
        if (!ChartColor.TryParse(RequiredString(item, "color"), out var color))
            throw new ArgumentException("An inner shadow color must be a supported native literal.");
        return new VisualInnerShadow(color, EffectInteger(item, "offsetX"), EffectInteger(item, "offsetY"),
            RequiredNumber(item, "sigmaX"), RequiredNumber(item, "sigmaY"), EffectInteger(item, "spread"),
            RequiredEffectDither(item), OptionalNumber(item, "opacity") ?? 1);
    }
}
