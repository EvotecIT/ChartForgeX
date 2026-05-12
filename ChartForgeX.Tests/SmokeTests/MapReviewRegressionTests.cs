using ChartForgeX.Core;
using ChartForgeX.Primitives;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GeoJsonMapDefinitionsSkipNullOptionalFeatureObjects() {
        const string geoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": null,
              "geometry": null
            },
            {
              "type": "Feature",
              "properties": { "NUTS_ID": "PL911", "NUTS_NAME": "Warszawski stoleczny" },
              "geometry": { "type": "Polygon", "coordinates": [[[20.5,52.0],[21.5,52.0],[21.5,52.5],[20.5,52.5],[20.5,52.0]]] }
            }
          ]
        }
        """;

        var definition = ChartMapDefinition.FromGeoJson("nuts-null", "NUTS null", geoJson);

        Assert(definition.Regions.Count == 1, "GeoJSON map definitions should treat null properties and geometry as absent optional feature objects.");
        Assert(definition.TryResolveRegion("PL911", out _), "GeoJSON map definitions should keep importing valid features after skipping null-geometry entries.");
    }

    private static void MapDefinitionsPreserveCanonicalCodesWhenAliasesCollide() {
        var laterAliasCollidesWithExistingCode = new ChartMapDefinition("canonical-alias", "Canonical Alias", 30, 10, new[] {
            new ChartMapRegion("A1", "Alpha", "M0 0L10 0L10 10L0 10Z"),
            new ChartMapRegion("B1", "A1", "M10 0L20 0L20 10L10 10Z")
        });
        var laterCodeCollidesWithExistingAlias = new ChartMapDefinition("alias-canonical", "Alias Canonical", 30, 10, new[] {
            new ChartMapRegion("A1", "B1", "M0 0L10 0L10 10L0 10Z"),
            new ChartMapRegion("B1", "Bravo", "M10 0L20 0L20 10L10 10Z")
        });

        Assert(laterAliasCollidesWithExistingCode.TryResolveRegion("A1", out var code) && code == "A1", "Map definitions should keep canonical codes resolvable when a later alias collides with them.");
        Assert(laterAliasCollidesWithExistingCode.TryResolveRegion("B1", out code) && code == "B1", "Map definitions should keep later canonical codes resolvable after alias collisions.");
        Assert(laterCodeCollidesWithExistingAlias.TryResolveRegion("B1", out code) && code == "B1", "Map definitions should let canonical codes override earlier non-canonical aliases.");
    }

    private static void PngMapRenderersRespectZeroRegionStrokeWidth() {
        var regionDefinition = new ChartMapDefinition("single-region", "Single Region", 10, 10, new[] {
            new ChartMapRegion("A", "Alpha", "M0 0L10 0L10 10L0 10Z")
        });
        var tileDefinition = new ChartTileMapDefinition("single-tile", "Single Tile", new[] {
            new ChartTileMapRegion("A", "Alpha", 0, 0)
        });
        var regions = new[] { new ChartRegionMapItem("A", 10, ChartColor.FromHex("#F97316")) };

        var strokedRegion = Chart.Create()
            .WithSize(220, 180)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithMapScaleLegend(false)
            .WithMapLabels(false)
            .WithMapRegionStroke(ChartColor.Black, 1)
            .AddRegionMap("Values", regionDefinition, regions);
        var zeroStrokeRegion = Chart.Create()
            .WithSize(220, 180)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithMapScaleLegend(false)
            .WithMapLabels(false)
            .WithMapRegionStroke(ChartColor.Black, 0)
            .AddRegionMap("Values", regionDefinition, regions);
        var strokedTile = Chart.Create()
            .WithSize(220, 180)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithMapScaleLegend(false)
            .WithMapLabels(false)
            .WithMapRegionStroke(ChartColor.Black, 1)
            .AddTileMap("Values", tileDefinition, regions);
        var zeroStrokeTile = Chart.Create()
            .WithSize(220, 180)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithMapScaleLegend(false)
            .WithMapLabels(false)
            .WithMapRegionStroke(ChartColor.Black, 0)
            .AddTileMap("Values", tileDefinition, regions);

        var strokedRegionPixels = ReadPngRgba(strokedRegion.ToPng(), out _, out _);
        var zeroStrokeRegionPixels = ReadPngRgba(zeroStrokeRegion.ToPng(), out _, out _);
        var strokedTilePixels = ReadPngRgba(strokedTile.ToPng(), out _, out _);
        var zeroStrokeTilePixels = ReadPngRgba(zeroStrokeTile.ToPng(), out _, out _);

        Assert(CountNearColor(strokedRegionPixels, 0, 0, 0, 0) > 0, "PNG region maps should draw configured non-zero region strokes.");
        Assert(CountNearColor(zeroStrokeRegionPixels, 0, 0, 0, 0) == 0, "PNG region maps should not draw outlines when map region stroke width is zero.");
        Assert(CountNearColor(strokedTilePixels, 0, 0, 0, 0) > 0, "PNG tile maps should draw configured non-zero region strokes.");
        Assert(CountNearColor(zeroStrokeTilePixels, 0, 0, 0, 0) == 0, "PNG tile maps should not draw outlines when map region stroke width is zero.");
    }

    private static void GeoJsonMapDefinitionsPreserveNumericIdentifierText() {
        const string geoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "GEOID": 9007199254740993, "NAME": "Large numeric ID" },
              "geometry": { "type": "Polygon", "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]] }
            }
          ]
        }
        """;

        var definition = ChartMapDefinition.FromGeoJson("numeric-id", "Numeric ID", geoJson, new ChartMapGeoJsonOptions {
            CodePropertyNames = new[] { "GEOID" },
            NamePropertyNames = new[] { "NAME" }
        });

        Assert(definition.TryResolveRegion("9007199254740993", out var code) && code == "9007199254740993", "GeoJSON map definitions should preserve large numeric identifier text without double rounding.");
    }

    private static void GeoJsonMapDefinitionsNormalizeNumericIdentifierText() {
        const string geoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "GEOID": 1.0, "NAME": "Decimal ID" },
              "geometry": { "type": "Polygon", "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]] }
            },
            {
              "type": "Feature",
              "properties": { "GEOID": 1e3, "NAME": "Exponent ID" },
              "geometry": { "type": "Polygon", "coordinates": [[[2,0],[3,0],[3,1],[2,1],[2,0]]] }
            }
          ]
        }
        """;

        var definition = ChartMapDefinition.FromGeoJson("numeric-normalized-id", "Numeric Normalized ID", geoJson, new ChartMapGeoJsonOptions {
            CodePropertyNames = new[] { "GEOID" },
            NamePropertyNames = new[] { "NAME" }
        });

        Assert(definition.TryResolveRegion("1", out var code) && code == "1", "GeoJSON map definitions should normalize decimal numeric identifier text for region lookup compatibility.");
        Assert(definition.TryResolveRegion("1000", out code) && code == "1000", "GeoJSON map definitions should normalize exponent numeric identifier text for region lookup compatibility.");
        Assert(!definition.TryResolveRegion("1.0", out _), "GeoJSON map definitions should not require callers to use raw decimal token text for region lookups.");
        Assert(!definition.TryResolveRegion("1e3", out _), "GeoJSON map definitions should not require callers to use raw exponent token text for region lookups.");
    }

    private static void GeoJsonMapDefinitionsAvoidGeneratedCodeCollisions() {
        const string geoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "NUTS_ID": "R1", "NUTS_NAME": "Explicit" },
              "geometry": { "type": "Polygon", "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]] }
            },
            {
              "type": "Feature",
              "properties": { "NUTS_NAME": "Generated" },
              "geometry": { "type": "Polygon", "coordinates": [[[2,0],[3,0],[3,1],[2,1],[2,0]]] }
            }
          ]
        }
        """;

        var definition = ChartMapDefinition.FromGeoJson("generated-codes", "Generated Codes", geoJson);

        Assert(definition.Regions.Count == 2, "GeoJSON map definitions should keep features whose generated fallback code would collide with explicit codes.");
        Assert(definition.TryResolveRegion("R1", out var explicitCode) && explicitCode == "R1", "GeoJSON map definitions should keep explicit region codes.");
        Assert(definition.Regions.Any(region => region.Code == "R2"), "GeoJSON map definitions should advance generated fallback codes until they are unique.");
    }
}
