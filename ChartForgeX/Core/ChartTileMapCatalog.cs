using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Provides generic access to built-in reusable tile-map definitions.
/// </summary>
public static class ChartTileMapCatalog {
    /// <summary>
    /// Gets all built-in tile-map definitions.
    /// </summary>
    /// <returns>The available tile-map definitions.</returns>
    public static IReadOnlyList<ChartTileMapDefinition> All() => BuiltInTileMapDefinitions.All();

    /// <summary>
    /// Gets a built-in tile-map definition by ID.
    /// </summary>
    /// <param name="id">The tile-map definition ID.</param>
    /// <returns>The requested tile-map definition.</returns>
    public static ChartTileMapDefinition Get(string id) {
        if (TryGet(id, out var definition)) return definition;
        throw new ArgumentException("Unknown tile-map definition ID: " + id + ".", nameof(id));
    }

    /// <summary>
    /// Attempts to get a built-in tile-map definition by ID.
    /// </summary>
    /// <param name="id">The tile-map definition ID.</param>
    /// <param name="definition">The resolved tile-map definition.</param>
    /// <returns><c>true</c> when the definition exists; otherwise <c>false</c>.</returns>
    public static bool TryGet(string id, out ChartTileMapDefinition definition) {
        return BuiltInTileMapDefinitions.TryGet(id, out definition);
    }
}
