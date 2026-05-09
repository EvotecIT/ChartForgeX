using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Describes a catalog loaded from topology icon-pack manifests plus per-file diagnostics.
/// </summary>
public sealed class TopologyIconCatalogLoadResult {
    internal TopologyIconCatalogLoadResult(TopologyIconCatalog catalog, IReadOnlyList<TopologyIconPackLoadResult> results) {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Results = results ?? throw new ArgumentNullException(nameof(results));
        LoadedPacks = Results.Where(result => result.Succeeded && result.Pack != null).Select(result => result.Pack!).ToList();
        FailedResults = Results.Where(result => result.ErrorMessage != null).ToList();
        SkippedResults = Results.Where(result => result.Skipped).ToList();
    }

    /// <summary>Gets the loaded catalog.</summary>
    public TopologyIconCatalog Catalog { get; }

    /// <summary>Gets per-file load results in deterministic file-name order.</summary>
    public IReadOnlyList<TopologyIconPackLoadResult> Results { get; }

    /// <summary>Gets successfully loaded packs in deterministic file-name order.</summary>
    public IReadOnlyList<TopologyIconPack> LoadedPacks { get; }

    /// <summary>Gets failed manifest results in deterministic file-name order.</summary>
    public IReadOnlyList<TopologyIconPackLoadResult> FailedResults { get; }

    /// <summary>Gets skipped manifest results in deterministic file-name order.</summary>
    public IReadOnlyList<TopologyIconPackLoadResult> SkippedResults { get; }

    /// <summary>Gets the number of successfully loaded packs.</summary>
    public int LoadedCount => LoadedPacks.Count;

    /// <summary>Gets the number of failed manifests.</summary>
    public int FailedCount => FailedResults.Count;

    /// <summary>Gets the number of skipped manifests.</summary>
    public int SkippedCount => SkippedResults.Count;

    /// <summary>Gets whether any manifest failed to load or add to the catalog.</summary>
    public bool HasErrors => FailedCount > 0;

    /// <summary>Gets whether any manifest loaded but was skipped.</summary>
    public bool HasSkipped => SkippedCount > 0;
}
