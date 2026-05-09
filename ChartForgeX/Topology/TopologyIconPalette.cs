using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines layout settings for rendering a topology icon catalog as a reusable palette.
/// </summary>
public sealed class TopologyIconPaletteOptions {
    /// <summary>Gets or sets the chart id.</summary>
    public string Id { get; set; } = "topology-icon-palette";

    /// <summary>Gets or sets the chart title.</summary>
    public string Title { get; set; } = "Topology Icon Palette";

    /// <summary>Gets or sets the chart subtitle.</summary>
    public string? Subtitle { get; set; } = "Reusable built-in and vendor icon packs for topology diagrams.";

    /// <summary>Gets or sets the number of icon-pack groups per row.</summary>
    public int PacksPerRow { get; set; } = 2;

    /// <summary>Gets or sets the maximum icon columns inside each pack group.</summary>
    public int ColumnsPerPack { get; set; } = 4;

    /// <summary>Gets or sets the icon-pack group width.</summary>
    public double PackWidth { get; set; } = 500;

    /// <summary>Gets or sets the palette node width.</summary>
    public double NodeWidth { get; set; } = 108;

    /// <summary>Gets or sets the palette node height.</summary>
    public double NodeHeight { get; set; } = 82;

    /// <summary>Gets or sets whether empty icon packs should be rendered.</summary>
    public bool IncludeEmptyPacks { get; set; }

    /// <summary>Gets or sets free-text search matched against pack and icon metadata.</summary>
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
}

