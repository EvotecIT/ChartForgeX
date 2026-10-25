using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void SurfacePaint(VisualArtifactInterchangeJsonWriter writer,double? width,double? opacity,double? surface,IReadOnlyList<double> dash) {
        OptionalNumber(writer,"strokeWidth",width);OptionalNumber(writer,"opacity",opacity);OptionalNumber(writer,"surfaceOpacity",surface);
        if(dash.Count==0)return;
        writer.Property("dashPattern");writer.StartArray();foreach(var value in dash)writer.Number(value);writer.EndArray();
    }
    private static void ReadSurfaceDash(Dictionary<string,GeoJsonValue> item,List<double> target) {
        foreach(var value in OptionalArray(item,"dashPattern"))target.Add(value.AsNumber("surface dash value"));
    }
}
