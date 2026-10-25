using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
namespace ChartForgeX.VisualArtifacts;
internal static partial class VisualArtifactInterchangeJson {
    private static void SequenceLifelineStroke(VisualArtifactInterchangeJsonWriter writer,ChartStrokeStyle? style) {
        if(style==null)return;writer.Property("lifelineStroke");writer.StartObject();OptionalString(writer,"color",style.Color?.ToCss());OptionalNumber(writer,"width",style.Width);if(style.Opacity!=1)Number(writer,"opacity",style.Opacity);
        if(style.Dash!=null){writer.Property("dash");writer.StartArray();foreach(var length in style.Dash)writer.Number(length);writer.EndArray();}writer.EndObject();
    }
    private static ChartStrokeStyle? ReadSequenceLifelineStroke(Dictionary<string,GeoJsonValue> sequence) {
        var item=OptionalObject(sequence,"lifelineStroke");if(item==null)return null;RequireSequenceMembers(item,new[]{"color","width","opacity","dash"});ChartColor? color=null;
        if(OptionalString(item,"color") is {} value)color=SvgRasterColor.TryParse(value,out var parsed)?parsed:throw new ArgumentException("Lifeline stroke requires a literal native color.");
        var dash=item.ContainsKey("dash")?OptionalArray(item,"dash").Select(value=>value.AsNumber("lifeline dash")).ToArray():null;
        return new ChartStrokeStyle(color,OptionalNumber(item,"width"),OptionalNumber(item,"opacity")??1,dash);
    }
}
