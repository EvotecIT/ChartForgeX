using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Describes one drawable region inside a map definition.
/// </summary>
public readonly struct ChartMapRegion {
    private readonly IReadOnlyList<string>? _aliases;

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

    /// <summary>Gets optional immutable source feature metadata, independent of region observation identity.</summary>
    public ChartMapFeature? Feature { get; }
    /// <summary>Gets the immutable original imported geometry JSON of a seam-aware polygon, or null for ordinary/manual geometry.</summary>
    /// <remarks>Complete raw rings, extra coordinate dimensions and original number spelling are retained through the existing bounded JSON owner.</remarks>
    public string? GeoJsonGeometry { get; }

    /// <summary>
    /// Gets alternate codes or names that should resolve to this region.
    /// </summary>
    public IReadOnlyList<string> Aliases => _aliases ?? Array.Empty<string>();

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
        _aliases = Array.AsReadOnly(MaterializeAliases(aliases)); Feature = null; GeoJsonGeometry = null;
    }

    private ChartMapRegion(ChartMapRegion source, ChartMapFeature? feature, string? geometry) {
        Code = source.Code; Name = source.Name; Path = source.Path; HasLabel = source.HasLabel; Label = source.Label;
        _aliases = source._aliases; Feature = feature; GeoJsonGeometry = geometry;
    }
    /// <summary>Returns the same region geometry with replacement immutable feature metadata. No observation ID is assigned.</summary>
    /// <param name="feature">A snapshot, or null to clear it. Imported raw geometry is retained.</param>
    /// <returns>An independent region with unchanged paths, code, name and aliases.</returns>
    public ChartMapRegion WithFeature(ChartMapFeature? feature) => new(this, feature, GeoJsonGeometry);
    internal ChartMapRegion WithGeoJsonSource(ChartMapFeature feature, string geometry) => new(this, feature, geometry);

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
