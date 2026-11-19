using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Describes one built-in region map: where its geometry lives, how it is projected and where it came from.</summary>
internal sealed class BuiltInRegionMap {
    internal BuiltInRegionMap(string id, string name, string geography, string level, string joinKey, string? atlas,
        ChartMapGeoJsonProjection projection, string source, string sourceUrl, string license, string notes,
        string? group = null, ChartMapViewport? window = null, int coordinatePrecision = 4) {
        Id = id;
        Name = name;
        Geography = geography;
        Level = level;
        JoinKey = joinKey;
        Atlas = atlas;
        Projection = projection;
        Source = source;
        SourceUrl = sourceUrl;
        License = license;
        Notes = notes;
        Group = group;
        Window = window;
        CoordinatePrecision = coordinatePrecision;
    }

    internal string Id { get; }
    internal string Name { get; }
    internal string Geography { get; }
    internal string Level { get; }
    internal string JoinKey { get; }
    internal string? Atlas { get; }
    internal ChartMapGeoJsonProjection Projection { get; }
    internal string Source { get; }
    internal string SourceUrl { get; }
    internal string License { get; }
    internal string Notes { get; }
    internal string? Group { get; }
    internal ChartMapViewport? Window { get; }
    internal int CoordinatePrecision { get; }

    internal ChartMapCatalogEntry ToEntry() => new(Id, Name, Geography, Level, Source, JoinKey, SourceUrl, "embedded",
        CreateOptions, isEmbedded: true, notes: Notes, license: License);

    /// <summary>Creates the import options of this map: its property names, projection, label points and continent group.</summary>
    internal ChartMapGeoJsonOptions CreateOptions() {
        var options = new ChartMapGeoJsonOptions {
            CodePropertyNames = new[] { "code" },
            NamePropertyNames = new[] { "name" },
            AliasPropertyNames = new[] { "aliases" },
            IncludeFeatureIdAsAlias = false,
            Projection = Projection,
            LabelPropertyName = "label",
            CoordinatePrecision = CoordinatePrecision
        };
        if (Group != null) options.IncludeFeaturePropertyValues("group", Group);
        return options;
    }
}

internal static class BuiltInRegionMaps {
    private const string NaturalEarth = "Natural Earth 5.1.2";
    private const string NaturalEarthCountriesUrl = "https://www.naturalearthdata.com/downloads/10m-cultural-vectors/";
    private const string WorldEdition = "Natural Earth 1:10m countries, German point-of-view edition (internationally recognised borders, for example Crimea in Ukraine)";
    private const string NaturalEarthProvincesUrl = "https://www.naturalearthdata.com/downloads/10m-cultural-vectors/";
    private const string PublicDomain = "Public domain (Natural Earth terms of use)";
    private const string Countries = "ISO 3166-1 alpha-2 code, alpha-3 code or country name";
    private const string Subdivisions = "ISO 3166-2 code, the code without its country prefix, or region name";

    private static IReadOnlyList<BuiltInRegionMap>? _all;

    internal static IReadOnlyList<BuiltInRegionMap> All => _all ??= Array.AsReadOnly(Create());

    internal static bool TryFind(string id, out BuiltInRegionMap map) {
        if (!string.IsNullOrWhiteSpace(id)) {
            foreach (var candidate in All) {
                if (string.Equals(candidate.Id, id.Trim(), StringComparison.OrdinalIgnoreCase)) {
                    map = candidate;
                    return true;
                }
            }
        }

        map = null!;
        return false;
    }

