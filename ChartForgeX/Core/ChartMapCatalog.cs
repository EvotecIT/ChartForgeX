using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Provides generic access to built-in reusable map definitions.
/// </summary>
public static class ChartMapCatalog {
    /// <summary>
    /// Gets all built-in map definitions.
    /// </summary>
    /// <returns>The available map definitions.</returns>
    public static IReadOnlyList<ChartMapDefinition> All() => BuiltInMapDefinitions.All();

    /// <summary>
    /// Gets metadata for built-in and known external map definitions.
    /// </summary>
    /// <returns>The available catalog entries.</returns>
    public static IReadOnlyList<ChartMapCatalogEntry> Entries() {
        var embedded = ChartMapCatalogEntry.BuiltIns();
        var external = ChartMapExternalCatalog.Entries();
        var entries = new ChartMapCatalogEntry[embedded.Count + external.Count];
        for (var i = 0; i < embedded.Count; i++) entries[i] = embedded[i];
        for (var i = 0; i < external.Count; i++) entries[embedded.Count + i] = external[i];
        return Array.AsReadOnly(entries);
    }

    /// <summary>
    /// Gets a built-in map definition by ID.
    /// </summary>
    /// <param name="id">The map definition ID.</param>
    /// <returns>The requested map definition.</returns>
    public static ChartMapDefinition Get(string id) {
        if (TryGet(id, out var definition)) return definition;
        throw new ArgumentException("Unknown map definition ID: " + id + ".", nameof(id));
    }

    /// <summary>
    /// Attempts to get a built-in map definition by ID.
    /// </summary>
    /// <param name="id">The map definition ID.</param>
    /// <param name="definition">The resolved map definition.</param>
    /// <returns><c>true</c> when the definition exists; otherwise <c>false</c>.</returns>
    public static bool TryGet(string id, out ChartMapDefinition definition) {
        return BuiltInMapDefinitions.TryGet(id, out definition);
    }

    /// <summary>
    /// Loads a map definition by catalog ID, using embedded geometry when available or an asset directory for external entries.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="assetDirectory">The optional directory containing external catalog GeoJSON assets.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition Load(string id, string? assetDirectory = null) {
        return Load(id, assetDirectory, null);
    }

    /// <summary>
    /// Loads a map definition by catalog ID, using embedded geometry when available or an asset directory for external entries.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="assetDirectory">The optional directory containing external catalog GeoJSON assets.</param>
    /// <param name="configure">Optional configuration applied to external GeoJSON import options.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition Load(string id, string? assetDirectory, Action<ChartMapGeoJsonOptions>? configure) {
        if (TryGet(id, out var embedded)) return embedded;
        var entry = GetEntry(id);
        if (assetDirectory == null) {
            throw new ArgumentException("Map catalog entry '" + id + "' is external. Provide an asset directory containing " + entry.FileName + ".", nameof(assetDirectory));
        }

        return entry.FromAssetDirectory(assetDirectory, configure);
    }

    /// <summary>
    /// Gets metadata for a built-in or known external map definition.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <returns>The requested catalog entry.</returns>
    public static ChartMapCatalogEntry GetEntry(string id) {
        if (TryGetEntry(id, out var entry)) return entry;
        throw new ArgumentException("Unknown map catalog entry ID: " + id + ".", nameof(id));
    }

    /// <summary>
    /// Attempts to get metadata for a built-in or known external map definition.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="entry">The resolved catalog entry.</param>
    /// <returns><c>true</c> when the entry exists; otherwise <c>false</c>.</returns>
    public static bool TryGetEntry(string id, out ChartMapCatalogEntry entry) {
        if (string.IsNullOrWhiteSpace(id)) {
            entry = null!;
            return false;
        }

        foreach (var candidate in Entries()) {
            if (string.Equals(candidate.Id, id.Trim(), StringComparison.OrdinalIgnoreCase)) {
                entry = candidate;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    /// <summary>
    /// Loads a known external map definition from a GeoJSON file.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="path">The local GeoJSON file path.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition FromGeoJsonFile(string id, string path) {
        return GetEntry(id).FromGeoJsonFile(path);
    }

    /// <summary>
    /// Loads a known external map definition from a GeoJSON file with caller-specific import refinements.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="path">The local GeoJSON file path.</param>
    /// <param name="configure">Optional configuration applied to a fresh options instance.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition FromGeoJsonFile(string id, string path, Action<ChartMapGeoJsonOptions>? configure) {
        return GetEntry(id).FromGeoJsonFile(path, configure);
    }

    /// <summary>
    /// Loads a known external map definition from a catalog asset directory using the entry's standard file name.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="directory">The directory containing catalog GeoJSON assets.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition FromAssetDirectory(string id, string directory) {
        return GetEntry(id).FromAssetDirectory(directory);
    }

    /// <summary>
    /// Loads a known external map definition from a catalog asset directory with caller-specific import refinements.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="directory">The directory containing catalog GeoJSON assets.</param>
    /// <param name="configure">Optional configuration applied to a fresh options instance.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition FromAssetDirectory(string id, string directory, Action<ChartMapGeoJsonOptions>? configure) {
        return GetEntry(id).FromAssetDirectory(directory, configure);
    }

    /// <summary>
    /// Loads a known external map definition from a GeoJSON document.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="geoJson">The GeoJSON document.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition FromGeoJson(string id, string geoJson) {
        return GetEntry(id).FromGeoJson(geoJson);
    }

    /// <summary>
    /// Loads a known external map definition from a GeoJSON document with caller-specific import refinements.
    /// </summary>
    /// <param name="id">The catalog entry ID.</param>
    /// <param name="geoJson">The GeoJSON document.</param>
    /// <param name="configure">Optional configuration applied to a fresh options instance.</param>
    /// <returns>The loaded map definition.</returns>
    public static ChartMapDefinition FromGeoJson(string id, string geoJson, Action<ChartMapGeoJsonOptions>? configure) {
        return GetEntry(id).FromGeoJson(geoJson, configure);
    }
}
