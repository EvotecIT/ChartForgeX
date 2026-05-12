using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Configures how GeoJSON features are converted into map regions.
/// </summary>
public sealed class ChartMapGeoJsonOptions {
    private string[] _codePropertyNames = new[] { "NUTS_ID", "nuts_id", "id", "code", "CODE", "ISO_A2", "iso_a2", "GEOID", "geoid" };
    private string[] _namePropertyNames = new[] { "NUTS_NAME", "NAME_LATN", "name", "NAME", "nam_en", "label", "LABEL" };
    private string[] _aliasPropertyNames = new[] { "aliases", "alias" };
    private string _generatedCodePrefix = "R";
    private int _coordinatePrecision = 4;
    private readonly Dictionary<string, HashSet<string>> _requiredPropertyValues = new(StringComparer.OrdinalIgnoreCase);
    private double? _minimumLongitude;
    private double? _maximumLongitude;
    private double? _minimumLatitude;
    private double? _maximumLatitude;

    /// <summary>
    /// Gets or sets property names searched for canonical region codes.
    /// </summary>
    public string[] CodePropertyNames {
        get => Clone(_codePropertyNames);
        set => _codePropertyNames = NormalizeNames(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets property names searched for region display names.
    /// </summary>
    public string[] NamePropertyNames {
        get => Clone(_namePropertyNames);
        set => _namePropertyNames = NormalizeNames(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets property names searched for alternate region aliases.
    /// </summary>
    public string[] AliasPropertyNames {
        get => Clone(_aliasPropertyNames);
        set => _aliasPropertyNames = NormalizeNames(value, nameof(value), allowEmpty: true);
    }

    /// <summary>
    /// Gets or sets the prefix used when a feature has no code-like property.
    /// </summary>
    public string GeneratedCodePrefix {
        get => _generatedCodePrefix;
        set {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Generated code prefix must not be empty.", nameof(value));
            _generatedCodePrefix = value.Trim();
        }
    }

    /// <summary>
    /// Gets or sets the coordinate projection used for longitude/latitude coordinates.
    /// </summary>
    public ChartMapGeoJsonProjection Projection { get; set; } = ChartMapGeoJsonProjection.Equirectangular;

    /// <summary>
    /// Gets or sets a value indicating whether unsupported non-polygon geometries are skipped.
    /// </summary>
    public bool SkipUnsupportedGeometries { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a top-level GeoJSON feature ID should be added as an alias.
    /// </summary>
    public bool IncludeFeatureIdAsAlias { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum longitude used to include GeoJSON features. Leave unset to include every longitude.
    /// </summary>
    public double? MinimumLongitude {
        get => _minimumLongitude;
        set {
            ValidateLongitude(value, nameof(value));
            _minimumLongitude = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum longitude used to include GeoJSON features. Leave unset to include every longitude.
    /// </summary>
    public double? MaximumLongitude {
        get => _maximumLongitude;
        set {
            ValidateLongitude(value, nameof(value));
            _maximumLongitude = value;
        }
    }

    /// <summary>
    /// Gets or sets the minimum latitude used to include GeoJSON features. Leave unset to include every latitude.
    /// </summary>
    public double? MinimumLatitude {
        get => _minimumLatitude;
        set {
            ValidateLatitude(value, nameof(value));
            _minimumLatitude = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum latitude used to include GeoJSON features. Leave unset to include every latitude.
    /// </summary>
    public double? MaximumLatitude {
        get => _maximumLatitude;
        set {
            ValidateLatitude(value, nameof(value));
            _maximumLatitude = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether partially overlapping features are included when coordinate bounds are configured.
    /// </summary>
    public bool IncludeFeaturesIntersectingCoordinateBounds { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum decimal places written to generated SVG path coordinates.
    /// </summary>
    public int CoordinatePrecision {
        get => _coordinatePrecision;
        set {
            if (value < 0 || value > 8) throw new ArgumentOutOfRangeException(nameof(value), value, "Coordinate precision must be between zero and eight decimal places.");
            _coordinatePrecision = value;
        }
    }

    /// <summary>
    /// Configures a longitude/latitude window used to include imported GeoJSON features.
    /// </summary>
    /// <param name="minimumLongitude">The minimum longitude in degrees.</param>
    /// <param name="maximumLongitude">The maximum longitude in degrees.</param>
    /// <param name="minimumLatitude">The minimum latitude in degrees.</param>
    /// <param name="maximumLatitude">The maximum latitude in degrees.</param>
    /// <param name="includeIntersections">Whether features crossing the window are included.</param>
    /// <returns>The current options instance.</returns>
    public ChartMapGeoJsonOptions WithCoordinateBounds(double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude, bool includeIntersections = true) {
        MinimumLongitude = minimumLongitude;
        MaximumLongitude = maximumLongitude;
        MinimumLatitude = minimumLatitude;
        MaximumLatitude = maximumLatitude;
        IncludeFeaturesIntersectingCoordinateBounds = includeIntersections;
        return this;
    }

    /// <summary>
    /// Limits imported features to those whose property value matches one of the supplied values.
    /// </summary>
    /// <param name="propertyName">The GeoJSON feature property name.</param>
    /// <param name="values">The accepted property values.</param>
    /// <returns>The current options instance.</returns>
    public ChartMapGeoJsonOptions IncludeFeaturePropertyValues(string propertyName, params string[] values) {
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Feature property names must not be empty.", nameof(propertyName));
        if (values == null) throw new ArgumentNullException(nameof(values));
        var accepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values) {
            if (string.IsNullOrWhiteSpace(value)) continue;
            accepted.Add(value.Trim());
        }

        if (accepted.Count == 0) throw new ArgumentException("At least one feature property value is required.", nameof(values));
        _requiredPropertyValues[propertyName.Trim()] = accepted;
        return this;
    }

    internal string FindCode(GeoJsonFeature feature, int index, ISet<string>? usedCodes = null) {
        foreach (var name in _codePropertyNames) {
            var value = feature.PropertyString(name);
            if (!string.IsNullOrWhiteSpace(value)) return value!.Trim();
        }

        if (!string.IsNullOrWhiteSpace(feature.Id)) return feature.Id!.Trim();
        var suffix = index;
        var code = _generatedCodePrefix + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        while (usedCodes != null && usedCodes.Contains(code)) {
            suffix++;
            code = _generatedCodePrefix + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return code;
    }

    internal string FindName(GeoJsonFeature feature, string code) {
        foreach (var name in _namePropertyNames) {
            var value = feature.PropertyString(name);
            if (!string.IsNullOrWhiteSpace(value)) return value!.Trim();
        }

        return code;
    }

    internal string[] FindAliases(GeoJsonFeature feature, string code, string name) {
        var aliases = new System.Collections.Generic.List<string>();
        if (IncludeFeatureIdAsAlias && !string.IsNullOrWhiteSpace(feature.Id)) aliases.Add(feature.Id!.Trim());
        foreach (var propertyName in _aliasPropertyNames) feature.AddPropertyAliases(propertyName, aliases);
        for (var i = aliases.Count - 1; i >= 0; i--) {
            if (string.Equals(aliases[i], code, StringComparison.OrdinalIgnoreCase) || string.Equals(aliases[i], name, StringComparison.OrdinalIgnoreCase)) aliases.RemoveAt(i);
        }

        return aliases.ToArray();
    }

    internal bool IncludesFeature(GeoJsonFeature feature) {
        foreach (var item in _requiredPropertyValues) {
            var value = feature.PropertyString(item.Key);
            if (value == null || !item.Value.Contains(value.Trim())) return false;
        }

        return true;
    }

    internal void Validate() {
        if (_minimumLongitude.HasValue && _maximumLongitude.HasValue && _maximumLongitude.Value <= _minimumLongitude.Value) {
            throw new ArgumentOutOfRangeException(nameof(MaximumLongitude), _maximumLongitude.Value, "Maximum longitude must be greater than minimum longitude.");
        }

        if (_minimumLatitude.HasValue && _maximumLatitude.HasValue && _maximumLatitude.Value <= _minimumLatitude.Value) {
            throw new ArgumentOutOfRangeException(nameof(MaximumLatitude), _maximumLatitude.Value, "Maximum latitude must be greater than minimum latitude.");
        }
    }

    internal bool IncludesCoordinateBounds(double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude) {
        if (!_minimumLongitude.HasValue && !_maximumLongitude.HasValue && !_minimumLatitude.HasValue && !_maximumLatitude.HasValue) return true;
        var minLon = _minimumLongitude ?? -180;
        var maxLon = _maximumLongitude ?? 180;
        var minLat = _minimumLatitude ?? -90;
        var maxLat = _maximumLatitude ?? 90;
        if (IncludeFeaturesIntersectingCoordinateBounds) {
            return maximumLongitude >= minLon && minimumLongitude <= maxLon && maximumLatitude >= minLat && minimumLatitude <= maxLat;
        }

        return minimumLongitude >= minLon && maximumLongitude <= maxLon && minimumLatitude >= minLat && maximumLatitude <= maxLat;
    }

    private static string[] Clone(string[] values) {
        var clone = new string[values.Length];
        Array.Copy(values, clone, values.Length);
        return clone;
    }

    private static string[] NormalizeNames(string[]? values, string parameterName, bool allowEmpty = false) {
        if (values == null) throw new ArgumentNullException(parameterName);
        var names = new System.Collections.Generic.List<string>();
        foreach (var value in values) {
            if (string.IsNullOrWhiteSpace(value)) continue;
            names.Add(value.Trim());
        }

        if (!allowEmpty && names.Count == 0) throw new ArgumentException("At least one property name is required.", parameterName);
        return names.ToArray();
    }

    private static void ValidateLongitude(double? value, string parameterName) {
        if (!value.HasValue) return;
        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < -180 || value.Value > 180) throw new ArgumentOutOfRangeException(parameterName, value.Value, "Longitude must be between -180 and 180 degrees.");
    }

    private static void ValidateLatitude(double? value, string parameterName) {
        if (!value.HasValue) return;
        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < -90 || value.Value > 90) throw new ArgumentOutOfRangeException(parameterName, value.Value, "Latitude must be between -90 and 90 degrees.");
    }
}
