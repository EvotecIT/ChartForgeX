using System.Xml.Linq;

namespace ChartForgeX.Tests;

/// <summary>Numeric paint observations for flat native topology controls whose authored metadata intentionally differs.</summary>
internal static class TopologyStaticPaintObservation {
    internal static string[] Paint(string svg)=>XDocument.Parse(svg).Descendants()
        .Where(element=>!element.Ancestors().Any(parent=>parent.Name.LocalName=="defs")
            && (element.Name.LocalName is "path" or "rect" or "line" or "circle" or "ellipse" or "polygon" or "polyline" or "text" or "image"
                || element.Name.LocalName=="g"&&(element.Attribute("transform")!=null||element.Attribute("opacity")!=null)))
        .Select(element=>element.Name.LocalName+"|"+string.Join("|",element.Attributes()
            .Where(attribute=>attribute.Name.LocalName is not ("id" or "class" or "role")&&!attribute.Name.LocalName.StartsWith("data-",StringComparison.Ordinal)
                &&!attribute.Name.LocalName.StartsWith("aria-",StringComparison.Ordinal))
            .Select(attribute=>attribute.Name.LocalName+"="+attribute.Value))+"|"+(element.Name.LocalName=="text"?element.Value:""))
        .ToArray();
}
