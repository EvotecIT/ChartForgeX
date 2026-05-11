using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines options for exporting topology icon catalogs as manifest files.
/// </summary>
public sealed class TopologyIconCatalogExportOptions {
    /// <summary>Gets or sets whether built-in packs should be exported.</summary>
    public bool IncludeBuiltInPacks { get; set; }

    /// <summary>Gets or sets whether existing files may be overwritten.</summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>Gets or sets whether exported JSON should be indented.</summary>
    public bool Indented { get; set; } = true;

    /// <summary>Gets or sets the exported file-name prefix.</summary>
    public string FileNamePrefix { get; set; } = "topology-icon-pack.";

    /// <summary>Gets or sets the exported file-name extension.</summary>
    public string FileExtension { get; set; } = ".json";

    /// <summary>Gets or sets an optional catalog query used to choose packs and icons before export.</summary>
    public TopologyIconCatalogQuery? Query { get; set; }
}

/// <summary>
/// Provides manifest export helpers for topology icon catalogs.
/// </summary>
public static class TopologyIconCatalogExportExtensions {
    /// <summary>
    /// Exports matching icon packs to deterministic JSON manifest files.
    /// </summary>
    /// <param name="catalog">The source catalog.</param>
    /// <param name="directoryPath">The target directory.</param>
    /// <param name="options">Optional export settings.</param>
    /// <returns>The exported manifest file paths.</returns>
    public static IReadOnlyList<string> SaveJsonManifestsToDirectory(this TopologyIconCatalog catalog, string directoryPath, TopologyIconCatalogExportOptions? options = null) {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException("Value cannot be empty.", nameof(directoryPath));
        options ??= new TopologyIconCatalogExportOptions();
        Directory.CreateDirectory(directoryPath);

        var exportedPaths = new List<string>();
        foreach (var pack in MatchingPacks(catalog, options)) {
            var path = Path.Combine(directoryPath, FileNameForPack(pack, options));
            if (!options.Overwrite && File.Exists(path)) throw new IOException("Topology icon pack manifest already exists: " + path);
            TopologyIconPackJson.WriteUtf8NoBomFile(path, pack.ToJsonManifest(options.Indented));
            exportedPaths.Add(path);
        }

        return exportedPaths;
    }

    private static IEnumerable<TopologyIconPack> MatchingPacks(TopologyIconCatalog catalog, TopologyIconCatalogExportOptions options) {
        if (options.Query == null) {
            return catalog.Packs.Where(pack => options.IncludeBuiltInPacks || !pack.IsBuiltIn);
        }

        var summaries = catalog.GetPackSummaries(options.Query);
        return summaries
            .Where(summary => options.IncludeBuiltInPacks || !summary.IsBuiltIn)
            .Select(summary => summary.Pack);
    }

    private static string FileNameForPack(TopologyIconPack pack, TopologyIconCatalogExportOptions options) {
        var extension = string.IsNullOrWhiteSpace(options.FileExtension) ? ".json" : options.FileExtension.Trim();
        if (!extension.StartsWith(".", StringComparison.Ordinal)) extension = "." + extension;
        return RequiredText(options.FileNamePrefix, nameof(options.FileNamePrefix)) + StableFileToken(pack.Id) + extension;
    }

    private static string StableFileToken(string value) {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value) builder.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');
        var token = builder.ToString().Trim('-');
        return token.Length == 0 ? "pack" : token;
    }

    private static string RequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameterName);
        return value!.Trim();
    }
}
