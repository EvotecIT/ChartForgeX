using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void TopologyCaptions(VisualArtifactInterchangeJsonWriter writer, TopologyCaptionPresentation? caption) {
        if (caption == null) return;
        writer.Property("topologyCaptions"); writer.StartObject();
        Boolean(writer, "includeNodeLabels", caption.IncludeNodeLabels); Boolean(writer, "includeIconLabels", caption.IncludeIconLabels);
        Boolean(writer, "includeTileSubtitles", caption.IncludeTileSubtitles); Boolean(writer, "wrapNodeLabels", caption.WrapNodeLabels);
        Boolean(writer, "allowMultilineNodeLabels", caption.AllowMultilineNodeLabels); Boolean(writer, "readableDenseLayout", caption.ReadableDenseLayout);
        Number(writer, "maxNodeLabelLines", caption.MaxNodeLabelLines); Number(writer, "maxNodeSubtitleLines", caption.MaxNodeSubtitleLines);
        TopologyRoutes(writer, caption.Routes);
        writer.EndObject();
    }
    private static TopologyCaptionPresentation? ReadTopologyCaptions(Dictionary<string, GeoJsonValue> presentation) {
        var item = OptionalObject(presentation, "topologyCaptions"); if (item == null) return null;
        bool Flag(string name) => OptionalBool(item, name) ?? throw new ArgumentException("Captured caption policy requires boolean " + name + ".");
        return new TopologyCaptionPresentation(Flag("includeNodeLabels"), Flag("includeIconLabels"), Flag("includeTileSubtitles"), Flag("wrapNodeLabels"),
            Flag("allowMultilineNodeLabels"), Flag("readableDenseLayout"), RequiredInt(item, "maxNodeLabelLines"), RequiredInt(item, "maxNodeSubtitleLines"), ReadTopologyRoutes(item));
    }
}
