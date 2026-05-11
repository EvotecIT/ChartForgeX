using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Describes a reusable map catalog entry and how its GeoJSON should be imported.
/// </summary>
public sealed class ChartMapCatalogEntry {
    private readonly Func<ChartMapGeoJsonOptions> _optionsFactory;

    /// <summary>
    /// Gets the stable catalog ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the covered geography, such as EU, US, or World.
    /// </summary>
    public string Geography { get; }

    /// <summary>
    /// Gets the administrative or statistical level, such as NUTS3, state, county, or admin0.
    /// </summary>
    public string Level { get; }

    /// <summary>
    /// Gets the source authority or dataset family.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the recommended join key used by metric data for this map.
    /// </summary>
    public string JoinKey { get; }

    /// <summary>
    /// Gets the source URL or landing page for the geometry.
    /// </summary>
    public string SourceUrl { get; }

    /// <summary>
    /// Gets the recommended local file name when this entry is stored as an external GeoJSON asset.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets a value indicating whether the entry is embedded in the core package.
    /// </summary>
    public bool IsEmbedded { get; }

    /// <summary>
    /// Gets a value indicating whether the entry is normally too large to embed in the core package.
    /// </summary>
    public bool IsLarge { get; }

    /// <summary>
    /// Gets the notes associated with the catalog entry.
    /// </summary>
    public string Notes { get; }

    /// <summary>
    /// Initializes a new catalog entry.
    /// </summary>
    public ChartMapCatalogEntry(
        string id,
        string name,
        string geography,
        string level,
        string source,
        string joinKey,
        string sourceUrl,
        string fileName,
        Func<ChartMapGeoJsonOptions>? optionsFactory = null,
        bool isEmbedded = false,
        bool isLarge = false,
        string? notes = null) {
        Id = Required(id, nameof(id));
        Name = Required(name, nameof(name));
        Geography = Required(geography, nameof(geography));
        Level = Required(level, nameof(level));
        Source = Required(source, nameof(source));
        JoinKey = Required(joinKey, nameof(joinKey));
        SourceUrl = Required(sourceUrl, nameof(sourceUrl));
        FileName = Required(fileName, nameof(fileName));
        _optionsFactory = optionsFactory ?? (() => new ChartMapGeoJsonOptions());
        IsEmbedded = isEmbedded;
        IsLarge = isLarge;
        Notes = notes == null ? string.Empty : notes.Trim();
    }

    /// <summary>
    /// Creates a map definition from GeoJSON using this entry's recommended import options.
    /// </summary>
    /// <param name="geoJson">The GeoJSON document.</param>
    /// <returns>The map definition.</returns>
    public ChartMapDefinition FromGeoJson(string geoJson) {
        return FromGeoJson(geoJson, null);
    }

    /// <summary>
    /// Creates a map definition from GeoJSON using this entry's recommended import options and caller-specific refinements.
    /// </summary>
    /// <param name="geoJson">The GeoJSON document.</param>
    /// <param name="configure">Optional configuration applied to a fresh options instance.</param>
    /// <returns>The map definition.</returns>
    public ChartMapDefinition FromGeoJson(string geoJson, Action<ChartMapGeoJsonOptions>? configure) {
        var options = CreateOptions();
        configure?.Invoke(options);
        return ChartMapDefinition.FromGeoJson(Id, Name, geoJson, options);
    }

    /// <summary>
    /// Creates a map definition from a GeoJSON file using this entry's recommended import options.
    /// </summary>
    /// <param name="path">The GeoJSON file path.</param>
    /// <returns>The map definition.</returns>
    public ChartMapDefinition FromGeoJsonFile(string path) {
        return FromGeoJsonFile(path, null);
    }

    /// <summary>
    /// Creates a map definition from a GeoJSON file using this entry's recommended import options and caller-specific refinements.
    /// </summary>
    /// <param name="path">The GeoJSON file path.</param>
    /// <param name="configure">Optional configuration applied to a fresh options instance.</param>
    /// <returns>The map definition.</returns>
    public ChartMapDefinition FromGeoJsonFile(string path, Action<ChartMapGeoJsonOptions>? configure) {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("GeoJSON file paths must not be empty.", nameof(path));
        return FromGeoJson(System.IO.File.ReadAllText(path), configure);
    }

    /// <summary>
    /// Creates a map definition from this entry's standard file name inside an asset directory.
    /// </summary>
    /// <param name="directory">The directory containing catalog GeoJSON assets.</param>
    /// <returns>The map definition.</returns>
    public ChartMapDefinition FromAssetDirectory(string directory) {
        return FromAssetDirectory(directory, null);
    }

    /// <summary>
    /// Creates a map definition from this entry's standard file name inside an asset directory and caller-specific refinements.
    /// </summary>
    /// <param name="directory">The directory containing catalog GeoJSON assets.</param>
    /// <param name="configure">Optional configuration applied to a fresh options instance.</param>
    /// <returns>The map definition.</returns>
    public ChartMapDefinition FromAssetDirectory(string directory, Action<ChartMapGeoJsonOptions>? configure) {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Catalog asset directories must not be empty.", nameof(directory));
        if (IsEmbedded || string.Equals(FileName, "embedded", StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("Embedded map catalog entries should be loaded with ChartMapCatalog.Get(\"" + Id + "\").");
        }

        return FromGeoJsonFile(System.IO.Path.Combine(directory, FileName), configure);
    }

    /// <summary>
    /// Creates a fresh options instance for this entry.
    /// </summary>
    /// <returns>The recommended GeoJSON import options.</returns>
    public ChartMapGeoJsonOptions CreateOptions() => _optionsFactory();

    internal static IReadOnlyList<ChartMapCatalogEntry> BuiltIns() {
        return Array.AsReadOnly(new[] {
            new ChartMapCatalogEntry(
                "us-states",
                "United States states",
                "US",
                "state",
                "ChartForgeX built-in simplified state geometry",
                "state abbreviation",
                "embedded",
                "embedded",
                isEmbedded: true,
                notes: "Small built-in geometry intended for dependency-free examples and dashboards.")
        });
    }

    private static string Required(string value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Catalog entry values must not be empty.", parameterName);
        return value.Trim();
    }
}