    private static BuiltInRegionMap[] Create() => new[] {
        World("world", "World countries", ChartMapGeoJsonProjection.Miller),
        World("world-mercator", "World countries (Mercator)", ChartMapGeoJsonProjection.WebMercator),
        new BuiltInRegionMap("us-states", "United States states", "US", "state", "state abbreviation, ISO 3166-2 code or state name", null,
            ChartMapGeoJsonProjection.UnitedStatesAlbers, "U.S. Census Bureau state boundaries via us-atlas states-albers-10m",
            "https://github.com/topojson/us-atlas", "Public domain (U.S. Census Bureau cartographic boundary files)",
            "Albers equal-area composite with Alaska and Hawaii insets."),
        UnitedStates("us-states-lambert", "United States states (Lambert)", ChartMapGeoJsonProjection.UnitedStatesLambert),
        UnitedStates("us-states-mercator", "United States states (Mercator)", ChartMapGeoJsonProjection.UnitedStatesMercator),
        UnitedStates("us-states-miller", "United States states (Miller)", ChartMapGeoJsonProjection.UnitedStatesMiller),
        new BuiltInRegionMap("continents", "Continents", "World", "continent", "continent code (AF, AS, EU, NA, OC, SA) or name", "world-continents",
            ChartMapGeoJsonProjection.Miller, NaturalEarth, NaturalEarthCountriesUrl, PublicDomain,
            WorldEdition + ", dissolved by continent; Antarctica is omitted."),
        Continent("africa", "Africa", "AF"),
        Continent("asia", "Asia", "AS"),
        Continent("europe", "Europe", "EU", new ChartMapViewport("Europe", -25, 45, 34, 72)),
        Continent("north-america", "North America", "NA", new ChartMapViewport("North America", -172, -12, 7, 84)),
        new BuiltInRegionMap("oceania", "Oceania countries", "Oceania", "admin0", Countries, "oceania-countries",
            ChartMapGeoJsonProjection.Miller, NaturalEarth, NaturalEarthCountriesUrl, PublicDomain,
            WorldEdition + ". Islands east of the antimeridian use longitudes above 180 so the region stays continuous; markers there are not placed."),
        Continent("south-america", "South America", "SA"),
        Country("spain", "Spain provinces", "ES", "province"),
        Country("russia", "Russia federal subjects", "RU", "federal subject",
            "Natural Earth 1:10m admin-1 subdivisions without Crimea and Sevastopol. Chukotka east of the antimeridian uses longitudes above 180 so the region stays continuous; markers there are not placed."),
        Country("canada", "Canada provinces and territories", "CA", "province"),
        Country("iraq", "Iraq governorates", "IQ", "governorate"),
        Country("brazil", "Brazil states", "BR", "state"),
        Country("france", "France regions", "FR", "region", "The 13 metropolitan regions, dissolved from Natural Earth departments; overseas regions are omitted."),
        Country("germany", "Germany states", "DE", "state"),
        Country("belgium", "Belgium provinces", "BE", "province", "The ten provinces and the Brussels-Capital Region."),
        Country("netherlands", "Netherlands provinces", "NL", "province", "The twelve European provinces; the Caribbean Netherlands are omitted."),
        new BuiltInRegionMap("united-kingdom", "United Kingdom countries", "GB", "country", "ISO 3166-2 code (GB-ENG, GB-SCT, GB-WLS, GB-NIR) or name",
            "united-kingdom", ChartMapGeoJsonProjection.Miller, NaturalEarth, NaturalEarthProvincesUrl, PublicDomain,
            "Natural Earth 1:10m map units for England, Scotland, Wales and Northern Ireland.")
    };

    private static BuiltInRegionMap World(string id, string name, ChartMapGeoJsonProjection projection) =>
        new(id, name, "World", "admin0", Countries, "world-countries", projection, NaturalEarth, NaturalEarthCountriesUrl, PublicDomain,
            WorldEdition + ", simplified; Antarctica is omitted. Dependencies without their own ISO code use their Natural Earth A3 code.");

    private static BuiltInRegionMap UnitedStates(string id, string name, ChartMapGeoJsonProjection projection) =>
        new(id, name, "US", "state", "state abbreviation, ISO 3166-2 code or state name", "us-states", projection, NaturalEarth, NaturalEarthProvincesUrl,
            PublicDomain, "Natural Earth 1:10m states with Alaska and Hawaii insets.");

    private static BuiltInRegionMap Continent(string id, string name, string group, ChartMapViewport? window = null) =>
        new(id, name + " countries", name, "admin0", Countries, "world-countries", ChartMapGeoJsonProjection.Miller, NaturalEarth, NaturalEarthCountriesUrl,
            PublicDomain, WorldEdition + ", assigned to " + name + "." + (window.HasValue ? " The frame is cropped to the continent; overseas parts are clipped." : string.Empty),
            group, window);

    private static BuiltInRegionMap Country(string id, string name, string iso, string level, string? notes = null) =>
        new(id, name, iso, level, Subdivisions, id, ChartMapGeoJsonProjection.Miller, NaturalEarth, NaturalEarthProvincesUrl, PublicDomain,
            notes ?? "Natural Earth 1:10m admin-1 subdivisions, simplified.");
}
