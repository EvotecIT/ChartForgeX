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
}
