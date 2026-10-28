/// <summary>A complete Core-only SVG-source consumer uses one explicit finite flood and alpha-composition operation.</summary>
public static class NativeCompositeSourceExample {
    /// <summary>Creates a script-free source document; its finite operator is explicit and independent of native blend modes.</summary>
    public static string Source(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, string operation = "in") {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (operation is not ("over" or "in" or "out" or "atop" or "xor")) throw new ArgumentOutOfRangeException(nameof(operation));
        var width = variant == "compact" ? 390 : 760;
        var ink = VisualTheme.Graphite().Resolve(mode).Foreground.ToCss();
        return "<svg xmlns='http://www.w3.org/2000/svg' width='" + width.ToString(CultureInfo.InvariantCulture) + "' height='280'>"
            + "<title>Explicit source " + operation + " flood</title><defs>"
            + "<filter id='composite-" + operation + "' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='" + width.ToString(CultureInfo.InvariantCulture)
            + "' height='280' color-interpolation-filters='sRGB'><feFlood flood-color='#0000ff' flood-opacity='0.5019607843137255' result='backdrop'/>"
            + "<feComposite in='SourceGraphic' in2='backdrop' operator='" + operation + "'/></filter></defs>"
            + "<rect id='source-" + operation + "' x='32' y='72' width='" + (width - 64).ToString(CultureInfo.InvariantCulture)
            + "' height='144' fill='#ff0000' fill-opacity='0.5019607843137255' filter='url(#composite-" + operation + ")'/>"
            + "<text x='24' y='38' font-family='Arial,sans-serif' font-size='20' fill='" + ink + "'>Source " + operation + " backdrop</text></svg>";
    }

    /// <summary>Uses the incumbent Core rasterizer with explicit per-operation filter admission.</summary>
    public static ChartForgeX.SvgRaster.SvgRasterizationResult Rasterize(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light,
        string operation = "in") => ChartForgeX.SvgRaster.SvgRasterizer.Rasterize(Source(variant, mode, operation),
            strict: true, filters: new ChartForgeX.SvgRaster.SvgRasterFilterOptions());

    /// <summary>Captures one completed half-opacity vector group before the selected finite filter.</summary>
    public static string SourceWithOpacity(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light,
        string operation = "in") {
        var document = System.Xml.Linq.XDocument.Parse(Source(variant, mode, operation));
        var root = document.Root ?? throw new InvalidOperationException("The generated source has no SVG root.");
        var ns = root.Name.Namespace;
        var shape = System.Linq.Enumerable.Single(root.Elements(ns + "rect"));
        var filterAttribute = shape.Attribute("filter")
            ?? throw new InvalidOperationException("The generated source has no finite filter.");
        var filterDefinition = System.Linq.Enumerable.Single(root.Element(ns + "defs")?.Elements(ns + "filter")
            ?? throw new InvalidOperationException("The generated source has no filter definition."));
        var filterId = filterDefinition.Attribute("id")?.Value
            ?? throw new InvalidOperationException("The generated filter has no identity.");
        filterDefinition.SetAttributeValue("id", filterId + "-opacity");
        shape.SetAttributeValue("id", "source-" + operation + "-opacity");
        var filter = "url(#" + filterId + "-opacity)";
        filterAttribute.Remove();
        shape.ReplaceWith(new System.Xml.Linq.XElement(ns + "g", new System.Xml.Linq.XAttribute("filter", filter),
            new System.Xml.Linq.XElement(ns + "g", new System.Xml.Linq.XAttribute("opacity", ".5"),
                new System.Xml.Linq.XElement(shape))));
        return document.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
    }

    /// <summary>Uses the same rasterizer and explicit limits for the completed-opacity source.</summary>
    public static ChartForgeX.SvgRaster.SvgRasterizationResult RasterizeWithOpacity(string variant = "wide",
        VisualThemeMode mode = VisualThemeMode.Light, string operation = "in") =>
        ChartForgeX.SvgRaster.SvgRasterizer.Rasterize(SourceWithOpacity(variant, mode, operation),
            strict: true, filters: new ChartForgeX.SvgRaster.SvgRasterFilterOptions());

}
