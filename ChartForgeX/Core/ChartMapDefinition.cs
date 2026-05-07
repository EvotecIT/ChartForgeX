using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Describes reusable map geometry that can be rendered with different map visual styles.
/// </summary>
public sealed class ChartMapDefinition {
    private readonly ChartMapRegion[] _regions;
    private readonly Dictionary<string, string> _aliases;

    /// <summary>
    /// Gets the stable map identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the map.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the coordinate bounds occupied by the map paths.
    /// </summary>
    public ChartRect Bounds { get; }

    /// <summary>
    /// Gets the drawable regions.
    /// </summary>
    public IReadOnlyList<ChartMapRegion> Regions => _regions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapDefinition"/> class with bounds starting at zero.
    /// </summary>
    /// <param name="id">The stable map identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="width">The map coordinate width.</param>
    /// <param name="height">The map coordinate height.</param>
    /// <param name="regions">The drawable regions.</param>
    public ChartMapDefinition(string id, string name, double width, double height, IEnumerable<ChartMapRegion> regions)
        : this(id, name, new ChartRect(0, 0, width, height), regions) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapDefinition"/> class.
    /// </summary>
    /// <param name="id">The stable map identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="bounds">The coordinate bounds occupied by the map paths.</param>
    /// <param name="regions">The drawable regions.</param>
    public ChartMapDefinition(string id, string name, ChartRect bounds, IEnumerable<ChartMapRegion> regions) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Map definition IDs must not be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Map definition names must not be empty.", nameof(name));
        if (regions == null) throw new ArgumentNullException(nameof(regions));
        ValidateBounds(bounds);
        Id = id.Trim();
        Name = name.Trim();
        Bounds = bounds;
        var materialized = new List<ChartMapRegion>();
        _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in regions) {
            if (_aliases.ContainsKey(region.Code)) throw new ArgumentException("Duplicate map region code: " + region.Code + ".", nameof(regions));
            materialized.Add(region);
            AddAlias(region.Code, region.Code);
            AddAlias(region.Name, region.Code);
            foreach (var alias in region.Aliases) AddAlias(alias, region.Code);
        }

        if (materialized.Count == 0) throw new ArgumentException("Map definitions must contain at least one region.", nameof(regions));
        _regions = materialized.ToArray();
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
            throw new ArgumentException("Duplicate map region alias: " + alias + ".");
        }

        _aliases[alias] = code;
    }

    private static void ValidateBounds(ChartRect bounds) {
        if (!IsFinite(bounds.Left) || !IsFinite(bounds.Top) || !IsFinite(bounds.Width) || !IsFinite(bounds.Height)) {
            throw new ArgumentOutOfRangeException(nameof(bounds), "Map definition bounds must be finite.");
        }

        if (bounds.Width <= 0 || bounds.Height <= 0) {
            throw new ArgumentOutOfRangeException(nameof(bounds), "Map definition bounds must have positive width and height.");
        }
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
