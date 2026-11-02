using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

/// <summary>Shares complete embedded SVG image emissions within one document.</summary>
internal sealed class VisualSvgImageResources {
    private readonly string _prefix;
    private readonly Dictionary<(string Href, double X, double Y, double Width, double Height, string Fit, double Opacity), string> _references = new();

    internal VisualSvgImageResources(string prefix) { _prefix = prefix; }

    internal string? Reference(SvgMarkupWriter writer, VisualSceneImageResource image) {
        var href = image.Href;
        if (!href.StartsWith("data:image/svg+xml;base64,", StringComparison.Ordinal)) return null;
        var bounds = image.Bounds;
        // Share paint and geometry only. Authored identity and semantics belong to each use.
        var key = (href, bounds.X, bounds.Y, bounds.Width, bounds.Height, image.PreserveAspectRatio, image.Opacity);
        if (_references.TryGetValue(key, out var reference)) return reference;
        var id = _prefix + "-image-resource-" + _references.Count.ToString(CultureInfo.InvariantCulture);
        writer.StartElement("defs").EndStartElement();
        StartInlineImage(writer, image).Attribute("id", id).EndEmptyElement();
        writer.EndElement();
        reference = "#" + id;
        _references.Add(key, reference);
        return reference;
    }

    internal SvgMarkupWriter StartImage(SvgMarkupWriter writer, VisualSceneImageResource image) {
        var reference = Reference(writer, image);
        return reference == null ? StartInlineImage(writer, image) : writer.StartElement("use").Attribute("href", reference);
    }

    private static SvgMarkupWriter StartInlineImage(SvgMarkupWriter writer, VisualSceneImageResource image) {
        var bounds = image.Bounds;
        writer.StartElement("image").Attribute("x", bounds.X).Attribute("y", bounds.Y)
            .Attribute("width", bounds.Width).Attribute("height", bounds.Height)
            .Attribute("href", image.Href).Attribute("preserveAspectRatio", image.PreserveAspectRatio);
        if (image.Opacity != 1) writer.Attribute("opacity", image.Opacity);
        return writer;
    }
}
