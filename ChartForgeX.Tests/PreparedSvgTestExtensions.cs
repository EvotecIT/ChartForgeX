using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

/// <summary>Reads actual leaf geometry/paint or source metadata from a prepared SVG semantic group.</summary>
internal static class PreparedSvgTestExtensions {
    internal static ChartColor RenderedColor(this XElement element, string name) =>
        ParseRenderedColor(element.RenderedAttribute(name)?.Value ?? throw new InvalidOperationException("Missing rendered colour: " + name));

    /// <summary>Reads the literal CSS emitted by SVG while retaining translucent paint's actual alpha.</summary>
    internal static ChartColor ParseRenderedColor(string value) {
        value = value.Trim();
        var alpha = value.StartsWith("rgba(", StringComparison.Ordinal);
        if (!alpha && !value.StartsWith("rgb(", StringComparison.Ordinal)) return ChartColor.Parse(value);
        if (!value.EndsWith(")", StringComparison.Ordinal)) throw new FormatException("Unclosed CSS colour.");
        var start = alpha ? 5 : 4;
        var channels = value.Substring(start, value.Length - start - 1).Split(',');
        if (channels.Length != (alpha ? 4 : 3)) throw new FormatException("Unexpected CSS colour channel count.");
        var opacity = alpha ? double.Parse(channels[3], NumberStyles.Float, CultureInfo.InvariantCulture) : 1;
        if (double.IsNaN(opacity) || opacity < 0 || opacity > 1) throw new FormatException("CSS colour alpha is outside [0, 1].");
        return ChartColor.FromRgba(byte.Parse(channels[0], NumberStyles.Integer, CultureInfo.InvariantCulture),
            byte.Parse(channels[1], NumberStyles.Integer, CultureInfo.InvariantCulture),
            byte.Parse(channels[2], NumberStyles.Integer, CultureInfo.InvariantCulture), (byte)Math.Round(opacity * 255));
    }

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
