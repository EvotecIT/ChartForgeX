using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal static class BuiltInMapDefinitions {
    private static readonly object Gate = new();
    private static readonly Dictionary<string, ChartMapDefinition> Definitions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> Atlases = new(StringComparer.Ordinal);
    private static ChartMapDefinition? _usStates;
    private static IReadOnlyList<ChartMapDefinition>? _all;

    public static IReadOnlyList<ChartMapDefinition> All() {
        if (_all != null) return _all;
        var maps = BuiltInRegionMaps.All;
        var definitions = new ChartMapDefinition[maps.Count];
        for (var i = 0; i < maps.Count; i++) definitions[i] = Get(maps[i]);
        return _all = Array.AsReadOnly(definitions);
    }

    public static bool TryGet(string id, out ChartMapDefinition definition) {
        if (BuiltInRegionMaps.TryFind(id, out var map)) {
            definition = Get(map);
            return true;
        }

        definition = null!;
        return false;
    }

    private static ChartMapDefinition Get(BuiltInRegionMap map) {
        lock (Gate) {
            if (Definitions.TryGetValue(map.Id, out var cached)) return cached;
            var definition = map.Atlas == null ? UsStates() : Load(map);
            Definitions[map.Id] = definition;
            return definition;
        }
    }

    // Geometry comes from simplified Natural Earth GeoJSON embedded as gzip; each atlas is read once and maps sharing it filter by group.
    private static ChartMapDefinition Load(BuiltInRegionMap map) {
        var options = new ChartMapGeoJsonOptions {
            CodePropertyNames = new[] { "code" },
            NamePropertyNames = new[] { "name" },
            AliasPropertyNames = new[] { "aliases" },
            IncludeFeatureIdAsAlias = false,
            Projection = map.Projection,
            LabelPropertyName = "label",
            CoordinatePrecision = map.CoordinatePrecision
        };
        if (map.Group != null) options.IncludeFeaturePropertyValues("group", map.Group);
        var definition = ChartMapGeoJson.ToMapDefinition(map.Id, map.Name, Atlas(map.Atlas!), options);
        if (!map.Window.HasValue) return definition;
        var window = map.Window.Value;
        var topLeft = ChartGeographicProjection.Project(window.MinimumLongitude, window.MaximumLatitude, map.Projection);
        var bottomRight = ChartGeographicProjection.Project(window.MaximumLongitude, window.MinimumLatitude, map.Projection);
        var bounds = new ChartRect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        return new ChartMapDefinition(map.Id, map.Name, bounds, definition.Regions).WithGeographicProjection(map.Projection);
    }

    private static string Atlas(string name) {
        if (Atlases.TryGetValue(name, out var text)) return text;
        var resource = "ChartForgeX.Maps.Data." + name + ".geojson.gz";
        using var stream = typeof(BuiltInMapDefinitions).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("Missing built-in map resource " + resource + ".");
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        text = reader.ReadToEnd();
        Atlases[name] = text;
        return text;
    }

    public static ChartMapDefinition UsStates() {
        if (_usStates != null) return _usStates;
        var regions = new List<ChartMapRegion>(UsStateGeoShapes.Shapes.Length);
        foreach (var shape in UsStateGeoShapes.Shapes) {
            var name = UsStateName(shape.Code);
            regions.Add(new ChartMapRegion(shape.Code, name, shape.Path, shape.Label, UsStateAliases(shape.Code, name)));
        }

        _usStates = new ChartMapDefinition("us-states", "United States states", UsStateGeoShapes.Bounds, regions)
            .WithGeographicProjection(ChartMapGeoJsonProjection.UnitedStatesAlbers);
        return _usStates;
    }

    internal static IEnumerable<string> UsStateAliases(string code, string name) {
        yield return name;
        yield return "US-" + code;
        if (code == "DC") {
            yield return "D.C.";
            yield return "Washington DC";
            yield return "Washington D.C.";
        }
    }

    internal static string UsStateName(string code) {
        return code switch {
            "AL" => "Alabama",
            "AK" => "Alaska",
            "AZ" => "Arizona",
            "AR" => "Arkansas",
            "CA" => "California",
            "CO" => "Colorado",
            "CT" => "Connecticut",
            "DE" => "Delaware",
            "DC" => "District of Columbia",
            "FL" => "Florida",
            "GA" => "Georgia",
            "HI" => "Hawaii",
            "ID" => "Idaho",
            "IL" => "Illinois",
            "IN" => "Indiana",
            "IA" => "Iowa",
            "KS" => "Kansas",
            "KY" => "Kentucky",
            "LA" => "Louisiana",
            "ME" => "Maine",
            "MD" => "Maryland",
            "MA" => "Massachusetts",
            "MI" => "Michigan",
            "MN" => "Minnesota",
            "MS" => "Mississippi",
            "MO" => "Missouri",
            "MT" => "Montana",
            "NE" => "Nebraska",
            "NV" => "Nevada",
            "NH" => "New Hampshire",
            "NJ" => "New Jersey",
            "NM" => "New Mexico",
            "NY" => "New York",
            "NC" => "North Carolina",
            "ND" => "North Dakota",
            "OH" => "Ohio",
            "OK" => "Oklahoma",
            "OR" => "Oregon",
            "PA" => "Pennsylvania",
            "RI" => "Rhode Island",
            "SC" => "South Carolina",
            "SD" => "South Dakota",
            "TN" => "Tennessee",
            "TX" => "Texas",
            "UT" => "Utah",
            "VT" => "Vermont",
            "VA" => "Virginia",
            "WA" => "Washington",
            "WV" => "West Virginia",
            "WI" => "Wisconsin",
            "WY" => "Wyoming",
            _ => code
        };
    }
}
