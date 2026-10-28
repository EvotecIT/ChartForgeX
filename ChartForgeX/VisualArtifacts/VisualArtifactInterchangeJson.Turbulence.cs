using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Effects;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void TurbulenceSource(VisualArtifactInterchangeJsonWriter writer, VisualTurbulence? source) {
        if (source == null) return;
        writer.Property("turbulenceSource"); writer.StartObject();
        Number(writer, "baseFrequencyX", source.BaseFrequencyX); Number(writer, "baseFrequencyY", source.BaseFrequencyY);
        Number(writer, "seed", source.Seed); Number(writer, "numberOfOctaves", source.NumberOfOctaves);
        String(writer, "type", source.Type.ToString()); String(writer, "colorSpace", source.ColorSpace.ToString());
        Boolean(writer, "stitchTiles", source.StitchTiles);
        writer.EndObject();
    }
    private static VisualTurbulence? ReadTurbulenceSource(Dictionary<string, GeoJsonValue> owner) {
        var item = OptionalObject(owner, "turbulenceSource");
        if (item == null) return null;
        RequireEffectMembers(item, new[] { "baseFrequencyX", "baseFrequencyY", "seed", "numberOfOctaves", "type", "colorSpace", "stitchTiles" }, "turbulence source");
        return new VisualTurbulence(RequiredNumber(item, "baseFrequencyX"), RequiredNumber(item, "baseFrequencyY"),
            RequiredInt(item, "numberOfOctaves"), RequiredNumber(item, "seed"), RequiredEnum<VisualTurbulenceType>(item, "type"),
            OptionalBool(item, "stitchTiles") ?? throw new ArgumentException("Missing turbulence source stitchTiles boolean."),
            RequiredEnum<VisualFilterColorSpace>(item, "colorSpace"));
    }
}
