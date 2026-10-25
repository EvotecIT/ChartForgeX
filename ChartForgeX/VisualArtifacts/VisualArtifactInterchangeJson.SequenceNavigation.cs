using System.Collections.Generic;
using ChartForgeX.Core;
namespace ChartForgeX.VisualArtifacts;
internal static partial class VisualArtifactInterchangeJson {
    private static void SequenceNavigation(VisualArtifactInterchangeJsonWriter writer,IReadOnlyList<VisualNavigationLink> links) {
        if(links.Count==0)return;
        writer.Property("navigationLinks");writer.StartArray();foreach(var link in links){writer.StartObject();String(writer,"label",link.Label);String(writer,"href",link.Href);writer.EndObject();}writer.EndArray();
    }
    private static void ReadSequenceNavigation(Dictionary<string,GeoJsonValue> item,List<VisualNavigationLink> links) {
        foreach(var value in OptionalArray(item,"navigationLinks")){var record=value.AsObject("sequence navigation link");RequireSequenceMembers(record,new[]{"label","href"});links.Add(new VisualNavigationLink(RequiredString(record,"label"),RequiredString(record,"href")));}
    }
}
