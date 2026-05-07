using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Describes one region in a tile-map definition.
/// </summary>
public readonly struct ChartTileMapRegion {
    private readonly string[] _aliases;

    /// <summary>
    /// Gets the canonical region code.
    /// </summary>
    public readonly string Code;

    /// <summary>
    /// Gets the display name for the region.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Gets the tile column.
    /// </summary>
    public readonly int Column;

    /// <summary>
    /// Gets the tile row.
    /// </summary>
    public readonly int Row;

    /// <summary>
    /// Gets alternate codes or names that should resolve to this region.
    /// </summary>
    public IReadOnlyList<string> Aliases => _aliases;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartTileMapRegion"/> struct.
    /// </summary>
    /// <param name="code">The canonical region code.</param>
    /// <param name="name">The display name for the region.</param>
    /// <param name="column">The tile column.</param>
    /// <param name="row">The tile row.</param>
    /// <param name="aliases">Optional alternate codes or names.</param>
    public ChartTileMapRegion(string code, string name, int column, int row, IEnumerable<string>? aliases = null) {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Tile-map region codes must not be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tile-map region names must not be empty.", nameof(name));
        if (column < 0) throw new ArgumentOutOfRangeException(nameof(column), column, "Tile-map columns must be zero or greater.");
        if (row < 0) throw new ArgumentOutOfRangeException(nameof(row), row, "Tile-map rows must be zero or greater.");
        Code = code.Trim();
        Name = name.Trim();
        Column = column;
        Row = row;
        _aliases = MaterializeAliases(aliases);
    }

    private static string[] MaterializeAliases(IEnumerable<string>? aliases) {
        if (aliases == null) return Array.Empty<string>();
        var values = new List<string>();
        foreach (var alias in aliases) {
            if (string.IsNullOrWhiteSpace(alias)) continue;
            values.Add(alias.Trim());
        }

        return values.ToArray();
    }
}
