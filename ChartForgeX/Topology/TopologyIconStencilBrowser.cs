using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Html;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines settings for rendering a topology icon catalog as a stencil browser page.
/// </summary>
public sealed class TopologyIconStencilBrowserOptions {
    /// <summary>Gets or sets the browser DOM id.</summary>
    public string Id { get; set; } = "topology-icon-stencil-browser";

    /// <summary>Gets or sets the page title.</summary>
    public string Title { get; set; } = "Topology Stencil Browser";

    /// <summary>Gets or sets the page subtitle.</summary>
    public string? Subtitle { get; set; } = "Browse reusable icon packs, vendor folders, and stencil categories.";

    /// <summary>Gets or sets placeholder text for the client-side search box.</summary>
    public string SearchPlaceholder { get; set; } = "Search icons, packs, vendors, tags...";

    /// <summary>Gets or sets free-text search applied before the page is rendered.</summary>
    public string? SearchText { get; set; }

    /// <summary>Gets pack ids to include. Empty means all packs.</summary>
    public List<string> PackIds { get; } = new();

    /// <summary>Gets vendors to include. Empty means all vendors.</summary>
    public List<string> Vendors { get; } = new();

    /// <summary>Gets icon categories to include. Empty means all categories.</summary>
    public List<string> Categories { get; } = new();

    /// <summary>Gets search tags to include. Empty means all tags.</summary>
    public List<string> Tags { get; } = new();

    /// <summary>Gets or sets whether built-in packs should be included.</summary>
    public bool IncludeBuiltInPacks { get; set; } = true;

    /// <summary>Gets or sets whether custom or vendor packs should be included.</summary>
    public bool IncludeCustomPacks { get; set; } = true;

    /// <summary>Gets or sets the number of icon-pack groups per row.</summary>
    public int PacksPerRow { get; set; } = 3;

    /// <summary>Gets or sets the maximum icon columns inside each pack group.</summary>
    public int ColumnsPerPack { get; set; } = 4;
}

/// <summary>
/// Renders topology icon catalogs as host-facing stencil browser pages.
/// </summary>
public static class TopologyIconStencilBrowserExtensions {
    /// <summary>
    /// Builds a complete HTML page with catalog folders, filters, a palette diagram, and selection details.
    /// </summary>
    /// <param name="catalog">The source icon catalog.</param>
    /// <param name="options">Optional stencil browser settings.</param>
    /// <returns>A complete HTML document.</returns>
    public static string ToStencilBrowserHtmlPage(this TopologyIconCatalog catalog, TopologyIconStencilBrowserOptions? options = null) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        options ??= new TopologyIconStencilBrowserOptions();
        var query = ToQuery(options);
        var items = catalog.Search(query);
        var packSummaries = catalog.GetPackSummaries(query);
        var vendorSummaries = catalog.GetVendorSummaries(query);
        var categories = catalog.GetCategories(query);
        var paletteOptions = new TopologyIconPaletteOptions {
            Id = RequiredText(options.Id, nameof(options.Id)) + "-palette",
            Title = RequiredText(options.Title, nameof(options.Title)),
            Subtitle = options.Subtitle,
            SearchText = options.SearchText,
            IncludeBuiltInPacks = options.IncludeBuiltInPacks,
            IncludeCustomPacks = options.IncludeCustomPacks,
            PacksPerRow = Math.Max(1, options.PacksPerRow),
            ColumnsPerPack = Math.Max(1, options.ColumnsPerPack)
        };
        paletteOptions.PackIds.AddRange(options.PackIds);
        paletteOptions.Vendors.AddRange(options.Vendors);
        paletteOptions.Categories.AddRange(options.Categories);
        paletteOptions.Tags.AddRange(options.Tags);
        var palette = catalog.ToPaletteChart(paletteOptions);
        var renderOptions = new TopologyRenderOptions {
            IconCatalog = catalog,
            IncludeLegend = false,
            EnableHtmlInteractions = false
        };

        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line()
            .StartElement("meta").Attribute("charset", "utf-8").EndVoidElement().Line()
            .StartElement("meta").Attribute("name", "viewport").Attribute("content", "width=device-width, initial-scale=1").EndVoidElement().Line()
            .StartElement("title").Text(options.Title).EndElement().Line()
            .StartElement("style").RawTrusted(StyleSheet()).EndElement().Line()
            .EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .StartElement("main")
                .Attribute("id", options.Id)
                .Attribute("class", "cfx-icon-browser")
                .Attribute("data-cfx-icon-browser", "true")
                .Attribute("data-total-icons", items.Count)
                .Attribute("data-total-packs", packSummaries.Count)
                .EndStartElement().Line();

