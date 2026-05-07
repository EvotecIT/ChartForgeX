using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Describes one drawable region inside a map definition.
/// </summary>
public readonly struct ChartMapRegion {
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
    /// Gets the SVG path data for the region in the definition coordinate space.
    /// </summary>
    public readonly string Path;

    /// <summary>
    /// Gets a value indicating whether the region has a preferred label point.
    /// </summary>
    public readonly bool HasLabel;

    /// <summary>
    /// Gets the preferred label point in the definition coordinate space.
    /// </summary>
    public readonly ChartPoint Label;

    /// <summary>
    /// Gets alternate codes or names that should resolve to this region.
    /// </summary>
    public IReadOnlyList<string> Aliases => _aliases;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapRegion"/> struct.
    /// </summary>
    /// <param name="code">The canonical region code.</param>
    /// <param name="name">The display name for the region.</param>
    /// <param name="path">The SVG path data for the region.</param>
    /// <param name="label">An optional preferred label point.</param>
    /// <param name="aliases">Optional alternate codes or names.</param>
    public ChartMapRegion(string code, string name, string path, ChartPoint? label = null, IEnumerable<string>? aliases = null) {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Map region codes must not be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Map region names must not be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Map region paths must not be empty.", nameof(path));
        Code = code.Trim();
        Name = name.Trim();
        Path = path.Trim();
        HasLabel = label.HasValue;
        Label = label.GetValueOrDefault();
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