/// <summary>
/// Creates topology diagrams from icon catalogs so hosts can expose a visual picker or stencil browser.
/// </summary>
public static class TopologyIconPaletteExtensions {
    /// <summary>
    /// Builds a topology chart that displays every icon in the catalog grouped by pack.
    /// </summary>
    /// <param name="catalog">The source icon catalog.</param>
    /// <param name="options">Optional palette layout settings.</param>
    /// <returns>A topology chart that can be rendered as SVG, PNG, or interactive HTML.</returns>
    public static TopologyChart ToPaletteChart(this TopologyIconCatalog catalog, TopologyIconPaletteOptions? options = null) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        options ??= new TopologyIconPaletteOptions();
        var query = ToQuery(options);
        var iconItems = catalog.Search(query);
        var iconItemsByPack = iconItems
            .GroupBy(item => item.PackId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Icon).ToList(), StringComparer.OrdinalIgnoreCase);
        var includeEmptyPacks = options.IncludeEmptyPacks && options.Categories.Count == 0 && string.IsNullOrWhiteSpace(options.SearchText);
        var packs = catalog.Packs.Where(pack => TopologyIconCatalogQueryExtensions.PackMatches(pack, query) && (includeEmptyPacks || iconItemsByPack.ContainsKey(pack.Id))).ToList();
        var packsPerRow = Math.Max(1, options.PacksPerRow);
        var columnsPerPack = Math.Max(1, options.ColumnsPerPack);
        var packWidth = Math.Max(260, options.PackWidth);
        var nodeWidth = Math.Max(72, options.NodeWidth);
        var nodeHeight = Math.Max(58, options.NodeHeight);
        const double outerPadding = 28;
        const double topPadding = 92;
        const double packGap = 22;
        const double nodeGap = 12;
        const double groupPadding = 18;
        const double groupHeader = 58;

        var chart = TopologyChart.Create()
            .WithId(RequiredText(options.Id, nameof(options.Id)))
            .WithTitle(RequiredText(options.Title, nameof(options.Title)))
            .WithLegend(null)
            .WithTheme(TopologyTheme.Light());
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) chart.WithSubtitle(options.Subtitle!);

        var x = outerPadding;
        var y = topPadding;
        var rowHeight = 0d;
        var renderedInRow = 0;
        for (var packIndex = 0; packIndex < packs.Count; packIndex++) {
            var pack = packs[packIndex];
            if (renderedInRow == packsPerRow) {
                x = outerPadding;
                y += rowHeight + packGap;
                rowHeight = 0;
                renderedInRow = 0;
            }

            var icons = iconItemsByPack.TryGetValue(pack.Id, out var filteredIcons) ? filteredIcons : new List<TopologyIconDefinition>();
            var actualColumns = Math.Max(1, Math.Min(columnsPerPack, Math.Max(1, icons.Count)));
            var rows = Math.Max(1, (int)Math.Ceiling(icons.Count / (double)actualColumns));
            var groupHeight = groupHeader + groupPadding + rows * nodeHeight + Math.Max(0, rows - 1) * nodeGap + groupPadding;
            var groupId = "icon-pack-" + StableId(pack.Id, packIndex);
            chart.AddGroup(groupId, pack.Label, x, y, packWidth, groupHeight, TopologyHealthStatus.Healthy, subtitle: PackSubtitle(pack, icons.Count), symbol: PackSymbol(pack), color: PackColor(icons), iconId: PackIconId(icons));
            var group = chart.Groups[chart.Groups.Count - 1];
            group.Metadata["packId"] = pack.Id;
            if (!string.IsNullOrWhiteSpace(pack.Vendor)) group.Metadata["vendor"] = pack.Vendor!;
            if (!string.IsNullOrWhiteSpace(pack.Version)) group.Metadata["version"] = pack.Version!;
            group.Metadata["isBuiltIn"] = pack.IsBuiltIn.ToString(CultureInfo.InvariantCulture);
            if (pack.Tags.Count > 0) group.Metadata["pack.tags"] = string.Join(", ", pack.Tags);
            foreach (var item in pack.Metadata) group.Metadata["pack." + item.Key] = item.Value;

            for (var iconIndex = 0; iconIndex < icons.Count; iconIndex++) {
                var icon = icons[iconIndex];
                var column = iconIndex % actualColumns;
                var row = iconIndex / actualColumns;
                var nodeX = x + groupPadding + column * (nodeWidth + nodeGap);
                var nodeY = y + groupHeader + groupPadding + row * (nodeHeight + nodeGap);
                chart.AddNode(groupId + "-" + StableId(icon.Id, iconIndex), icon.Label, nodeX, nodeY, icon.NodeKind, TopologyHealthStatus.Healthy, groupId, IconSubtitle(icon), null, "Icon " + icon.QualifiedId, nodeWidth, nodeHeight, icon.Symbol, "topology-icon-palette-node", icon.Color, icon.QualifiedId);
                var node = chart.Nodes[chart.Nodes.Count - 1];
                node.DisplayMode = icon.DisplayMode ?? TopologyNodeDisplayMode.Tile;
                node.Metadata["iconId"] = icon.QualifiedId;
                node.Metadata["iconPackId"] = pack.Id;
                node.Metadata["iconLocalId"] = icon.Id;
                node.Metadata["iconShape"] = icon.Shape.ToString();
                node.Metadata["pack.label"] = pack.Label;
                node.Metadata["isBuiltIn"] = pack.IsBuiltIn.ToString(CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(pack.Vendor)) node.Metadata["vendor"] = pack.Vendor!;
                if (!string.IsNullOrWhiteSpace(pack.Version)) node.Metadata["pack.version"] = pack.Version!;
                foreach (var item in pack.Metadata) node.Metadata["pack." + item.Key] = item.Value;
                if (icon.Artwork != null) node.Metadata["icon.artwork"] = ArtworkKind(icon.Artwork);
                if (!string.IsNullOrWhiteSpace(icon.Category)) node.Metadata["category"] = icon.Category!;
                if (icon.Tags.Count > 0) node.Metadata["icon.tags"] = string.Join(", ", icon.Tags);
                foreach (var item in icon.Metadata) node.Metadata["icon." + item.Key] = item.Value;
            }

            rowHeight = Math.Max(rowHeight, groupHeight);
            x += packWidth + packGap;
            renderedInRow++;
        }

        var actualPackColumns = Math.Max(1, Math.Min(packsPerRow, Math.Max(1, packs.Count)));
        var width = outerPadding * 2 + actualPackColumns * packWidth + Math.Max(0, actualPackColumns - 1) * packGap;
        var height = Math.Max(320, y + rowHeight + outerPadding);
        chart.WithViewport(width, height, 24);
        return chart;
    }

    private static string? PackIconId(IReadOnlyList<TopologyIconDefinition> icons) => icons.Count == 0 ? null : icons[0].QualifiedId;

    private static string? PackColor(IReadOnlyList<TopologyIconDefinition> icons) => icons.FirstOrDefault(icon => !string.IsNullOrWhiteSpace(icon.Color))?.Color;

    private static string PackSymbol(TopologyIconPack pack) {
        var words = pack.Label.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return pack.Id.Length <= 3 ? pack.Id.ToUpperInvariant() : pack.Id.Substring(0, 3).ToUpperInvariant();
        var symbol = string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
        return symbol.Length == 1 && words[0].Length > 1 ? words[0].Substring(0, Math.Min(3, words[0].Length)).ToUpperInvariant() : symbol;
    }

    private static string? PackSubtitle(TopologyIconPack pack, int iconCount) {
        var source = pack.IsBuiltIn ? "Built-in" : "Vendor";
        var vendor = string.IsNullOrWhiteSpace(pack.Vendor) ? source : pack.Vendor!;
        var version = string.IsNullOrWhiteSpace(pack.Version) ? null : pack.Version!.Equals("sample", StringComparison.OrdinalIgnoreCase) ? " sample" : " v" + pack.Version;
        return vendor + version + " - " + iconCount.ToString(CultureInfo.InvariantCulture) + " icons";
    }

    private static string? IconSubtitle(TopologyIconDefinition icon) {
        return string.IsNullOrWhiteSpace(icon.Category) ? icon.QualifiedId : icon.Category + " - " + icon.QualifiedId;
    }

    private static string ArtworkKind(TopologyIconArtwork artwork) {
        if (artwork.HasSvgBody) return "svg";
        if (artwork.HasSvgPath) return "svg";
        if (artwork.HasImageHref) return "image";
        return "empty";
    }

    private static string StableId(string value, int fallbackIndex) {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value) {
            builder.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
        }

        var id = builder.ToString().Trim('-');
        return id.Length == 0 ? "item-" + fallbackIndex.ToString(CultureInfo.InvariantCulture) : id;
    }

    private static string RequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameterName);
        return value!.Trim();
    }

    private static TopologyIconCatalogQuery ToQuery(TopologyIconPaletteOptions options) {
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
}
