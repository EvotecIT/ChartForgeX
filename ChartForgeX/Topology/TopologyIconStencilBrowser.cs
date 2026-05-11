using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

    private static string StyleSheet() {
        return """
body{margin:0;background:#f6f8fb;color:#0f172a;font-family:Inter,Segoe UI,system-ui,sans-serif}.cfx-icon-browser{min-height:100vh;padding:24px;box-sizing:border-box}.cfx-icon-browser__header{display:flex;gap:24px;align-items:flex-start;justify-content:space-between;max-width:1500px;margin:0 auto 18px}.cfx-icon-browser__eyebrow{margin:0 0 6px;color:#2563eb;font-size:11px;font-weight:800;text-transform:uppercase;letter-spacing:.04em}.cfx-icon-browser h1{margin:0;font-size:28px;letter-spacing:0}.cfx-icon-browser__subtitle{margin:6px 0 0;max-width:760px;color:#475569;font-size:14px}.cfx-icon-browser__search{display:grid;grid-template-columns:minmax(260px,360px) auto;gap:8px 12px;align-items:center}.cfx-icon-browser__search input{grid-column:1 / -1;height:40px;border:1px solid #cbd5e1;border-radius:8px;background:white;padding:0 12px;font:500 14px/1 Inter,Segoe UI,system-ui,sans-serif;color:#0f172a;box-shadow:0 8px 20px rgba(15,23,42,.05)}.cfx-icon-browser__search span{font-size:13px;font-weight:800;color:#0f172a}.cfx-icon-browser__search small{justify-self:end;color:#64748b}.cfx-icon-browser__shell{max-width:1500px;margin:0 auto;display:grid;grid-template-columns:240px minmax(0,1fr) 280px;gap:16px;align-items:start}.cfx-icon-browser__sidebar,.cfx-icon-browser__inspector{background:#fff;border:1px solid #dbe3ef;border-radius:10px;box-shadow:0 12px 28px rgba(15,23,42,.06);padding:14px;position:sticky;top:16px}.cfx-icon-browser__sidebar section+section{margin-top:18px}.cfx-icon-browser h2{margin:0 0 10px;font-size:13px}.cfx-icon-browser button{width:100%;height:34px;border:1px solid transparent;border-radius:7px;background:transparent;color:#334155;font:700 12px/1 Inter,Segoe UI,system-ui,sans-serif;display:flex;align-items:center;justify-content:space-between;gap:8px;padding:0 10px;cursor:pointer;text-align:left}.cfx-icon-browser button:hover,.cfx-icon-browser button[aria-pressed='true']{background:#eff6ff;border-color:#bfdbfe;color:#1d4ed8}.cfx-icon-browser button small{color:#64748b}.cfx-icon-browser__workspace{min-width:0}.cfx-icon-browser__filters{display:flex;gap:8px;align-items:center;overflow:auto;padding:0 0 10px}.cfx-icon-browser__filters button{width:auto;white-space:nowrap;background:#fff;border-color:#dbe3ef}.cfx-icon-browser__palette{background:#fff;border:1px solid #dbe3ef;border-radius:10px;padding:12px;box-shadow:0 12px 28px rgba(15,23,42,.06);overflow:auto}.cfx-icon-browser__palette svg{min-width:980px}.cfx-icon-browser__palette [data-cfx-role='topology-node']{cursor:pointer}.cfx-icon-browser__palette .cfx-icon-browser-hidden{display:none}.cfx-icon-browser__palette .cfx-icon-browser-selected{filter:drop-shadow(0 12px 18px rgba(37,99,235,.25))}.cfx-icon-browser__inspector h2{font-size:18px;margin:0 0 14px}.cfx-icon-browser__inspector dl{display:grid;grid-template-columns:82px minmax(0,1fr);gap:8px 10px;margin:0}.cfx-icon-browser__inspector dt{color:#64748b;font-size:12px}.cfx-icon-browser__inspector dd{margin:0;color:#0f172a;font-size:12px;font-weight:700;overflow-wrap:anywhere}.cfx-icon-browser__hint{margin:16px 0 0;color:#64748b;font-size:12px;line-height:1.45}@media(max-width:1100px){.cfx-icon-browser__header{display:block}.cfx-icon-browser__search{margin-top:14px}.cfx-icon-browser__shell{grid-template-columns:1fr}.cfx-icon-browser__sidebar,.cfx-icon-browser__inspector{position:static}.cfx-icon-browser__sidebar{display:grid;grid-template-columns:1fr 1fr;gap:14px}.cfx-icon-browser__sidebar section+section{margin-top:0}}@media(max-width:640px){.cfx-icon-browser{padding:16px}.cfx-icon-browser__sidebar{grid-template-columns:1fr}.cfx-icon-browser__search{grid-template-columns:1fr}.cfx-icon-browser h1{font-size:24px}}
""";
    }

    private static string Script() {
        return """
(() => {
  const root = document.querySelector('[data-cfx-icon-browser="true"]');
  if (!root) return;
  const search = root.querySelector('[data-cfx-icon-search]');
  const count = root.querySelector('[data-cfx-icon-count]');
  const nodes = Array.from(root.querySelectorAll('[data-cfx-role="topology-node"]'));
  const badges = Array.from(root.querySelectorAll('[data-cfx-role="topology-node-status"]'));
  const groups = Array.from(root.querySelectorAll('[data-cfx-role="topology-group"]'));
  const state = { vendor: '', pack: '', category: '', search: search ? search.value.toLowerCase().trim() : '' };
  const attr = (element, name) => element.getAttribute(name) || '';
  const meta = (element, key) => attr(element, 'data-cfx-meta-' + key);
  const detail = name => root.querySelector('[data-cfx-icon-detail="' + name + '"]');
  const setText = (name, value) => { const target = detail(name); if (target) target.textContent = value || '-'; };
  const textFor = element => [
    attr(element, 'data-node-icon-id'),
    attr(element, 'data-node-icon-label'),
    attr(element, 'data-node-icon-pack'),
    attr(element, 'data-node-icon-shape'),
    attr(element, 'data-node-kind'),
    meta(element, 'category'),
    meta(element, 'vendor'),
    meta(element, 'pack-label'),
    meta(element, 'icon-tags'),
    meta(element, 'icon-source-path'),
    meta(element, 'pack-source-url'),
    meta(element, 'pack-source-license')
  ].join(' ').toLowerCase();
  const nodeVisible = node => {
    if (state.vendor && meta(node, 'vendor') !== state.vendor) return false;
    if (state.pack && attr(node, 'data-node-icon-pack') !== state.pack) return false;
    if (state.category && meta(node, 'category') !== state.category) return false;
    return !state.search || textFor(node).includes(state.search);
  };
  const syncButtons = kind => {
    root.querySelectorAll('[data-cfx-icon-filter="' + kind + '"]').forEach(button => {
      button.setAttribute('aria-pressed', attr(button, 'data-cfx-icon-filter-value') === state[kind] ? 'true' : 'false');
    });
  };
  const apply = () => {
    let visible = 0;
    let firstVisible = null;
    const visibleNodeIds = new Set();
    const visibleByPack = new Set();
    for (const node of nodes) {
      const show = nodeVisible(node);
      node.classList.toggle('cfx-icon-browser-hidden', !show);
      if (show) {
        visible++;
        if (!firstVisible) firstVisible = node;
        visibleNodeIds.add(attr(node, 'data-node-id'));
        visibleByPack.add(attr(node, 'data-node-icon-pack'));
      }
    }
    for (const badge of badges) badge.classList.toggle('cfx-icon-browser-hidden', !visibleNodeIds.has(attr(badge, 'data-node-id')));
    for (const group of groups) {
      const pack = attr(group, 'data-group-icon-id').split(':')[0] || meta(group, 'packid');
      group.classList.toggle('cfx-icon-browser-hidden', pack && !visibleByPack.has(pack));
    }
    if (count) count.textContent = visible + ' icons';
    if (firstVisible && (state.search || state.vendor || state.pack || state.category)) {
      window.requestAnimationFrame(() => firstVisible.scrollIntoView({ block: 'nearest', inline: 'center' }));
    }
  };
  const select = node => {
    nodes.forEach(item => item.classList.toggle('cfx-icon-browser-selected', item === node));
    const payload = {
      iconId: attr(node, 'data-node-icon-id'),
      iconPack: attr(node, 'data-node-icon-pack'),
      iconLabel: attr(node, 'data-node-icon-label'),
      iconShape: attr(node, 'data-node-icon-shape'),
      iconArtwork: attr(node, 'data-node-icon-artwork') || meta(node, 'icon-artwork'),
      nodeKind: attr(node, 'data-node-kind'),
      category: meta(node, 'category'),
      vendor: meta(node, 'vendor'),
      packLabel: meta(node, 'pack-label'),
      tags: meta(node, 'icon-tags'),
      sourcePath: meta(node, 'icon-source-path'),
      sourceUrl: meta(node, 'pack-source-url'),
      sourceRevision: meta(node, 'icon-source-revision') || meta(node, 'pack-source-revision'),
      sourceLicense: meta(node, 'pack-source-license'),
      sourceLicenseUrl: meta(node, 'pack-source-licenseurl')
    };
    setText('label', payload.iconLabel);
    setText('id', payload.iconId);
    setText('pack', payload.packLabel || payload.iconPack);
    setText('vendor', payload.vendor);
    setText('category', payload.category);
    setText('shape', payload.iconShape);
    setText('artwork', payload.iconArtwork || 'fallback shape');
    setText('source', payload.sourcePath || payload.sourceUrl);
    setText('revision', payload.sourceRevision);
    setText('license', payload.sourceLicense);
    root.dispatchEvent(new CustomEvent('cfx-icon-browser-select', { bubbles: true, detail: payload }));
  };
  root.querySelectorAll('[data-cfx-icon-filter]').forEach(button => {
    button.addEventListener('click', () => {
      const kind = attr(button, 'data-cfx-icon-filter');
      state[kind] = attr(button, 'data-cfx-icon-filter-value');
      syncButtons(kind);
      apply();
    });
  });
  if (search) search.addEventListener('input', () => {
    state.search = search.value.toLowerCase().trim();
    apply();
  });
  nodes.forEach(node => {
    node.setAttribute('tabindex', '0');
    node.addEventListener('click', () => select(node));
    node.addEventListener('keydown', event => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        select(node);
      }
    });
  });
  apply();
})();
""";
    }

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
