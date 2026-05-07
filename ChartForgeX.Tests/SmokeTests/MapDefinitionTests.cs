using System;
using System.Collections.Generic;
using System.Globalization;
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

    private static void MapCatalogsExposeBuiltInDefinitionsById() {
        Assert(ChartMapCatalog.All().Count >= 1, "Map catalogs should expose built-in geography definitions.");
        Assert(ChartTileMapCatalog.All().Count >= 1, "Tile-map catalogs should expose built-in tile definitions.");
        Assert(ChartMapCatalog.Get("us-states").Id == "us-states", "Map catalogs should resolve built-in definitions by ID.");
        Assert(ChartTileMapCatalog.Get("us-states").Id == "us-states", "Tile-map catalogs should resolve built-in definitions by ID.");
        Assert(ChartMapCatalog.TryGet("missing", out _) == false, "Map catalogs should reject unknown IDs without throwing.");
        Assert(ChartTileMapCatalog.TryGet("missing", out _) == false, "Tile-map catalogs should reject unknown IDs without throwing.");
        AssertThrows<ArgumentException>(() => ChartMapCatalog.Get("missing"), "Map catalogs should throw clear errors for unknown IDs.");
        AssertThrows<ArgumentException>(() => ChartTileMapCatalog.Get("missing"), "Tile-map catalogs should throw clear errors for unknown IDs.");
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