        WriteHeader(writer, options, items.Count, packSummaries.Count);
        writer.StartElement("div").Attribute("class", "cfx-icon-browser__shell").EndStartElement().Line();
        WriteSidebar(writer, vendorSummaries, packSummaries);
        writer.StartElement("section").Attribute("class", "cfx-icon-browser__workspace").EndStartElement().Line();
        WriteFilters(writer, categories);
        writer.StartElement("div").Attribute("class", "cfx-icon-browser__palette").EndStartElement()
            .RawTrusted(palette.ToSvg(renderOptions))
            .EndElement().Line();
        writer.EndElement().Line();
        WriteInspector(writer);
        writer.EndElement().Line()
            .EndElement().Line()
            .StartElement("script").RawTrusted(Script()).EndElement().Line()
            .EndElement().Line()
            .EndElement().Line();
        return writer.Build();
    }

    private static void WriteHeader(HtmlMarkupWriter writer, TopologyIconStencilBrowserOptions options, int iconCount, int packCount) {
        writer.StartElement("header").Attribute("class", "cfx-icon-browser__header").EndStartElement()
            .StartElement("div").EndStartElement()
            .StartElement("p").Attribute("class", "cfx-icon-browser__eyebrow").Text("Topology catalog").EndElement()
            .StartElement("h1").Text(options.Title).EndElement();
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) writer.StartElement("p").Attribute("class", "cfx-icon-browser__subtitle").Text(options.Subtitle).EndElement();
        writer.EndElement()
            .StartElement("div").Attribute("class", "cfx-icon-browser__search").EndStartElement()
            .StartElement("input")
                .Attribute("type", "search")
                .Attribute("placeholder", options.SearchPlaceholder)
                .Attribute("value", options.SearchText)
                .Attribute("aria-label", "Search topology icons")
                .Attribute("data-cfx-icon-search", "true")
                .EndVoidElement()
            .StartElement("span").Attribute("data-cfx-icon-count", "true").Text(iconCount.ToString(CultureInfo.InvariantCulture) + " icons").EndElement()
            .StartElement("small").Text(packCount.ToString(CultureInfo.InvariantCulture) + " packs").EndElement()
            .EndElement()
            .EndElement().Line();
    }

    private static void WriteSidebar(HtmlMarkupWriter writer, IReadOnlyList<TopologyIconVendorSummary> vendors, IReadOnlyList<TopologyIconPackSummary> packs) {
        writer.StartElement("aside").Attribute("class", "cfx-icon-browser__sidebar").EndStartElement();
        writer.StartElement("section").EndStartElement()
            .StartElement("h2").Text("Vendors").EndElement();
        WriteFilterButton(writer, "vendor", string.Empty, "All vendors", vendors.Sum(vendor => vendor.IconCount));
        foreach (var vendor in vendors) WriteFilterButton(writer, "vendor", vendor.Label, vendor.Label, vendor.IconCount);
        writer.EndElement();

        writer.StartElement("section").EndStartElement()
            .StartElement("h2").Text("Packs").EndElement();
        WriteFilterButton(writer, "pack", string.Empty, "All packs", packs.Sum(pack => pack.IconCount));
        foreach (var pack in packs) WriteFilterButton(writer, "pack", pack.PackId, pack.Label, pack.IconCount);
        writer.EndElement()
            .EndElement().Line();
    }

    private static void WriteFilters(HtmlMarkupWriter writer, IReadOnlyList<string> categories) {
        writer.StartElement("div").Attribute("class", "cfx-icon-browser__filters").EndStartElement();
        WriteFilterButton(writer, "category", string.Empty, "All categories", 0);
        foreach (var category in categories) WriteFilterButton(writer, "category", category, category, 0);
        writer.EndElement().Line();
    }

    private static void WriteFilterButton(HtmlMarkupWriter writer, string kind, string value, string label, int count) {
        writer.StartElement("button")
            .Attribute("type", "button")
            .Attribute("data-cfx-icon-filter", kind)
            .Attribute("data-cfx-icon-filter-value", value)
            .Attribute("aria-pressed", string.IsNullOrEmpty(value) ? "true" : "false")
            .EndStartElement()
            .StartElement("span").Text(label).EndElement();
        if (count > 0) writer.StartElement("small").Text(count.ToString(CultureInfo.InvariantCulture)).EndElement();
        writer.EndElement();
    }

    private static void WriteInspector(HtmlMarkupWriter writer) {
        writer.StartElement("aside").Attribute("class", "cfx-icon-browser__inspector").Attribute("data-cfx-icon-inspector", "true").EndStartElement()
            .StartElement("p").Attribute("class", "cfx-icon-browser__eyebrow").Text("Selected stencil").EndElement()
            .StartElement("h2").Attribute("data-cfx-icon-detail", "label").Text("Choose an icon").EndElement()
            .StartElement("dl").EndStartElement()
            .StartElement("dt").Text("Icon id").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "id").Text("-").EndElement()
            .StartElement("dt").Text("Pack").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "pack").Text("-").EndElement()
            .StartElement("dt").Text("Vendor").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "vendor").Text("-").EndElement()
            .StartElement("dt").Text("Category").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "category").Text("-").EndElement()
            .StartElement("dt").Text("Shape").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "shape").Text("-").EndElement()
            .StartElement("dt").Text("Artwork").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "artwork").Text("-").EndElement()
            .StartElement("dt").Text("Source").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "source").Text("-").EndElement()
            .StartElement("dt").Text("Revision").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "revision").Text("-").EndElement()
            .StartElement("dt").Text("License").EndElement().StartElement("dd").Attribute("data-cfx-icon-detail", "license").Text("-").EndElement()
            .EndElement()
            .StartElement("p").Attribute("class", "cfx-icon-browser__hint").Text("Clicking an icon emits cfx-icon-browser-select with the stable catalog metadata a host diagram builder needs.").EndElement()
            .EndElement().Line();
    }

    private static string StyleSheet() => TopologyHtmlAssets.IconStencilBrowserStyle;

    private static string Script() => TopologyHtmlAssets.IconStencilBrowserScript;

    private static TopologyIconCatalogQuery ToQuery(TopologyIconStencilBrowserOptions options) {
        var query = new TopologyIconCatalogQuery {
            SearchText = options.SearchText,
            IncludeBuiltInPacks = options.IncludeBuiltInPacks,
            IncludeCustomPacks = options.IncludeCustomPacks
        };
        query.PackIds.AddRange(options.PackIds);
        query.Vendors.AddRange(options.Vendors);
        query.Categories.AddRange(options.Categories);
        query.Tags.AddRange(options.Tags);
        return query;
    }

    private static string RequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameterName);
        return value!.Trim();
    }
}
