using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

internal static class ChartMapExternalCatalog {
    private static IReadOnlyList<ChartMapCatalogEntry>? _entries;

    public static IReadOnlyList<ChartMapCatalogEntry> Entries() {
        return _entries ??= Array.AsReadOnly(new[] {
            Nuts("eu-nuts0-2021", "EU NUTS0 2021", "NUTS0", "NUTS_ID", "nuts0-20m-2021.geojson", false),
            Nuts("eu-nuts1-2021", "EU NUTS1 2021", "NUTS1", "NUTS_ID", "nuts1-20m-2021.geojson", false),
            Nuts("eu-nuts2-2021", "EU NUTS2 2021", "NUTS2", "NUTS_ID", "nuts2-20m-2021.geojson", true),
            Nuts("eu-nuts3-2021", "EU NUTS3 2021", "NUTS3", "NUTS_ID", "nuts3-20m-2021.geojson", true),
            new ChartMapCatalogEntry(
                "us-counties-2024",
                "United States counties 2024",
                "US",
                "county",
                "US Census Cartographic Boundary Files",
                "GEOID",
                "https://www.census.gov/geographies/mapping-files/time-series/geo/cartographic-boundary.html",
                "us-counties-2024.geojson",
                () => new ChartMapGeoJsonOptions {
                    CodePropertyNames = new[] { "GEOID", "geoid", "GEOIDFQ", "AFFGEOID" },
                    NamePropertyNames = new[] { "NAME", "NAMELSAD", "name" }
                },
                isLarge: true,
                notes: "Recommended as a repository-hosted or user-supplied GeoJSON converted from the official Census shapefile."),
            new ChartMapCatalogEntry(
                "world-admin0-natural-earth-50m",
                "World countries Natural Earth 50m",
                "World",
                "admin0",
                "Natural Earth",
                "ADM0_A3",
                "https://www.naturalearthdata.com/downloads/50m-cultural-vectors/",
                "world-admin0-natural-earth-50m.geojson",
                () => new ChartMapGeoJsonOptions {
                    CodePropertyNames = new[] { "ADM0_A3", "ISO_A3", "NAME" },
                    NamePropertyNames = new[] { "NAME", "NAME_EN", "ADMIN" }
                },
                isLarge: false,
                notes: "Useful for global country maps and pale context layers."),
            new ChartMapCatalogEntry(
                "global-adm1-geoboundaries",
                "Global administrative level 1",
                "World",
                "ADM1",
                "geoBoundaries",
                "shapeID",
                "https://www.geoboundaries.org/globalDownloads.html",
                "global-adm1-geoboundaries.geojson",
                () => new ChartMapGeoJsonOptions {
                    CodePropertyNames = new[] { "shapeID", "shapeISO", "shapeGroup", "id" },
                    NamePropertyNames = new[] { "shapeName", "name", "NAME" }
                },
                isLarge: true,
                notes: "Global subnational data is valuable but large; keep it external or split by country/continent.")
        });
    }

    private static ChartMapCatalogEntry Nuts(string id, string name, string level, string joinKey, string fileName, bool isLarge) {
        return new ChartMapCatalogEntry(
            id,
            name,
            "EU",
            level,
            "Eurostat/GISCO NUTS 2021",
            joinKey,
            "https://gisco-services.ec.europa.eu/distribution/v1/nuts-2021.html",
            fileName,
            () => new ChartMapGeoJsonOptions {
                CodePropertyNames = new[] { "NUTS_ID", "id" },
                NamePropertyNames = new[] { "NUTS_NAME", "NAME_LATN", "name" },
                AliasPropertyNames = new[] { "CNTR_CODE" }
            },
            isLarge: isLarge,
            notes: "Official EU statistical regions. Join metric data by NUTS_ID.");
    }
}
