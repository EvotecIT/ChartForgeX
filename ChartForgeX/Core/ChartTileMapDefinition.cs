using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>
/// Describes reusable tile-map geometry.
/// </summary>
public sealed class ChartTileMapDefinition {
    private readonly ChartTileMapRegion[] _regions;
    private readonly Dictionary<string, string> _aliases;

    /// <summary>
    /// Gets the stable map identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the tile map.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the number of occupied tile columns.
    /// </summary>
    public int ColumnCount { get; }

    /// <summary>
    /// Gets the number of occupied tile rows.
    /// </summary>
    public int RowCount { get; }

    /// <summary>
    /// Gets the drawable regions.
    /// </summary>
    public IReadOnlyList<ChartTileMapRegion> Regions => _regions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartTileMapDefinition"/> class.
    /// </summary>
    /// <param name="id">The stable map identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="regions">The tile regions.</param>
    public ChartTileMapDefinition(string id, string name, IEnumerable<ChartTileMapRegion> regions) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Tile-map definition IDs must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tile-map definition names must not be empty.", nameof(name));
        if (regions == null) throw new ArgumentNullException(nameof(regions));
        Id = id.Trim();
        Name = name.Trim();
        var materialized = regions.ToArray();
        if (materialized.Length == 0) throw new ArgumentException("Tile-map definitions must contain at least one region.", nameof(regions));
        _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in materialized) {
            if (_aliases.ContainsKey(region.Code)) throw new ArgumentException("Duplicate tile-map region code: " + region.Code + ".", nameof(regions));
            var tileKey = region.Column.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + region.Row.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!occupied.Add(tileKey)) throw new ArgumentException("Duplicate tile-map cell: " + tileKey + ".", nameof(regions));
            AddAlias(region.Code, region.Code);
            AddAlias(region.Name, region.Code);
            foreach (var alias in region.Aliases) AddAlias(alias, region.Code);
        }

        _regions = materialized;
        ColumnCount = _regions.Max(region => region.Column) + 1;
        RowCount = _regions.Max(region => region.Row) + 1;
    }

    /// <summary>
    /// Resolves a caller-provided region code or alias to the canonical region code.
    /// </summary>
    /// <param name="region">The code, name, or alias to resolve.</param>
    /// <param name="code">The canonical region code.</param>
    /// <returns><c>true</c> when the region can be resolved; otherwise <c>false</c>.</returns>
    public bool TryResolveRegion(string region, out string code) {
        if (string.IsNullOrWhiteSpace(region)) {
            code = string.Empty;
            return false;
        }

        return _aliases.TryGetValue(region.Trim(), out code!);
    }

    private void AddAlias(string alias, string code) {
        if (string.IsNullOrWhiteSpace(alias)) return;
        if (_aliases.TryGetValue(alias, out var existing) && !string.Equals(existing, code, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Duplicate tile-map region alias: " + alias + ".");
        }

        _aliases[alias] = code;
    }
}
