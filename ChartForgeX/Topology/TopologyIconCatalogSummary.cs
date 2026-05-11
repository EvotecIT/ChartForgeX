using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Summarizes one topology icon pack for host-side picker folders and catalog browsers.
/// </summary>
public sealed class TopologyIconPackSummary {
    internal TopologyIconPackSummary(TopologyIconPack pack, IReadOnlyList<TopologyIconDefinition> icons) {
        Pack = pack;
        PackId = pack.Id;
        Label = pack.Label;
        Vendor = pack.Vendor;
        Version = pack.Version;
        IsBuiltIn = pack.IsBuiltIn;
        IconCount = icons.Count;
        Categories = DistinctSorted(icons.Select(icon => icon.Category));
        Tags = DistinctSorted(pack.Tags.Concat(icons.SelectMany(icon => icon.Tags)));
        SourcePath = pack.Metadata.TryGetValue("manifest.path", out var path) ? path : null;
    }

    /// <summary>Gets the source icon pack.</summary>
    public TopologyIconPack Pack { get; }

    /// <summary>Gets the source pack id.</summary>
    public string PackId { get; }

    /// <summary>Gets the source pack label.</summary>
    public string Label { get; }

    /// <summary>Gets the optional source pack vendor.</summary>
    public string? Vendor { get; }

    /// <summary>Gets the optional source pack version.</summary>
    public string? Version { get; }

    /// <summary>Gets whether the pack is supplied by ChartForgeX.</summary>
    public bool IsBuiltIn { get; }

    /// <summary>Gets the number of icons matching the query.</summary>
    public int IconCount { get; }

    /// <summary>Gets matching icon categories in deterministic order.</summary>
    public IReadOnlyList<string> Categories { get; }

    /// <summary>Gets pack and icon tags in deterministic order.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Gets the manifest file path when the pack was loaded from disk.</summary>
    public string? SourcePath { get; }

    private static IReadOnlyList<string> DistinctSorted(IEnumerable<string?> values) {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>
/// Summarizes icon packs grouped by vendor or source family.
/// </summary>
public sealed class TopologyIconVendorSummary {
    internal TopologyIconVendorSummary(string label, IReadOnlyList<TopologyIconPackSummary> packs) {
        Label = label;
        Packs = packs;
        PackCount = packs.Count;
        IconCount = packs.Sum(pack => pack.IconCount);
        PackIds = packs.Select(pack => pack.PackId).ToList();
        Categories = DistinctSorted(packs.SelectMany(pack => pack.Categories));
        Tags = DistinctSorted(packs.SelectMany(pack => pack.Tags));
        HasBuiltInPacks = packs.Any(pack => pack.IsBuiltIn);
        HasCustomPacks = packs.Any(pack => !pack.IsBuiltIn);
    }

    /// <summary>Gets the vendor or source-family label.</summary>
    public string Label { get; }

    /// <summary>Gets source packs in catalog order.</summary>
    public IReadOnlyList<TopologyIconPackSummary> Packs { get; }

    /// <summary>Gets the number of matching packs.</summary>
    public int PackCount { get; }

    /// <summary>Gets the number of matching icons.</summary>
    public int IconCount { get; }

    /// <summary>Gets matching pack ids in catalog order.</summary>
    public IReadOnlyList<string> PackIds { get; }

    /// <summary>Gets matching icon categories in deterministic order.</summary>
    public IReadOnlyList<string> Categories { get; }

    /// <summary>Gets pack and icon tags in deterministic order.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Gets whether the folder contains built-in packs.</summary>
    public bool HasBuiltInPacks { get; }

    /// <summary>Gets whether the folder contains custom or vendor packs.</summary>
    public bool HasCustomPacks { get; }

    private static IReadOnlyList<string> DistinctSorted(IEnumerable<string> values) {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>
/// Provides host-facing summaries for topology icon catalog pickers.
/// </summary>
public static class TopologyIconCatalogSummaryExtensions {
    /// <summary>
    /// Returns icon-pack summaries in catalog order.
    /// </summary>
    public static IReadOnlyList<TopologyIconPackSummary> GetPackSummaries(this TopologyIconCatalog catalog, TopologyIconCatalogQuery? query = null) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        query ??= new TopologyIconCatalogQuery();
        var iconItemsByPack = catalog.Search(query)
            .GroupBy(item => item.PackId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Icon).ToList(), StringComparer.OrdinalIgnoreCase);
        return catalog.Packs
            .Where(pack => TopologyIconCatalogQueryExtensions.PackMatches(pack, query) && iconItemsByPack.ContainsKey(pack.Id))
            .Select(pack => new TopologyIconPackSummary(pack, iconItemsByPack[pack.Id]))
            .ToList();
    }

    /// <summary>
    /// Returns vendor/source folders for matching icon packs.
    /// </summary>
    public static IReadOnlyList<TopologyIconVendorSummary> GetVendorSummaries(this TopologyIconCatalog catalog, TopologyIconCatalogQuery? query = null) {
        return catalog.GetPackSummaries(query)
            .GroupBy(VendorFolderLabel, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TopologyIconVendorSummary(group.Key, group.ToList()))
            .ToList();
    }

    private static string VendorFolderLabel(TopologyIconPackSummary pack) {
        if (!string.IsNullOrWhiteSpace(pack.Vendor)) return pack.Vendor!;
        return pack.IsBuiltIn ? "ChartForgeX" : "Custom";
    }
}
