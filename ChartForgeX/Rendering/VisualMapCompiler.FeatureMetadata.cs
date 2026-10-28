using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    // Feature metadata is inert source data. SVG/HTML writers escape attribute values; it never becomes markup or identity.
    private static void AddFeatureFacts(Dictionary<string, string> metadata, ChartMapFeature? feature) {
        if (feature == null) return;
        if (feature.Id != null && CanWriteFeatureId(feature.Id)) metadata["data-cfx-feature-id"] = feature.Id;
        if (feature.IdJson != null) metadata["data-cfx-feature-id-json"] = feature.IdJson;
        if (feature.PropertiesJson != null) metadata["data-cfx-feature-properties"] = feature.PropertiesJson;
    }

    private static bool CanWriteFeatureId(string id) {
        for (var index = 0; index < id.Length; index++) {
            var scalar = (int)id[index];
            if (char.IsHighSurrogate(id[index])) { var high = id[index]; scalar = char.ConvertToUtf32(high, id[++index]); }
            if (!SvgMarkupWriter.IsMarkupScalar(scalar)) return false;
        }
        return true;
    }
}
