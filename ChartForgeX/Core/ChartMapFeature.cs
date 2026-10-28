using System;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Core;

/// <summary>An immutable map feature ID and JSON property snapshot, independent of series observation identity.</summary>
public sealed class ChartMapFeature {
    /// <summary>Gets the original feature ID as text, or null when it was absent or null. IDs are never generated.</summary>
    public string? Id { get; }
    /// <summary>Gets the JSON scalar spelling of the ID, distinguishing numeric and string IDs. Null means absent; the text "null" retains explicit source null.</summary>
    public string? IdJson { get; }
    /// <summary>Gets the JSON object or null-value snapshot with property-name case, nested values and number spelling retained; null means absent properties.</summary>
    public string? PropertiesJson { get; }

    /// <summary>Creates feature metadata with an optional string ID and a JSON properties object.</summary>
    /// <param name="id">A string feature ID, distinct from an observation ID; null leaves it absent.</param>
    /// <param name="propertiesJson">A JSON object, the JSON text "null", or null for absent properties. The existing parser rejects duplicate keys, excessive nesting, invalid finite numbers and malformed JSON.</param>
    /// <remarks>The existing 8 Mi-character and 600,000-value JSON bounds apply. The snapshot contains no geometry or executable markup.
    /// GeoJSON imports additionally retain the source ID's string, number or explicit null in IdJson.</remarks>
    public ChartMapFeature(string? id = null, string? propertiesJson = "{}")
        : this(id == null ? null : GeoJsonValue.String(id), ParseProperties(propertiesJson)) { }

    private ChartMapFeature(GeoJsonValue? id, GeoJsonValue? properties) {
        if (id != null && !id.IsNull && id.RawValue is not (string or double or GeoJsonNumber))
            throw new ArgumentException("A map feature ID must be a string, finite number or null.", nameof(id));
        if (id?.RawValue is string idText && idText.Length > VisualArtifactInterchangeEnvelope.MaximumJsonCharacters)
            throw new ArgumentException("Map feature ID exceeds the supported JSON size.", nameof(id));
        Id = id?.AsOptionalString();
        IdJson = id == null ? null : Write(id);
        PropertiesJson = properties == null ? null : Write(properties);
        if ((long)(IdJson?.Length ?? 0) + (PropertiesJson?.Length ?? 0) > VisualArtifactInterchangeEnvelope.MaximumJsonCharacters)
            throw new ArgumentException("Map feature metadata exceeds the supported JSON size.");
    }

    internal static ChartMapFeature FromFeature(GeoJsonFeature feature) => new(feature.IdValue, feature.PropertiesValue);
    internal int CharacterCount => checked((IdJson?.Length ?? 0) + (PropertiesJson?.Length ?? 0));

    private static GeoJsonValue? ParseProperties(string? json) {
        if (json == null) return null;
        if (json.Length > VisualArtifactInterchangeEnvelope.MaximumJsonCharacters)
            throw new ArgumentException("Map feature properties exceed the supported JSON size.", nameof(json));
        var limit = VisualArtifactInterchangeEnvelope.MaximumJsonValues;
        var value = GeoJsonValue.Parse(json, StringComparer.Ordinal, new GeoJsonReadLimits(limit, limit, limit).KeepNumberSpelling().RejectDuplicates());
        if (!value.IsNull) _ = value.AsObject("map feature properties"); return value;
    }

    private static string Write(GeoJsonValue value) {
        var writer = new VisualArtifactInterchangeJsonWriter(escapeMarkupUnsafeCharacters: true); writer.Value(value); return writer.ToString();
    }
}
