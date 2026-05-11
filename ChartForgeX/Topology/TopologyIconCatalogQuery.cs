using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines host-side filters for browsing topology icon catalogs.
/// </summary>
public sealed class TopologyIconCatalogQuery {
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
/// Represents a catalog icon plus the pack metadata needed by pickers and vendor adapters.
/// </summary>
public sealed class TopologyIconCatalogItem {
    internal TopologyIconCatalogItem(TopologyIconPack pack, TopologyIconDefinition icon) {
        Pack = pack;
        Icon = icon;
    }

    /// <summary>Gets the source icon pack.</summary>
    public TopologyIconPack Pack { get; }

    /// <summary>Gets the icon definition.</summary>
    public TopologyIconDefinition Icon { get; }

    /// <summary>Gets the source pack id.</summary>
    public string PackId => Pack.Id;

    /// <summary>Gets the source pack label.</summary>
    public string PackLabel => Pack.Label;

    /// <summary>Gets the optional source pack vendor.</summary>
    public string? Vendor => Pack.Vendor;

    /// <summary>Gets whether the icon comes from a built-in pack.</summary>
    public bool IsBuiltIn => Pack.IsBuiltIn;

    /// <summary>Gets the local icon id.</summary>
    public string Id => Icon.Id;

    /// <summary>Gets the fully qualified icon id.</summary>
    public string QualifiedId => Icon.QualifiedId;

    /// <summary>Gets the display label.</summary>
    public string Label => Icon.Label;

    /// <summary>Gets the optional icon category.</summary>
    public string? Category => Icon.Category;

    /// <summary>Gets pack-level search tags.</summary>
    public IReadOnlyList<string> PackTags => Pack.Tags;

    /// <summary>Gets icon-level search tags.</summary>
    public IReadOnlyList<string> IconTags => Icon.Tags;
}

/// <summary>
/// Provides catalog browsing helpers for topology icon pickers.
/// </summary>
public static class TopologyIconCatalogQueryExtensions {
    /// <summary>
    /// Returns catalog icons that match the supplied query.
    /// </summary>
    public static IReadOnlyList<TopologyIconCatalogItem> Search(this TopologyIconCatalog catalog, TopologyIconCatalogQuery? query = null) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        query ??= new TopologyIconCatalogQuery();
        var tokens = SearchTokens(query.SearchText);
        var items = new List<TopologyIconCatalogItem>();
        foreach (var pack in catalog.Packs) {
            if (!PackMatches(pack, query)) continue;
            foreach (var icon in pack.Icons) {
                if (!MatchesAny(query.Categories, icon.Category)) continue;
                if (!MatchesAllTags(query.Tags, pack, icon)) continue;
                if (tokens.Count > 0 && !MatchesSearch(pack, icon, tokens)) continue;
                items.Add(new TopologyIconCatalogItem(pack, icon));
            }
        }

        return items;
    }

    /// <summary>
    /// Returns matching pack ids in catalog order.
    /// </summary>
    public static IReadOnlyList<string> GetPackIds(this TopologyIconCatalog catalog, TopologyIconCatalogQuery? query = null) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        query ??= new TopologyIconCatalogQuery();
        return catalog.Packs
            .Where(pack => PackMatches(pack, query))
            .Select(pack => pack.Id)
            .ToList();
    }

    /// <summary>
    /// Returns matching vendors in deterministic order.
    /// </summary>
    public static IReadOnlyList<string> GetVendors(this TopologyIconCatalog catalog, TopologyIconCatalogQuery? query = null) {
        return catalog.Search(query)
            .Select(item => item.Vendor)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns matching icon categories in deterministic order.
    /// </summary>
    public static IReadOnlyList<string> GetCategories(this TopologyIconCatalog catalog, TopologyIconCatalogQuery? query = null) {
        return catalog.Search(query)
            .Select(item => item.Category)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns matching search tags in deterministic order.
    /// </summary>
    public static IReadOnlyList<string> GetTags(this TopologyIconCatalog catalog, TopologyIconCatalogQuery? query = null) {
        return catalog.Search(query)
            .SelectMany(item => item.PackTags.Concat(item.IconTags))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static bool PackMatches(TopologyIconPack pack, TopologyIconCatalogQuery query) {
        if (pack.IsBuiltIn && !query.IncludeBuiltInPacks) return false;
        if (!pack.IsBuiltIn && !query.IncludeCustomPacks) return false;
        if (!MatchesAny(query.PackIds, pack.Id)) return false;
        if (!MatchesAny(query.Vendors, pack.Vendor)) return false;
        return true;
    }

    private static bool MatchesAny(IReadOnlyList<string> filters, string? value) {
        if (filters.Count == 0) return true;
        if (string.IsNullOrWhiteSpace(value)) return false;
        return filters.Any(filter => string.Equals(filter?.Trim(), value!.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesAllTags(IReadOnlyList<string> filters, TopologyIconPack pack, TopologyIconDefinition icon) {
        if (filters.Count == 0) return true;
        return filters.Where(filter => !string.IsNullOrWhiteSpace(filter))
            .All(filter => ContainsTag(pack.Tags, filter) || ContainsTag(icon.Tags, filter));
    }

    private static bool ContainsTag(IReadOnlyList<string> tags, string? tag) {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return tags.Any(existing => string.Equals(existing, tag!.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> SearchTokens(string? text) {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return text!.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim())
            .Where(token => token.Length > 0)
            .ToList();
    }

    private static bool MatchesSearch(TopologyIconPack pack, TopologyIconDefinition icon, IReadOnlyList<string> tokens) {
        return tokens.All(token => Contains(pack.Id, token)
            || Contains(pack.Label, token)
            || Contains(pack.Vendor, token)
            || Contains(pack.Version, token)
            || pack.Tags.Any(value => Contains(value, token))
            || pack.Metadata.Values.Any(value => Contains(value, token))
            || Contains(icon.Id, token)
            || Contains(icon.QualifiedId, token)
            || Contains(icon.Label, token)
            || Contains(icon.Symbol, token)
            || Contains(icon.Category, token)
            || icon.Tags.Any(value => Contains(value, token))
            || icon.Metadata.Values.Any(value => Contains(value, token)));
    }

    private static bool Contains(string? value, string token) {
        return !string.IsNullOrWhiteSpace(value) && value!.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
