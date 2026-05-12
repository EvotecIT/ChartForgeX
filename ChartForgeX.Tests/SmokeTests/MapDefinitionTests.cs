using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RegionMapRendersReusableMapDefinition() {
        var definition = ChartMapCatalog.Get("us-states");
        var chart = Chart.Create()
            .WithSize(760, 420)
            .WithTitle("Revenue by region")
            .WithMapLabels(false)
            .AddRegionMap("Revenue", definition, new[] {
                new ChartRegionMapItem("California", 95, ChartColor.FromHex("#2563EB")),
                new ChartRegionMapItem("NY", 82),
                new ChartRegionMapItem("TX", 74),
                new ChartRegionMapItem("Washington D.C.", 12)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"region-map\"", StringComparison.Ordinal), "Region maps should expose a role marker.");
        Assert(svg.Contains("data-cfx-role=\"region-map\" data-cfx-map-kind=\"us-states\" data-cfx-label=\"Revenue\" data-cfx-region-count=\"51\" data-cfx-filled-region-count=\"4\" data-cfx-missing-region-count=\"47\"", StringComparison.Ordinal), "Region maps should expose reusable map definition metadata.");
        Assert(svg.Contains("data-cfx-map-id=\"us-states\"", StringComparison.Ordinal), "Region maps should expose the source map definition ID.");
        Assert(svg.Contains("data-cfx-min-value=\"12\" data-cfx-max-value=\"95\"", StringComparison.Ordinal), "Region map containers should expose the source value range.");
        Assert(svg.Contains("Revenue by region region map for Revenue on United States states with 4 filled regions and 47 missing regions.", StringComparison.Ordinal), "Region map SVG descriptions should summarize the reusable map definition.");
        Assert(svg.Contains("role=\"group\" aria-label=\"Revenue region map with 4 filled regions and 47 missing regions\"", StringComparison.Ordinal), "Region map containers should expose a useful group label.");
        Assert(!svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "Region maps should not emit generic series legends.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"region-map-region\"") == 51, "Region maps should render every region in the definition.");
        Assert(svg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"95\"", StringComparison.Ordinal), "Region maps should resolve full names through the map definition.");
        Assert(svg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"12\"", StringComparison.Ordinal), "Region maps should resolve custom aliases through the map definition.");
        Assert(svg.Contains("<title>California (CA): 95</title>", StringComparison.Ordinal), "Region map regions should expose native SVG hover titles.");
        Assert(svg.Contains("class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"region-map-region\"", StringComparison.Ordinal), "Region map regions should be keyboard-focusable interactive SVG regions.");
        Assert(svg.Contains("data-cfx-role=\"region-map-scale-step\"", StringComparison.Ordinal), "Region maps should render a value scale.");
        Assert(svg.Contains("data-cfx-role=\"region-map-scale-no-data\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "Region maps should tag missing-data scale swatches as empty.");
        Assert(svg.Contains("fill=\"#2E69EC\"", StringComparison.Ordinal), "Region maps should honor per-region colors inside the choropleth scale.");
        Assert(chart.ToPng().Length > 64, "Region maps should render PNG output.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().AddRegionMap("Bad", null!, new[] { new ChartRegionMapItem("CA", 1) }), "Region maps should reject missing definitions.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRegionMap("Empty", definition, Array.Empty<ChartRegionMapItem>()), "Region maps should reject empty inputs.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRegionMap("Bad", definition, new[] { new ChartRegionMapItem("ZZ", 1) }), "Region maps should reject regions outside the map definition.");
    }

    private static void TileMapRendersReusableTileMapDefinition() {
        var definition = ChartTileMapCatalog.Get("us-states");
        var chart = Chart.Create()
            .WithSize(760, 420)
            .WithTitle("Revenue by tile region")
            .AddTileMap("Revenue", definition, new[] {
                new ChartRegionMapItem("CA", 95, ChartColor.FromHex("#2563EB")),
                new ChartRegionMapItem("NY", 82),
                new ChartRegionMapItem("TX", 74),
                new ChartRegionMapItem("FL", 61),
                new ChartRegionMapItem("WA", 55),
                new ChartRegionMapItem("California", 5)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"tile-map\"", StringComparison.Ordinal), "Tile maps should expose a role marker.");
        Assert(svg.Contains("data-cfx-role=\"tile-map\" data-cfx-map-kind=\"us-states\" data-cfx-map-id=\"us-states\" data-cfx-label=\"Revenue\" data-cfx-region-count=\"51\" data-cfx-filled-region-count=\"5\" data-cfx-missing-region-count=\"46\"", StringComparison.Ordinal), "Tile maps should expose reusable map definition metadata.");
        Assert(svg.Contains("data-cfx-map-id=\"us-states\"", StringComparison.Ordinal), "Tile maps should expose the source tile-map definition ID.");
        Assert(svg.Contains("data-cfx-min-value=\"55\" data-cfx-max-value=\"100\"", StringComparison.Ordinal), "Tile map containers should expose the source value range.");
        Assert(svg.Contains("Revenue by tile region tile map for Revenue on United States states with 5 filled regions and 46 missing regions.", StringComparison.Ordinal), "Tile map SVG descriptions should summarize the reusable tile definition.");
        Assert(svg.Contains("role=\"group\" aria-label=\"Revenue tile map with 5 filled regions and 46 missing regions\"", StringComparison.Ordinal), "Tile map containers should expose a useful group label.");
        Assert(!svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "Tile maps should not emit generic series legends.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"tile-map-region\"") == 51, "Tile maps should render every region in the definition.");
        Assert(svg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"100\"", StringComparison.Ordinal), "Tile maps should resolve aliases and aggregate duplicate values.");
        Assert(svg.Contains("<title>California (CA): 100</title>", StringComparison.Ordinal), "Tile map regions should expose native SVG hover titles.");
        Assert(svg.Contains("class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"tile-map-region\"", StringComparison.Ordinal), "Tile map regions should be keyboard-focusable interactive SVG regions.");
        Assert(svg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"0\" data-cfx-empty=\"true\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "Tile maps should expose empty status metadata for missing regions.");
        Assert(svg.Contains("data-cfx-role=\"tile-map-label\"", StringComparison.Ordinal), "Tile maps should label tiles when there is enough room.");
        Assert(svg.Contains("data-cfx-role=\"tile-map-scale-step\"", StringComparison.Ordinal), "Tile maps should render a value scale.");
        Assert(svg.Contains("data-cfx-role=\"tile-map-scale-no-data\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "Tile maps should tag missing-data scale swatches as empty.");
        Assert(svg.Contains("fill=\"#2563EB\"", StringComparison.Ordinal), "Tile maps should honor per-region colors.");
        Assert(chart.ToPng().Length > 64, "Tile maps should render PNG output.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().AddTileMap("Bad", null!, new[] { new ChartRegionMapItem("CA", 1) }), "Tile maps should reject missing definitions.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTileMap("Empty", definition, Array.Empty<ChartRegionMapItem>()), "Tile maps should reject empty inputs.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTileMap("Bad", definition, new[] { new ChartRegionMapItem("ZZ", 1) }), "Tile maps should reject regions outside the tile-map definition.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRegionMapItem("CA", -1), "Region map values should reject negatives.");
    }

    private static void MapColorScaleRendersCustomChoroplethColors() {
        var scale = ChartMapColorScale
            .Diverging(ChartColor.FromHex("#F97316"), ChartColor.FromHex("#FFF7ED"), ChartColor.FromHex("#065F46"), 50)
            .WithValueRange(0, 100)
            .WithLabels("0", "50 median", ">100")
            .WithNoDataColor(ChartColor.FromHex("#E5E7EB"));

        var regions = new[] {
            new ChartRegionMapItem("CA", 0),
            new ChartRegionMapItem("NY", 50),
            new ChartRegionMapItem("TX", 100)
        };

        var region = Chart.Create()
            .WithSize(760, 420)
            .WithMapLabels(false)
            .WithMapColorScale(scale)
            .AddRegionMap("Births", ChartMapCatalog.Get("us-states"), regions);
        var tile = Chart.Create()
            .WithSize(760, 420)
            .WithMapColorScale(scale)
            .AddTileMap("Births", ChartTileMapCatalog.Get("us-states"), regions);

        var regionSvg = region.ToSvg();
        var tileSvg = tile.ToSvg();
        Assert(regionSvg.Contains("data-cfx-map-color-scale=\"custom\"", StringComparison.Ordinal), "Region maps should expose that a custom color scale is active.");
        Assert(tileSvg.Contains("data-cfx-map-color-scale=\"custom\"", StringComparison.Ordinal), "Tile maps should expose that a custom color scale is active.");
        Assert(regionSvg.Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Region maps should render the low color from a diverging map scale.");
        Assert(regionSvg.Contains("fill=\"#FFF7ED\"", StringComparison.Ordinal), "Region maps should render the midpoint color from a diverging map scale.");
        Assert(regionSvg.Contains("fill=\"#065F46\"", StringComparison.Ordinal), "Region maps should render the high color from a diverging map scale.");
        Assert(tileSvg.Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Tile maps should render the low color from a diverging map scale.");
        Assert(tileSvg.Contains("fill=\"#FFF7ED\"", StringComparison.Ordinal), "Tile maps should render the midpoint color from a diverging map scale.");
        Assert(tileSvg.Contains("fill=\"#065F46\"", StringComparison.Ordinal), "Tile maps should render the high color from a diverging map scale.");
        Assert(regionSvg.Contains("data-cfx-role=\"region-map-scale-midpoint-label\" data-cfx-value=\"50\"", StringComparison.Ordinal), "Region maps should expose custom midpoint legend labels.");
        Assert(tileSvg.Contains("data-cfx-role=\"tile-map-scale-midpoint-label\" data-cfx-value=\"50\"", StringComparison.Ordinal), "Tile maps should expose custom midpoint legend labels.");
        Assert(regionSvg.Contains("data-cfx-role=\"region-map-scale-step\" data-cfx-value=\"100\" data-cfx-status=\"positive\"", StringComparison.Ordinal), "Custom map color scales should classify high range values against the configured scale.");
        Assert(tileSvg.Contains("data-cfx-role=\"tile-map-scale-step\" data-cfx-value=\"100\" data-cfx-status=\"positive\"", StringComparison.Ordinal), "Custom tile-map color scales should classify high range values against the configured scale.");
        Assert(regionSvg.Contains(">0</text>", StringComparison.Ordinal), "Region map scale legends should honor custom low labels.");
        Assert(regionSvg.Contains("&gt;100</text>", StringComparison.Ordinal), "Region map scale legends should escape and honor custom high labels.");
        Assert(regionSvg.Contains("data-cfx-role=\"region-map-scale-no-data\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "Region maps should still reserve missing-data swatches with custom scales.");
        Assert(regionSvg.Contains("fill=\"#E5E7EB\"", StringComparison.Ordinal), "Region maps should honor custom no-data colors.");
        Assert(region.ToPng().Length > 64, "Custom color-scaled region maps should render PNG output.");
        Assert(tile.ToPng().Length > 64, "Custom color-scaled tile maps should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartMapColorScale.Sequential(ChartColor.White, ChartColor.Black).WithValueRange(10, 10), "Map color scale ranges should reject equal bounds.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartMapColorScale.Sequential(ChartColor.White, ChartColor.Black).WithMidpoint(double.NaN), "Map color scale midpoint values should reject non-finite values.");
    }

    private static void RegionHeatmapColorsEveryCustomRegionByValue() {
        var definition = new ChartMapDefinition("districts", "Districts", 20, 20, new[] {
            new ChartMapRegion("A", "Alpha", "M0 0L10 0L10 10L0 10Z"),
            new ChartMapRegion("B", "Beta", "M10 0L20 0L20 10L10 10Z"),
            new ChartMapRegion("C", "Gamma", "M0 10L10 10L10 20L0 20Z"),
            new ChartMapRegion("D", "Delta", "M10 10L20 10L20 20L10 20Z")
        });
        var scale = ChartMapColorScale
            .Diverging(ChartColor.FromHex("#F97316"), ChartColor.FromHex("#FFF7ED"), ChartColor.FromHex("#065F46"), 5)
            .WithValueRange(0, 10);

        var chart = Chart.Create()
            .WithSize(360, 260)
            .WithMapLabels(false)
            .AddRegionHeatmap("Births", definition, new[] {
                new ChartRegionMapItem("A", 0),
                new ChartRegionMapItem("B", 2.5),
                new ChartRegionMapItem("C", 7.5),
                new ChartRegionMapItem("D", 10)
            }, scale);

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-label=\"Births\"", StringComparison.Ordinal), "Region heatmaps should render through the map series surface.");
        Assert(svg.Contains("data-cfx-map-color-scale=\"custom\"", StringComparison.Ordinal), "Region heatmaps should activate their provided color scale.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"region-map-region\"") == 4, "Region heatmaps should render every custom region polygon.");
        Assert(svg.Contains("data-cfx-region=\"A\" data-cfx-region-name=\"Alpha\" data-cfx-value=\"0\"", StringComparison.Ordinal), "Region heatmaps should preserve each polygon's own value.");
        Assert(svg.Contains("data-cfx-region=\"D\" data-cfx-region-name=\"Delta\" data-cfx-value=\"10\"", StringComparison.Ordinal), "Region heatmaps should preserve the high polygon's own value.");
        Assert(svg.Contains("data-cfx-region=\"A\"", StringComparison.Ordinal) && svg.Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Region heatmaps should color low-valued polygons with the low scale color.");
        Assert(svg.Contains("data-cfx-region=\"D\"", StringComparison.Ordinal) && svg.Contains("fill=\"#065F46\"", StringComparison.Ordinal), "Region heatmaps should color high-valued polygons with the high scale color.");
        Assert(chart.ToPng().Length > 64, "Region heatmaps should render PNG output.");
    }

    private static void MapHeatmapsCanUseReportStyleRightScale() {
        var scale = ChartMapColorScale
            .Diverging(ChartColor.FromHex("#F97316"), ChartColor.FromHex("#FFF7ED"), ChartColor.FromHex("#065F46"), 50)
            .WithValueRange(0, 100)
            .WithLabels("0", "50 median", ">100")
            .WithNoDataColor(ChartColor.FromHex("#E5E7EB"));

        var reportDefinition = new ChartMapDefinition("report-regions", "Report Regions", new ChartRect(-12, -72, 47, 38), new[] {
            new ChartMapRegion("A", "Alpha", "M-10 -70L0 -70L0 -60L-10 -60Z"),
            new ChartMapRegion("B", "Beta", "M5 -62L15 -62L15 -50L5 -50Z"),
            new ChartMapRegion("C", "Gamma", "M18 -55L32 -55L32 -40L18 -40Z")
        });
        var contextDefinition = new ChartMapDefinition("context", "Context", new ChartRect(-12, -72, 47, 38), new[] {
            new ChartMapRegion("BG", "Background", "M-12 -72L35 -72L35 -34L-12 -34Z")
        });
        var region = Chart.Create()
            .WithSize(760, 420)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithMapLabels(false)
            .WithMapRegionStroke(ChartColor.FromRgba(255, 255, 255, 160), 0.42)
            .WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right)
            .WithRegionMapCoordinateBounds(-12, 35, 34, 72)
            .AddMapBaseLayer(contextDefinition, ChartColor.FromHex("#E5E7EB"), ChartColor.FromHex("#D4D4D8"), 0.8)
            .AddRegionHeatmap("Births", reportDefinition, new[] {
                new ChartRegionMapItem("A", 0),
                new ChartRegionMapItem("B", 50),
                new ChartRegionMapItem("C", 100)
            }, scale)
            .AddMapBoundaryLayer(contextDefinition, ChartColor.FromHex("#111827"), 1.4);
        var tile = Chart.Create()
            .WithSize(760, 420)
            .WithCard(false)
            .WithPlotBackground(false)
            .WithMapSurface(false)
            .WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right)
            .AddTileHeatmap("Births", ChartTileMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 0),
                new ChartRegionMapItem("NY", 50),
                new ChartRegionMapItem("TX", 100)
            }, scale);

        var regionSvg = region.ToSvg();
        var tileSvg = tile.ToSvg();
        Assert(regionSvg.Contains("data-cfx-role=\"region-map-scale-border\"", StringComparison.Ordinal), "Region heatmaps should support right-side scale legends for report maps.");
        Assert(regionSvg.Contains("data-cfx-role=\"region-map-scale-midpoint-label\" data-cfx-value=\"50\"", StringComparison.Ordinal), "Right-side region scale legends should expose midpoint labels.");
        Assert(!regionSvg.Contains("data-cfx-role=\"region-map-surface\"", StringComparison.Ordinal), "Report-style region maps should be able to suppress the map surface.");
        Assert(regionSvg.Contains("data-cfx-role=\"map-base-layer\"", StringComparison.Ordinal), "Report-style region maps should render context base geography behind data regions.");
        Assert(regionSvg.Contains("data-cfx-role=\"map-boundary-layer\"", StringComparison.Ordinal), "Report-style region maps should render boundary overlays above data regions.");
        Assert(regionSvg.Contains("stroke-width=\"0.42\"", StringComparison.Ordinal), "Report-style region maps should allow quiet internal region strokes.");
        Assert(regionSvg.Contains("data-cfx-source-left=\"-12\" data-cfx-source-top=\"-72\" data-cfx-source-width=\"47\" data-cfx-source-height=\"38\"", StringComparison.Ordinal), "Report-style region maps should expose explicit coordinate framing metadata.");
        Assert(regionSvg.IndexOf("data-cfx-role=\"map-base-layer\"", StringComparison.Ordinal) < regionSvg.IndexOf("data-cfx-role=\"region-map-region\"", StringComparison.Ordinal) && regionSvg.IndexOf("data-cfx-role=\"region-map-region\"", StringComparison.Ordinal) < regionSvg.IndexOf("data-cfx-role=\"map-boundary-layer\"", StringComparison.Ordinal), "Cartographic region-map layers should render base geography, data regions, then boundary overlays.");
        Assert(tileSvg.Contains("data-cfx-role=\"tile-map-scale-border\"", StringComparison.Ordinal), "Tile heatmaps should support right-side scale legends for report maps.");
        Assert(!tileSvg.Contains("data-cfx-role=\"tile-map-surface\"", StringComparison.Ordinal), "Report-style tile maps should be able to suppress the map surface.");
        Assert(region.ToPng().Length > 64, "Report-style region heatmaps should render PNG output.");
        Assert(tile.ToPng().Length > 64, "Report-style tile heatmaps should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithRegionMapBounds(new ChartRect(double.NaN, 0, 10, 10)), "Region map bounds should reject non-finite left values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithRegionMapBounds(new ChartRect(0, 0, double.PositiveInfinity, 10)), "Region map bounds should reject non-finite dimensions.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithRegionMapCoordinateBounds(35, -12, 34, 72), "Region map coordinate bounds should reject inverted longitudes.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithRegionMapCoordinateBounds(-12, 35, 72, 34), "Region map coordinate bounds should reject inverted latitudes.");
    }

    private static void GeoJsonMapDefinitionsImportPolygonFeaturesForRegionHeatmaps() {
        const string geoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "id": "PL911",
              "properties": { "NUTS_ID": "PL911", "NUTS_NAME": "Warszawski stoleczny", "CNTR_CODE": "PL" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[20.5,52.0],[21.5,52.0],[21.5,52.5],[20.5,52.5],[20.5,52.0]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "NUTS_ID": "DE300", "NUTS_NAME": "Berlin" },
              "geometry": {
                "type": "MultiPolygon",
                "coordinates": [[[[13.0,52.2],[13.8,52.2],[13.8,52.8],[13.0,52.8],[13.0,52.2]]]]
              }
            }
          ]
        }
        """;
        var definition = ChartMapDefinition.FromGeoJson("nuts-mini", "NUTS mini", geoJson, new ChartMapGeoJsonOptions {
            AliasPropertyNames = new[] { "CNTR_CODE" }
        });
        Assert(definition.Id == "nuts-mini", "GeoJSON map definitions should preserve caller IDs.");
        Assert(definition.Regions.Count == 2, "GeoJSON map definitions should import polygon and multipolygon features.");
        Assert(definition.TryResolveRegion("PL911", out var plCode) && plCode == "PL911", "GeoJSON map definitions should resolve NUTS_ID codes.");
        Assert(definition.TryResolveRegion("Warszawski stoleczny", out plCode) && plCode == "PL911", "GeoJSON map definitions should resolve imported region names.");
        Assert(definition.TryResolveRegion("PL", out plCode) && plCode == "PL911", "GeoJSON map definitions should resolve configured alias fields.");
        Assert(definition.Bounds.Width > 0 && definition.Bounds.Height > 0, "GeoJSON map definitions should derive valid path bounds.");

        var scale = ChartMapColorScale.Sequential(ChartColor.FromHex("#F97316"), ChartColor.FromHex("#065F46")).WithValueRange(0, 10);
        var svg = Chart.Create()
            .WithSize(420, 260)
            .WithMapLabels(false)
            .AddRegionHeatmap("Births", definition, new[] {
                new ChartRegionMapItem("PL911", 0),
                new ChartRegionMapItem("DE300", 10)
            }, scale)
            .ToSvg();
        Assert(svg.Contains("data-cfx-map-id=\"nuts-mini\"", StringComparison.Ordinal), "GeoJSON map definitions should render through the region heatmap surface.");
        Assert(svg.Contains("data-cfx-region=\"PL911\"", StringComparison.Ordinal), "GeoJSON region heatmaps should preserve imported region codes.");
        Assert(svg.Contains("data-cfx-region=\"DE300\"", StringComparison.Ordinal), "GeoJSON multipolygon regions should preserve imported region codes.");
        Assert(svg.Contains("fill=\"#F97316\"", StringComparison.Ordinal) && svg.Contains("fill=\"#065F46\"", StringComparison.Ordinal), "GeoJSON region heatmaps should color imported polygons from data values.");

        var filtered = ChartMapDefinition.FromGeoJson("nuts-filtered", "NUTS filtered", geoJson, new ChartMapGeoJsonOptions {
            AliasPropertyNames = new[] { "CNTR_CODE" }
        }.IncludeFeaturePropertyValues("CNTR_CODE", "PL"));
        Assert(filtered.Regions.Count == 1 && filtered.TryResolveRegion("PL911", out _), "GeoJSON map definitions should support property-value filters for metric-layer scopes.");
    }

    private static void GeoJsonMapDefinitionsCanLimitImportedCoordinateBounds() {
        const string geoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "NUTS_ID": "PL911", "NUTS_NAME": "Warszawski stoleczny" },
              "geometry": { "type": "Polygon", "coordinates": [[[20.5,52.0],[21.5,52.0],[21.5,52.5],[20.5,52.5],[20.5,52.0]]] }
            },
            {
              "type": "Feature",
              "properties": { "NUTS_ID": "FRY10", "NUTS_NAME": "Guadeloupe" },
              "geometry": { "type": "Polygon", "coordinates": [[[-62.0,15.5],[-61.0,15.5],[-61.0,16.5],[-62.0,16.5],[-62.0,15.5]]] }
            },
            {
              "type": "Feature",
              "properties": { "NUTS_ID": "CZ010", "NUTS_NAME": "Praha" },
              "geometry": { "type": "Polygon", "coordinates": [[[14.0,49.8],[15.0,49.8],[15.0,50.3],[14.0,50.3],[14.0,49.8]]] }
            }
          ]
        }
        """;

        var definition = ChartMapDefinition.FromGeoJson("nuts-bounded", "NUTS bounded", geoJson, new ChartMapGeoJsonOptions()
            .WithCoordinateBounds(10, 30, 49, 60));

        Assert(definition.Regions.Count == 2, "GeoJSON coordinate bounds should keep features intersecting the requested longitude and latitude window.");
        Assert(definition.TryResolveRegion("PL911", out _), "GeoJSON coordinate bounds should keep matching regions.");
        Assert(definition.TryResolveRegion("CZ010", out _), "GeoJSON coordinate bounds should include matching edge regions.");
        Assert(!definition.TryResolveRegion("FRY10", out _), "GeoJSON coordinate bounds should omit far-away regions so imported map bounds remain focused.");
        Assert(definition.Bounds.Left > 10 && definition.Bounds.Right < 30, "Bounded GeoJSON definitions should derive map bounds from kept regions only.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartMapDefinition.FromGeoJson("bad", "Bad", geoJson, new ChartMapGeoJsonOptions().WithCoordinateBounds(30, 10, 49, 60)), "GeoJSON coordinate bounds should validate ordered longitudes.");
    }

    private static void MapDefinitionsIgnoreAmbiguousAliases() {
        var definition = new ChartMapDefinition("ambiguous", "Ambiguous", 20, 10, new[] {
            new ChartMapRegion("A1", "Jura", "M0 0L10 0L10 10L0 10Z"),
            new ChartMapRegion("B1", "Jura", "M10 0L20 0L20 10L10 10Z")
        });

        Assert(definition.TryResolveRegion("A1", out var code) && code == "A1", "Map definitions should keep canonical codes resolvable when names are duplicated.");
        Assert(definition.TryResolveRegion("B1", out code) && code == "B1", "Map definitions should keep every canonical code resolvable when names are duplicated.");
        Assert(!definition.TryResolveRegion("Jura", out _), "Map definitions should not resolve ambiguous display names to an arbitrary region.");
    }

    private static void MapCatalogsExposeBuiltInDefinitionsById() {
        Assert(ChartMapCatalog.All().Count >= 1, "Map catalogs should expose built-in geography definitions.");
        Assert(ChartMapCatalog.Entries().Count > ChartMapCatalog.All().Count, "Map catalogs should expose known external geography entries separately from embedded map definitions.");
        Assert(ChartMapCatalog.EmbeddedEntries().All(entry => entry.Kind == ChartMapCatalogEntryKind.Embedded && entry.IsEmbedded && !entry.IsExternal), "Embedded map catalog entries should advertise that they ship with the core package.");
        Assert(ChartMapCatalog.ExternalEntries().All(entry => entry.Kind == ChartMapCatalogEntryKind.External && entry.IsExternal && !entry.IsEmbedded), "External map catalog entries should advertise that callers must provide the GeoJSON assets.");
        Assert(ChartMapCatalog.EmbeddedEntries().Count + ChartMapCatalog.ExternalEntries().Count == ChartMapCatalog.Entries().Count, "Combined map catalog entries should remain the union of embedded and external entries.");
        Assert(ChartTileMapCatalog.All().Count >= 1, "Tile-map catalogs should expose built-in tile definitions.");
        Assert(ChartMapCatalog.Get("us-states").Id == "us-states", "Map catalogs should resolve built-in definitions by ID.");
        Assert(ChartMapCatalog.Load("us-states").Id == "us-states", "Unified map catalog loading should return embedded definitions without an asset directory.");
        Assert(ChartMapCatalog.GetEntry("eu-nuts3-2021").JoinKey == "NUTS_ID", "Map catalog entries should document the metric join key for external maps.");
        Assert(ChartMapCatalog.GetEntry("us-counties-2024").JoinKey == "GEOID", "Map catalog entries should document US Census join keys.");
        Assert(ChartTileMapCatalog.Get("us-states").Id == "us-states", "Tile-map catalogs should resolve built-in definitions by ID.");
        Assert(ChartMapCatalog.TryGet("missing", out _) == false, "Map catalogs should reject unknown IDs without throwing.");
        Assert(ChartMapCatalog.TryGetEntry("missing", out _) == false, "Map catalog entries should reject unknown IDs without throwing.");
        Assert(ChartTileMapCatalog.TryGet("missing", out _) == false, "Tile-map catalogs should reject unknown IDs without throwing.");
        AssertThrows<ArgumentException>(() => ChartMapCatalog.Get("missing"), "Map catalogs should throw clear errors for unknown IDs.");
        AssertThrows<ArgumentException>(() => ChartMapCatalog.GetEntry("missing"), "Map catalog entries should throw clear errors for unknown IDs.");
        AssertThrows<ArgumentException>(() => ChartMapCatalog.Load("eu-nuts3-2021"), "Unified map catalog loading should require an asset directory for external entries.");
        AssertThrows<ArgumentException>(() => ChartTileMapCatalog.Get("missing"), "Tile-map catalogs should throw clear errors for unknown IDs.");
    }

    private static void MapCatalogEntriesLoadKnownExternalGeoJsonDefinitions() {
        const string geoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "id": "PL911",
              "properties": { "NUTS_ID": "PL911", "NUTS_NAME": "Warszawski stoleczny", "CNTR_CODE": "PL" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[20.5,52.0],[21.5,52.0],[21.5,52.5],[20.5,52.5],[20.5,52.0]]]
              }
            },
            {
              "type": "Feature",
              "id": "DE300",
              "properties": { "NUTS_ID": "DE300", "NUTS_NAME": "Berlin", "CNTR_CODE": "DE" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[13.0,52.2],[13.8,52.2],[13.8,52.8],[13.0,52.8],[13.0,52.2]]]
              }
            }
          ]
        }
        """;

        var entry = ChartMapCatalog.GetEntry("eu-nuts3-2021");
        var definition = entry.FromGeoJson(geoJson);
        var definitionFromCatalog = ChartMapCatalog.FromGeoJson("eu-nuts3-2021", geoJson);
        var filtered = ChartMapCatalog.FromGeoJson("eu-nuts3-2021", geoJson, options => options.IncludeFeaturePropertyValues("CNTR_CODE", "PL"));
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chartforgex-map-catalog-tests-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        try {
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, entry.FileName), geoJson);
            var definitionFromDirectory = ChartMapCatalog.FromAssetDirectory("eu-nuts3-2021", directory);
            var definitionFromUnifiedLoad = ChartMapCatalog.Load("eu-nuts3-2021", directory);
            var filteredFromUnifiedLoad = ChartMapCatalog.Load("eu-nuts3-2021", directory, options => options.IncludeFeaturePropertyValues("CNTR_CODE", "PL"));
            Assert(definitionFromDirectory.TryResolveRegion("PL911", out _), "Catalog asset directories should load the entry's standard file name without repeating it at the call site.");
            Assert(definitionFromUnifiedLoad.TryResolveRegion("PL911", out _), "Unified map catalog loading should use the same external entry file-name conventions.");
            Assert(filteredFromUnifiedLoad.TryResolveRegion("PL911", out _) && !filteredFromUnifiedLoad.TryResolveRegion("DE300", out _), "Unified map catalog loading should support country-focused option refinements.");
        } finally {
            System.IO.Directory.Delete(directory, recursive: true);
        }

        Assert(definition.Id == "eu-nuts3-2021", "External catalog entries should create definitions with stable catalog IDs.");
        Assert(definition.Name == "EU NUTS3 2021", "External catalog entries should create definitions with stable catalog names.");
        Assert(definition.TryResolveRegion("PL911", out var code) && code == "PL911", "External catalog entries should use their configured code fields.");
        Assert(definition.TryResolveRegion("Warszawski stoleczny", out code) && code == "PL911", "External catalog entries should use their configured name fields.");
        Assert(definitionFromCatalog.TryResolveRegion("PL911", out _), "Catalog-level GeoJSON loading should use the resolved entry import options.");
        Assert(filtered.TryResolveRegion("PL911", out _) && !filtered.TryResolveRegion("DE300", out _), "Catalog-level GeoJSON loading should support country-focused filters without raw importer setup.");
    }

    private static void MapCatalogsAndDefinitionsExposeImmutableLists() {
        Assert(!(ChartMapCatalog.All() is ChartMapDefinition[]), "Map catalogs should not expose mutable backing arrays.");
        Assert(!(ChartTileMapCatalog.All() is ChartTileMapDefinition[]), "Tile-map catalogs should not expose mutable backing arrays.");
        Assert(!(ChartMapCatalog.Get("us-states").Regions is ChartMapRegion[]), "Map definitions should not expose mutable region arrays.");
        Assert(!(ChartTileMapCatalog.Get("us-states").Regions is ChartTileMapRegion[]), "Tile-map definitions should not expose mutable region arrays.");
    }

    private static void MapDefinitionsAcceptRegionNamesAndAliases() {
        var regions = new[] {
            new ChartRegionMapItem("california", 90),
            new ChartRegionMapItem("CA", 10),
            new ChartRegionMapItem("New York", 82),
            new ChartRegionMapItem("Washington D.C.", 12)
        };

        var tileSvg = Chart.Create()
            .WithSize(760, 420)
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), regions)
            .ToSvg();
        var regionSvg = Chart.Create()
            .WithSize(760, 420)
            .WithMapLabels(false)
            .AddRegionMap("Revenue", ChartMapCatalog.Get("us-states"), regions)
            .ToSvg();

        Assert(tileSvg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"100\"", StringComparison.Ordinal), "Tile maps should normalize aliases to canonical codes and aggregate duplicates.");
        Assert(tileSvg.Contains("data-cfx-region=\"NY\" data-cfx-region-name=\"New York\" data-cfx-value=\"82\"", StringComparison.Ordinal), "Tile maps should accept full region names.");
        Assert(tileSvg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"12\"", StringComparison.Ordinal), "Tile maps should accept common District of Columbia aliases.");
        Assert(regionSvg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"100\"", StringComparison.Ordinal), "Region maps should normalize aliases to canonical codes and aggregate duplicates.");
        Assert(regionSvg.Contains("data-cfx-region=\"NY\" data-cfx-region-name=\"New York\" data-cfx-value=\"82\"", StringComparison.Ordinal), "Region maps should accept full region names.");
        Assert(regionSvg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"12\"", StringComparison.Ordinal), "Region maps should accept common District of Columbia aliases.");
    }

    private static void CustomMapDefinitionsPreserveRegionIdentifierCasing() {
        var mapDefinition = new ChartMapDefinition("mixed-case", "Mixed Case", 10, 10, new[] {
            new ChartMapRegion("de-BE", "Berlin", "M0 0L5 0L5 10L0 10Z"),
            new ChartMapRegion("pl-MZ", "Mazowieckie", "M5 0L10 0L10 10L5 10Z")
        });
        var mapSvg = Chart.Create()
            .WithSize(360, 220)
            .WithMapLabels(false)
            .AddRegionMap("Regions", mapDefinition, new[] {
                new ChartRegionMapItem("de-BE", 10),
                new ChartRegionMapItem("pl-MZ", 20)
            })
            .ToSvg();

        var tileDefinition = new ChartTileMapDefinition("mixed-case-tiles", "Mixed Case Tiles", new[] {
            new ChartTileMapRegion("tenant-A", "Tenant A", 0, 0),
            new ChartTileMapRegion("tenant-b", "Tenant b", 1, 0)
        });
        var tileSvg = Chart.Create()
            .WithSize(360, 220)
            .WithMapLabels(false)
            .AddTileMap("Tenants", tileDefinition, new[] {
                new ChartRegionMapItem("tenant-A", 10),
                new ChartRegionMapItem("tenant-b", 20)
            })
            .ToSvg();

        Assert(mapSvg.Contains("data-cfx-region=\"de-BE\"", StringComparison.Ordinal), "Custom region maps should preserve caller-defined mixed-case identifiers.");
        Assert(mapSvg.Contains("data-cfx-region=\"pl-MZ\"", StringComparison.Ordinal), "Custom region maps should not force country or subdivision identifiers to US-style uppercase.");
        Assert(tileSvg.Contains("data-cfx-region=\"tenant-A\"", StringComparison.Ordinal), "Custom tile maps should preserve caller-defined mixed-case identifiers.");
        Assert(tileSvg.Contains("data-cfx-region=\"tenant-b\"", StringComparison.Ordinal), "Custom tile maps should preserve lowercase identifiers.");
    }

    private static void CustomMapDefinitionsValidateGeometryInputs() {
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapDefinition("bad", "Bad", new ChartRect(0, 0, 0, 10), new[] { new ChartMapRegion("A", "A", "M0 0L1 0L1 1Z") }), "Map definitions should reject zero-width bounds.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapDefinition("bad", "Bad", new ChartRect(0, 0, 10, -1), new[] { new ChartMapRegion("A", "A", "M0 0L1 0L1 1Z") }), "Map definitions should reject negative-height bounds.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapDefinition("bad", "Bad", new ChartRect(0, 0, double.NaN, 10), new[] { new ChartMapRegion("A", "A", "M0 0L1 0L1 1Z") }), "Map definitions should reject non-finite bounds.");
        AssertThrows<ArgumentException>(() => new ChartTileMapDefinition("bad", "Bad", new[] {
            new ChartTileMapRegion("A", "Alpha", 0, 0),
            new ChartTileMapRegion("B", "Beta", 0, 0)
        }), "Tile-map definitions should reject overlapping tile coordinates.");
    }

    private static void CustomRegionMapsParseStandardSvgPathSeparators() {
        var definition = new ChartMapDefinition("custom", "Custom", 10, 10, new[] {
            new ChartMapRegion("A", "Alpha", "M0,0 10,0 10,10 0,10Z"),
            new ChartMapRegion("B", "Beta", "M1e0-1L3,1L3,3L1,3z m.5,.5 l1,0 0,1 -1,0 z")
        });
        var chart = Chart.Create()
            .WithSize(360, 240)
            .WithMapLabels(false)
            .AddRegionMap("Custom", definition, new[] {
                new ChartRegionMapItem("A", 10),
                new ChartRegionMapItem("B", 20)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-map-id=\"custom\"", StringComparison.Ordinal), "Custom region maps should render SVG from caller-supplied path data.");
        Assert(svg.Contains("fill-rule=\"evenodd\"", StringComparison.Ordinal), "Custom region-map SVG paths should use even-odd filling so interior holes remain holes.");
        Assert(chart.ToPng().Length > 64, "Custom region maps with comma-separated paths and interior holes should render PNG output.");
    }

    private static void CustomRegionMapPngPreservesInteriorHoles() {
        var definition = new ChartMapDefinition("holes", "Holes", 12, 10, new[] {
            new ChartMapRegion("A", "Alpha", "M0,0 10,0 10,10 0,10Z M2,2 8,2 8,8 2,8Z"),
            new ChartMapRegion("B", "Beta", "M11,0 12,0 12,1 11,1Z")
        });
        var chart = Chart.Create()
            .WithSize(240, 190)
            .WithMapLabels(false)
            .WithMapScaleLegend(false)
            .AddRegionMap("Holes", definition, new[] {
                new ChartRegionMapItem("A", 100, ChartColor.FromHex("#DC2626")),
                new ChartRegionMapItem("B", 0)
            });

        var image = DecodePng(chart.ToPng());
        var redCount = 0;
        var minX = image.Width;
        var maxX = 0;
        var minY = image.Height;
        var maxY = 0;
        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                if (!IsStrongRegionRed(image.Pixel(x, y))) continue;
                redCount++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        Assert(redCount > 500, "Region-map PNG regression should find a strong filled outer region before testing its hole.");
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;
        var centerRedPixels = 0;
        for (var y = centerY - 4; y <= centerY + 4; y++) {
            for (var x = centerX - 4; x <= centerX + 4; x++) {
                if (IsStrongRegionRed(image.Pixel(x, y))) centerRedPixels++;
            }
        }

        Assert(centerRedPixels == 0, "Region-map PNG rendering should preserve interior holes instead of filling every ring independently.");

        static bool IsStrongRegionRed((byte R, byte G, byte B, byte A) pixel) {
            return pixel.A > 200 && pixel.R > 170 && pixel.G < 90 && pixel.B < 90;
        }
    }

    private static void CustomRegionMapsParseCommonSvgPathCommands() {
        var definition = new ChartMapDefinition("commands", "Commands", 100, 100, new[] {
            new ChartMapRegion("A", "Alpha", "M10 10 H90 V45 C90 60 75 70 60 70 S30 80 20 55 Q12 38 10 10 Z"),
            new ChartMapRegion("B", "Beta", "M35 35 h30 v18 t-30 0 z"),
            new ChartMapRegion("C", "Gamma", "M25 82 A18 12 20 0 1 75 82 A18 12 20 0 1 25 82 Z")
        });
        var chart = Chart.Create()
            .WithSize(420, 300)
            .WithMapLabels(false)
            .AddRegionMap("Commands", definition, new[] {
                new ChartRegionMapItem("A", 10),
                new ChartRegionMapItem("B", 20),
                new ChartRegionMapItem("C", 30)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-map-id=\"commands\"", StringComparison.Ordinal), "Custom region maps should render SVG from common path command input.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"region-map-region\"") == 3, "Custom region maps should render each region after flattening common SVG path commands.");
        Assert(chart.ToPng().Length > 64, "Custom region maps with H/V, curve, smooth, and arc commands should render PNG output.");
    }

    private static void CustomRegionMapsPreserveSubunitBoundsAspectRatio() {
        var definition = new ChartMapDefinition("normalized", "Normalized", new ChartRect(0, 0, 1, 0.5), new[] {
            new ChartMapRegion("A", "Alpha", "M0,0 1,0 1,.5 0,.5Z")
        });
        var chart = Chart.Create()
            .WithSize(420, 300)
            .WithMapLabels(false)
            .WithMapScaleLegend(false)
            .AddRegionMap("Normalized", definition, new[] {
                new ChartRegionMapItem("A", 10)
            });

        var svg = chart.ToSvg();
        var path = GetStringAttribute(svg, "data-cfx-region=\"A\"", "d");
        var bounds = GetPathBounds(path);
        Assert(bounds.Width > bounds.Height * 1.8 && bounds.Width < bounds.Height * 2.2, "Region maps should preserve valid source bounds below one unit instead of clamping height to one.");
        Assert(chart.ToPng().Length > 64, "Subunit-bounds custom region maps should render PNG output.");
    }

    private static void TileMapLabelsCanBeHiddenForCompactCards() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithMapLabels(false)
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"tile-map-label\"", StringComparison.Ordinal), "Tile maps should allow compact cards to hide tile labels.");
        Assert(svg.Contains("data-cfx-role=\"tile-map-region\"", StringComparison.Ordinal), "Tile maps should still render regions when labels are hidden.");
        Assert(chart.ToPng().Length > 64, "Tile maps without labels should render PNG output.");
    }

    private static void RegionMapScaleLegendCanBeHiddenForCompactCards() {
        var chart = Chart.Create()
            .WithSize(420, 260)
            .WithMapScaleLegend(false)
            .WithMapLabels(false)
            .AddRegionMap("Revenue", ChartMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"region-map-scale-step\"", StringComparison.Ordinal), "Region maps should allow compact cards to hide scale legends.");
        Assert(svg.Contains("data-cfx-role=\"region-map-region\"", StringComparison.Ordinal), "Region maps should still render regions when scale legends are hidden.");
        Assert(chart.ToPng().Length > 64, "Region maps without scale legends should render PNG output.");
    }

    private static void RegionMapLabelsOnlyRenderWhenRegionsHaveRoom() {
        var chart = Chart.Create()
            .WithSize(760, 420)
            .AddRegionMap("Revenue", ChartMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("DC", 42)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"region-map-label\" x=", StringComparison.Ordinal), "Region maps should render labels for regions that have enough room.");
        Assert(svg.Contains(">CA</text>", StringComparison.Ordinal), "Region maps should label large regions by default.");
        Assert(!svg.Contains(">DC</text>", StringComparison.Ordinal), "Region maps should avoid cramped labels for tiny regions.");
        Assert(svg.Contains("<title>District of Columbia (DC): 42</title>", StringComparison.Ordinal), "Region maps should preserve tiny-region identity through hover titles.");
        Assert(chart.ToPng().Length > 64, "Region maps with fitted labels should render PNG output.");
    }

    private static void TileMapScaleLegendCanBeHiddenForCompactCards() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithMapScaleLegend(false)
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"tile-map-scale-step\"", StringComparison.Ordinal), "Tile maps should allow compact cards to hide scale legends.");
        Assert(svg.Contains("data-cfx-role=\"tile-map-region\"", StringComparison.Ordinal), "Tile maps should still render regions when scale legends are hidden.");
        Assert(chart.ToPng().Length > 64, "Tile maps without scale legends should render PNG output.");
    }

    private static void MapScaleLegendSeparatesNoDataFromLowValues() {
        var completeRegions = AllCatalogRegions();
        var region = Chart.Create()
            .WithSize(760, 420)
            .AddRegionMap("Revenue", ChartMapCatalog.Get("us-states"), completeRegions);
        var tile = Chart.Create()
            .WithSize(760, 420)
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), completeRegions);

        var regionSvg = region.ToSvg();
        var tileSvg = tile.ToSvg();
        Assert(!regionSvg.Contains("data-cfx-role=\"region-map-scale-no-data\"", StringComparison.Ordinal), "Complete region maps should not reserve a missing-data legend swatch.");
        Assert(!tileSvg.Contains("data-cfx-role=\"tile-map-scale-no-data\"", StringComparison.Ordinal), "Complete tile maps should not reserve a missing-data legend swatch.");
        Assert(CountOccurrences(regionSvg, "data-cfx-role=\"region-map-scale-step\"") == 5, "Region maps should render five value-colored scale steps.");
        Assert(CountOccurrences(tileSvg, "data-cfx-role=\"tile-map-scale-step\"") == 5, "Tile maps should render five value-colored scale steps.");
        Assert(regionSvg.Contains("data-cfx-role=\"region-map-scale-step\" data-cfx-value=\"10\" data-cfx-status=\"negative\"", StringComparison.Ordinal), "Region map scale steps should expose the low value as data, not as no-data.");
        Assert(tileSvg.Contains("data-cfx-role=\"tile-map-scale-step\" data-cfx-value=\"10\" data-cfx-status=\"negative\"", StringComparison.Ordinal), "Tile map scale steps should expose the low value as data, not as no-data.");
        Assert(region.ToPng().Length > 64, "Complete region map scale legends should render PNG output.");
        Assert(tile.ToPng().Length > 64, "Complete tile map scale legends should render PNG output.");
    }

    private static void MapNoDataScaleStaysInsideCompactCards() {
        var region = Chart.Create()
            .WithSize(300, 210)
            .WithMapLabels(false)
            .AddRegionMap("Revenue", ChartMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });
        var tile = Chart.Create()
            .WithSize(300, 210)
            .WithMapLabels(false)
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var regionSvg = region.ToSvg();
        var tileSvg = tile.ToSvg();
        var regionWidth = GetAttribute(regionSvg, "<svg", "width");
        var tileWidth = GetAttribute(tileSvg, "<svg", "width");
        var regionHeight = GetAttribute(regionSvg, "<svg", "height");
        var tileHeight = GetAttribute(tileSvg, "<svg", "height");
        var regionNoDataX = GetAttribute(regionSvg, "data-cfx-role=\"region-map-scale-no-data\"", "x");
        var tileNoDataX = GetAttribute(tileSvg, "data-cfx-role=\"tile-map-scale-no-data\"", "x");
        var regionNoDataY = GetAttribute(regionSvg, "data-cfx-role=\"region-map-scale-no-data\"", "y");
        var tileNoDataY = GetAttribute(tileSvg, "data-cfx-role=\"tile-map-scale-no-data\"", "y");
        var regionScaleX = GetAttribute(regionSvg, "data-cfx-role=\"region-map-scale-step\"", "x");
        var tileScaleX = GetAttribute(tileSvg, "data-cfx-role=\"tile-map-scale-step\"", "x");

        Assert(regionNoDataX >= 0 && regionNoDataX < regionWidth, "Region map no-data scale swatches should remain inside compact SVG cards.");
        Assert(tileNoDataX >= 0 && tileNoDataX < tileWidth, "Tile map no-data scale swatches should remain inside compact SVG cards.");
        Assert(regionNoDataY >= 0 && regionNoDataY < regionHeight, "Region map no-data scale swatches should remain vertically inside compact SVG cards.");
        Assert(tileNoDataY >= 0 && tileNoDataY < tileHeight, "Tile map no-data scale swatches should remain vertically inside compact SVG cards.");
        Assert(regionNoDataX < regionScaleX, "Region map no-data swatches should not overlap the value scale start.");
        Assert(tileNoDataX < tileScaleX, "Tile map no-data swatches should not overlap the value scale start.");
        Assert(region.ToPng().Length > 64, "Compact region map no-data scale legends should render PNG output.");
        Assert(tile.ToPng().Length > 64, "Compact tile map no-data scale legends should render PNG output.");
    }

    private static ChartRegionMapItem[] AllCatalogRegions() {
        var regions = ChartMapCatalog.Get("us-states").Regions;
        var values = new ChartRegionMapItem[regions.Count];
        for (var i = 0; i < regions.Count; i++) values[i] = new ChartRegionMapItem(regions[i].Code, 10 + i);
        return values;
    }

    private static ChartRect GetPathBounds(string path) {
        var numbers = new List<double>();
        var start = -1;
        for (var i = 0; i <= path.Length; i++) {
            var value = i < path.Length ? path[i] : ' ';
            if (char.IsDigit(value) || value == '-' || value == '+' || value == '.' || value == 'e' || value == 'E') {
                if (start < 0) start = i;
                continue;
            }

            if (start >= 0) {
                numbers.Add(double.Parse(path.Substring(start, i - start), CultureInfo.InvariantCulture));
                start = -1;
            }
        }

        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        for (var i = 0; i + 1 < numbers.Count; i += 2) {
            var x = numbers[i];
            var y = numbers[i + 1];
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        return new ChartRect(minX, minY, maxX - minX, maxY - minY);
    }
}
