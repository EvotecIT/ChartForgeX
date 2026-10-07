using System.Xml.Linq;

namespace ChartForgeX.Tests;

/// <summary>Reads actual leaf geometry/paint or source metadata from a prepared SVG semantic group.</summary>
internal static class PreparedSvgTestExtensions {
    internal static XAttribute? RenderedAttribute(this XElement element, string name) {
        var value = element.Attribute(name);
        if (name.StartsWith("data-", StringComparison.Ordinal))
            value ??= element.Ancestors().Select(owner => owner.Attribute(name)).FirstOrDefault(attribute => attribute != null);
        value ??= element.Descendants().Select(child => child.Attribute(name)).FirstOrDefault(attribute => attribute != null);
        return value ?? element.Ancestors().Select(owner => owner.Attribute(name)).FirstOrDefault(attribute => attribute != null);
    }

    internal static string Tooltip(this XElement element) => element.Descendants()
        .First(child => child.Name.LocalName == "title").Value;
}
