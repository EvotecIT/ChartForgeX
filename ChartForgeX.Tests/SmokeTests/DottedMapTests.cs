using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void DottedMapRendersWorldDotsAndPoints() {
        var chart = Chart.Create()
            .WithSize(760, 420)
            .WithTitle("Travel Map")
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Indonesia", 113.9213, -0.7893, ChartColor.FromHex("#22C55E")),
                new ChartMapPoint("Spain", -3.7038, 40.4168),
                new ChartMapPoint("United States", -98.5795, 39.8283)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"dotted-map\"", StringComparison.Ordinal), "Dotted maps should expose a role marker.");
        Assert(svg.Contains("data-cfx-role=\"dotted-map\" data-cfx-map-kind=\"world-dotted\" data-cfx-label=\"Visited\" data-cfx-projection=\"equirectangular\" data-cfx-point-count=\"3\"", StringComparison.Ordinal), "Dotted map containers should expose map-kind, label, projection, and point-count metadata.");
        Assert(svg.Contains("data-cfx-min-longitude=\"-98.58\" data-cfx-max-longitude=\"113.921\" data-cfx-min-latitude=\"-0.789\" data-cfx-max-latitude=\"40.417\"", StringComparison.Ordinal), "Dotted map containers should expose source coordinate bounds.");
        Assert(svg.Contains("Travel Map dotted world map for Visited with 3 highlighted points.", StringComparison.Ordinal), "Dotted map SVG descriptions should summarize the specialized chart shape.");
        Assert(svg.Contains("role=\"group\" aria-label=\"Visited dotted map with 3 highlighted points\"", StringComparison.Ordinal), "Dotted map containers should expose a useful group label.");
        Assert(!svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "Dotted maps should not emit generic series legends.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-graticule\"") == 5, "Dotted maps should render subtle longitude and latitude guide lines.");
        Assert(svg.Contains("data-cfx-role=\"dotted-map-base-layer\" clip-path=\"url(#", StringComparison.Ordinal), "Dotted maps should clip geography and route base layers to the plot frame.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-land-dot\"") > 1000, "Dotted maps should render a recognizable dotted land layer from world geometry.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-point\"") == 3, "Dotted maps should render one highlighted point per map item.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-point-halo\"") == 3, "Dotted maps should render a soft emphasis halo for each highlighted point.");
        Assert(svg.Contains("data-cfx-label=\"Indonesia\"", StringComparison.Ordinal), "Dotted map points should expose labels.");
        Assert(svg.Contains("data-cfx-longitude=\"113.921\"", StringComparison.Ordinal), "Dotted map points should expose longitudes.");
        Assert(svg.Contains("data-cfx-longitude-label=\"113.921 E\"", StringComparison.Ordinal), "Dotted map points should expose human-readable longitude labels.");
        Assert(svg.Contains("data-cfx-latitude-label=\"0.789 S\"", StringComparison.Ordinal), "Dotted map points should expose human-readable latitude labels.");
        Assert(svg.Contains("<title>Indonesia: 0.789 S, 113.921 E</title>", StringComparison.Ordinal), "Dotted map points should expose native SVG hover titles.");
        Assert(svg.Contains("class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"dotted-map-point\"", StringComparison.Ordinal), "Dotted map points should be keyboard-focusable interactive SVG regions.");
        Assert(svg.Contains("fill=\"#22C55E\"", StringComparison.Ordinal), "Dotted map points should honor per-point colors.");
        Assert(chart.ToPng().Length > 64, "Dotted maps should render PNG output.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddDottedMap("Empty", Array.Empty<ChartMapPoint>()), "Dotted maps should reject empty inputs.");
        AssertThrows<ArgumentException>(() => new ChartMapPoint(" ", 0, 0), "Map points should reject empty labels.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapPoint("Bad", 181, 0), "Map points should reject invalid longitudes.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapPoint("Bad", 0, 91), "Map points should reject invalid latitudes.");
    }

    private static void DottedMapTrimsPointLabels() {
        var svg = Chart.Create()
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("  Spain  ", -3.7038, 40.4168)
            })
            .ToSvg();

        Assert(svg.Contains("data-cfx-label=\"Spain\"", StringComparison.Ordinal), "Dotted map points should trim labels before rendering metadata.");
        Assert(svg.Contains("<title>Spain: 40.417 N, 3.704 W</title>", StringComparison.Ordinal), "Dotted map hover titles should use trimmed labels.");
    }

    private static void DottedMapWorldViewportSuppressesPolarPointsOutsideMapBand() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .AddDottedMap("Extremes", new[] {
                new ChartMapPoint("North Pole", 0, 90),
                new ChartMapPoint("South Pole", 0, -90)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-point-count=\"2\"", StringComparison.Ordinal), "Dotted map containers should preserve the source point count.");
        Assert(svg.Contains("data-cfx-visible-point-count=\"0\"", StringComparison.Ordinal), "The world viewport should not treat polar points outside its latitude band as visible.");
        Assert(!svg.Contains("data-cfx-role=\"dotted-map-point\"", StringComparison.Ordinal), "Out-of-viewport polar points should not be clamped onto the visible map edge.");
        Assert(!svg.Contains("data-cfx-label=\"North Pole\"", StringComparison.Ordinal), "Out-of-viewport polar point labels should not render at misleading edge positions.");
        Assert(!svg.Contains("data-cfx-label=\"South Pole\"", StringComparison.Ordinal), "Out-of-viewport polar point labels should not render at misleading edge positions.");
        Assert(chart.ToPng().Length > 64, "Dotted maps with out-of-viewport polar source data should render PNG output.");
    }

    private static void DottedMapDataLabelsSuppressPolarPointsOutsideMapBand() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithDataLabels()
            .AddDottedMap("Extremes", new[] {
                new ChartMapPoint("North Pole", 0, 90),
                new ChartMapPoint("South Pole", 0, -90)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-visible-point-count=\"0\"", StringComparison.Ordinal), "World dotted maps should suppress source points outside the actual visible latitude band.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-label\"") == 0, "Dotted map data labels should not render for out-of-viewport points.");
        Assert(!svg.Contains("data-cfx-label=\"North Pole\"", StringComparison.Ordinal), "Out-of-viewport point labels should not be clamped into the fitted map band.");
        Assert(!svg.Contains("data-cfx-label=\"South Pole\"", StringComparison.Ordinal), "Out-of-viewport point labels should not be clamped into the fitted map band.");
        Assert(chart.ToPng().Length > 64, "Dotted maps with out-of-viewport data labels should render PNG output.");
    }

    private static void DottedMapClusteredLabelsUseAlternatePlacements() {
        var chart = Chart.Create()
            .WithSize(520, 300)
            .WithDataLabels()
            .AddDottedMap("Cluster", new[] {
                new ChartMapPoint("A", 0, 0),
                new ChartMapPoint("B", 0, 0),
                new ChartMapPoint("C", 0, 0),
                new ChartMapPoint("D", 0, 0)
            });

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-label\"") == 4, "Dotted maps should label clustered highlighted points when labels are enabled.");
        Assert(svg.Contains("data-cfx-placement=\"top\"", StringComparison.Ordinal), "Dotted map labels should try the top placement.");
        Assert(svg.Contains("data-cfx-placement=\"right\"", StringComparison.Ordinal), "Dotted map labels should use alternate right placement for clustered points.");
        Assert(svg.Contains("data-cfx-placement=\"bottom\"", StringComparison.Ordinal), "Dotted map labels should use alternate bottom placement for clustered points.");
        Assert(svg.Contains("data-cfx-placement=\"left\"", StringComparison.Ordinal), "Dotted map labels should use alternate left placement for clustered points.");
        Assert(chart.ToPng().Length > 64, "Clustered dotted maps with labels should render PNG output.");
    }

    private static void DottedMapDenseClustersUseDiagonalLabelPlacements() {
        var chart = Chart.Create()
            .WithSize(520, 300)
            .WithDataLabels()
            .AddDottedMap("Cluster", new[] {
                new ChartMapPoint("Alpha Market", 0, 0),
                new ChartMapPoint("Beta Market", 0, 0),
                new ChartMapPoint("Central Market", 0, 0),
                new ChartMapPoint("Delta Market", 0, 0),
                new ChartMapPoint("Eastern Market", 0, 0),
                new ChartMapPoint("Frontier Market", 0, 0)
            });

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-label\"") == 6, "Dotted maps should continue labeling dense highlighted clusters when labels are enabled.");
        Assert(svg.Contains("data-cfx-placement=\"top-right\"", StringComparison.Ordinal) || svg.Contains("data-cfx-placement=\"bottom-right\"", StringComparison.Ordinal) || svg.Contains("data-cfx-placement=\"bottom-left\"", StringComparison.Ordinal) || svg.Contains("data-cfx-placement=\"top-left\"", StringComparison.Ordinal) || svg.Contains("data-cfx-placement=\"far-top-right\"", StringComparison.Ordinal) || svg.Contains("data-cfx-placement=\"far-bottom-right\"", StringComparison.Ordinal) || svg.Contains("data-cfx-placement=\"far-bottom-left\"", StringComparison.Ordinal) || svg.Contains("data-cfx-placement=\"far-top-left\"", StringComparison.Ordinal), "Dense dotted-map label clusters should use diagonal placements after cardinal lanes are occupied.");
        Assert(chart.ToPng().Length > 64, "Dense clustered dotted maps with diagonal labels should render PNG output.");
    }

    private static void DottedMapPreservesMapAspectRatioInTallCards() {
        var chart = Chart.Create()
            .WithSize(420, 620)
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Equator", 0, 0)
            });

        var svg = chart.ToSvg();
        var width = GetAttribute(svg, "<svg", "width");
        var height = GetAttribute(svg, "<svg", "height");
        var west = GetAttribute(svg, "data-cfx-role=\"dotted-map-land-dot\"", "cx");
        var top = GetAttribute(svg, "data-cfx-role=\"dotted-map-land-dot\"", "cy");
        var centerY = GetAttribute(svg, "data-cfx-label=\"Equator\"", "cy");
        Assert(centerY > height * 0.30 && centerY < height * 0.70, "Dotted maps should center the fitted world band in tall cards.");
        Assert(west > 0 && west < width, "Dotted map fitted longitude coordinates should remain inside the SVG viewport.");
        Assert(top > 0 && top < height, "Dotted map fitted latitude coordinates should remain inside the SVG viewport.");
        Assert(chart.ToPng().Length > 64, "Aspect-fitted dotted maps should render PNG output.");
    }

    private static void DottedMapViewportFocusesRegionalMaps() {
        var chart = Chart.Create()
            .WithSize(420, 320)
            .WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Spain", -3.7038, 40.4168),
                new ChartMapPoint("Poland", 19.1451, 51.9194),
                new ChartMapPoint("United States", -98.5795, 39.8283)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-viewport=\"Europe\"", StringComparison.Ordinal), "Dotted maps should expose the selected viewport.");
        Assert(svg.Contains("data-cfx-visible-point-count=\"2\"", StringComparison.Ordinal), "Dotted map viewports should report the number of visible highlighted points.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-point\"") == 2, "Dotted map viewports should render only points inside the selected map window.");
        Assert(svg.Contains("data-cfx-label=\"Spain\"", StringComparison.Ordinal) && svg.Contains("data-cfx-label=\"Poland\"", StringComparison.Ordinal), "Regional dotted maps should keep in-viewport point labels.");
        Assert(!svg.Contains("data-cfx-label=\"United States\"", StringComparison.Ordinal), "Regional dotted maps should suppress out-of-viewport point marks.");
        Assert(!svg.Contains("data-cfx-role=\"dotted-map-land-dot\"", StringComparison.Ordinal), "Regional dotted maps with filled boundaries should avoid a secondary dot texture that can read as stray data.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-land-area\"") > 10, "Regional dotted maps should render a quiet land-area layer so the map reads before dotted texture is added.");
        Assert(svg.Contains("data-cfx-role=\"dotted-map-land-area\"", StringComparison.Ordinal), "Regional dotted maps should use filled geography rather than texture dots when boundary polygons are available.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-boundary\"") > 20, "Regional dotted maps should render subtle Natural Earth boundary paths so focused viewport cards read as geography at small sizes.");
        Assert(GetAttribute(svg, "data-cfx-role=\"dotted-map-boundary\"", "stroke-width") >= 0.65, "Regional dotted-map boundaries should remain readable after removing the secondary land-dot texture.");
        Assert(GetAttribute(svg, "data-cfx-role=\"dotted-map-point\"", "r") >= 3, "Highlighted dotted-map points should remain readable in compact grid panels.");
        var polandSvg = Chart.Create()
            .WithSize(420, 320)
            .WithMapViewport(ChartMapViewport.Poland())
            .AddDottedMap("Cities", new[] { new ChartMapPoint("Warsaw", 21.0122, 52.2297) })
            .ToSvg();
        Assert(CountOccurrences(polandSvg, "data-cfx-role=\"dotted-map-land-dot\"") > 100, "Poland dotted maps should use a dedicated country-shaped land-dot layer instead of a sparse rectangular grid.");
        Assert(polandSvg.Contains("data-cfx-role=\"dotted-map-viewport-shape\" data-cfx-viewport=\"Poland\"", StringComparison.Ordinal), "Country-focused dotted maps should render a subtle viewport silhouette so the geography remains readable in small previews.");
        Assert(Chart.Create().WithMapViewport(ChartMapViewport.Poland()).AddDottedMap("Cities", new[] { new ChartMapPoint("Warsaw", 21.0122, 52.2297) }).ToPng().Length > 64, "Country-focused dotted map viewports should render PNG output.");
        AssertThrows<ArgumentException>(() => new ChartMapViewport(" ", -10, 10, -10, 10), "Map viewports should reject empty names.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapViewport("Bad", 10, 10, -10, 10), "Map viewports should reject unordered longitude ranges.");
    }

    private static void DottedMapRegionalLabelsAvoidMarkerHalos() {
        var chart = Chart.Create()
            .WithSize(360, 260)
            .WithMapViewport(ChartMapViewport.Poland())
            .WithDataLabels()
            .AddDottedMap("Cities", new[] {
                new ChartMapPoint("Gdansk", 18.6466, 54.3520),
                new ChartMapPoint("Warsaw", 21.0122, 52.2297),
                new ChartMapPoint("Krakow", 19.9450, 50.0647)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-label=\"Gdansk\" data-cfx-placement=\"bottom\"", StringComparison.Ordinal) || svg.Contains("data-cfx-label=\"Gdansk\" data-cfx-placement=\"right\"", StringComparison.Ordinal) || svg.Contains("data-cfx-label=\"Gdansk\" data-cfx-placement=\"left\"", StringComparison.Ordinal), "Regional dotted-map labels should avoid clamping into large marker halos near viewport edges.");
        Assert(chart.ToPng().Length > 64, "Regional dotted-map label placement polish should render PNG output.");
    }

    private static void DottedMapRendersWeightedRevenueMarkers() {
        var chart = Chart.Create()
            .WithSize(520, 320)
            .WithMapViewport(ChartMapViewport.Europe())
            .WithDataLabels()
            .WithValueFormatter(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "k")
            .AddDottedMap("Revenue", new[] {
                new ChartMapPoint("Poland", 19.1451, 51.9194, 142, ChartColor.FromHex("#DC2626")),
                new ChartMapPoint("Germany", 10.4515, 51.1657, 214, ChartColor.FromHex("#22C55E")),
                new ChartMapPoint("Spain", -3.7038, 40.4168, 96, ChartColor.FromHex("#F59E0B"))
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-valued-point-count=\"3\" data-cfx-min-value=\"96\" data-cfx-max-value=\"214\"", StringComparison.Ordinal), "Weighted dotted maps should expose value range metadata for country and market maps.");
        Assert(svg.Contains("data-cfx-label=\"Germany\" data-cfx-value=\"214\" data-cfx-formatted-value=\"$214k\"", StringComparison.Ordinal), "Weighted dotted map points should expose raw and formatted values.");
        Assert(svg.Contains("data-cfx-role=\"dotted-map-label-backdrop\" data-cfx-label=\"Germany $214k\"", StringComparison.Ordinal), "Weighted dotted map data labels should render readable label backdrops.");
        Assert(svg.Contains("data-cfx-role=\"dotted-map-label\" data-cfx-label=\"Germany $214k\"", StringComparison.Ordinal), "Weighted dotted map data labels should include formatted values when labels are enabled.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-label-leader\"") == 3, "Weighted dotted map labels should render subtle leader lines back to their markers.");
        Assert(svg.IndexOf("data-cfx-role=\"dotted-map-label-leader\" data-cfx-label=\"Germany $214k\"", StringComparison.Ordinal) < svg.IndexOf("data-cfx-role=\"dotted-map-label-backdrop\" data-cfx-label=\"Germany $214k\"", StringComparison.Ordinal), "Dotted map label leaders should render behind label backdrops.");
        Assert(svg.Contains("<title>Germany: $214k; 51.166 N, 10.452 E</title>", StringComparison.Ordinal), "Weighted dotted map hover titles should include formatted values and coordinates.");
        Assert(GetAttribute(svg, "data-cfx-label=\"Germany\"", "r") > GetAttribute(svg, "data-cfx-label=\"Spain\"", "r"), "Higher-value dotted map points should render with larger markers.");
        Assert(chart.ToPng().Length > 64, "Weighted dotted maps should render PNG output.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartMapPoint("Bad", 0, 0, -1), "Weighted map points should reject negative values.");
    }

    private static void DottedMapRendersConnectorRoutes() {
        var chart = Chart.Create()
            .WithSize(520, 300)
            .WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Spain", -3.7038, 40.4168),
                new ChartMapPoint("Warsaw", 21.0122, 52.2297),
                new ChartMapPoint("Oslo", 10.7522, 59.9139)
            })
            .AddMapRouteBetweenPoints("Spain to Warsaw", "Spain", "Warsaw", ChartColor.FromHex("#22C55E"))
            .AddMapConnectorBetweenPoints("Warsaw to Oslo", "Warsaw", "Oslo");

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-connector-count=\"2\"", StringComparison.Ordinal), "Dotted maps should expose the number of visible connector routes.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-connector-halo\"") == 2, "Dotted maps should render a subtle route halo so connectors stay readable over land texture.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-connector\" data-cfx-connector=") == 2, "Dotted maps should render route connectors before point markers.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"dotted-map-connector-arrow\"") == 2, "Dotted map connectors should render directional arrowheads.");
        Assert(svg.Contains("class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"dotted-map-connector\"", StringComparison.Ordinal), "Dotted map connectors should be keyboard-focusable interactive SVG regions.");
        Assert(svg.Contains("style=\"--cfx-interactive-focus-stroke-width:", StringComparison.Ordinal), "Interactive dotted map connector routes should carry a connector-sized focus stroke.");
        Assert(svg.Contains(".cfx-interactive-region[data-cfx-role=\"dotted-map-connector\"]{pointer-events:stroke}", StringComparison.Ordinal), "Interactive dotted map connector routes should target the visible stroke for pointer interactions.");
        Assert(svg.Contains("role=\"img\" aria-label=\"Spain to Warsaw:", StringComparison.Ordinal), "Dotted map connectors should expose accessible route summaries.");
        Assert(svg.Contains("data-cfx-label=\"Spain to Warsaw\"", StringComparison.Ordinal), "Dotted map connectors should expose labels.");
        Assert(svg.Contains("data-cfx-from-longitude=\"-3.704\" data-cfx-from-latitude=\"40.417\" data-cfx-to-longitude=\"21.012\" data-cfx-to-latitude=\"52.23\"", StringComparison.Ordinal), "Point-bound dotted map routes should use the rendered source and target marker coordinates.");
        Assert(svg.Contains("data-cfx-control-x=", StringComparison.Ordinal) && svg.Contains("data-cfx-control-y=", StringComparison.Ordinal), "Dotted map connectors should expose SVG control points for visual QA and regression checks.");
        Assert(svg.Contains("data-cfx-rendered-from-x=", StringComparison.Ordinal) && svg.Contains("data-cfx-rendered-to-x=", StringComparison.Ordinal), "Dotted map connectors should expose trimmed rendered endpoints for visual QA.");
        var spainMarkerX = GetAttribute(svg, "data-cfx-label=\"Spain\"", "cx");
        var spainMarkerY = GetAttribute(svg, "data-cfx-label=\"Spain\"", "cy");
        var renderedFromX = GetAttribute(svg, "data-cfx-label=\"Spain to Warsaw\"", "data-cfx-rendered-from-x");
        var renderedFromY = GetAttribute(svg, "data-cfx-label=\"Spain to Warsaw\"", "data-cfx-rendered-from-y");
        Assert(Math.Sqrt(Math.Pow(renderedFromX - spainMarkerX, 2) + Math.Pow(renderedFromY - spainMarkerY, 2)) > 3, "Point-bound dotted map routes should start at the marker edge instead of disappearing under marker centers.");
        Assert(svg.Contains("<title>Spain to Warsaw:", StringComparison.Ordinal), "Dotted map connectors should expose native SVG hover titles.");
        Assert(svg.Contains("stroke=\"#22C55E\"", StringComparison.Ordinal), "Dotted map connectors should honor explicit connector colors.");
        Assert(chart.ToPng().Length > 64, "Dotted map connector routes should render PNG output.");
        var labeledRouteSvg = chart.WithDataLabels().ToSvg();
        Assert(CountOccurrences(labeledRouteSvg, "data-cfx-role=\"dotted-map-connector-label\"") == 2, "Dotted map connector labels should render when map data labels are enabled.");
        Assert(labeledRouteSvg.Contains(">Spain to Warsaw<", StringComparison.Ordinal), "Non-latency dotted-map route labels should render as host-visible route text.");
        var verticalRouteSvg = Chart.Create()
            .WithSize(520, 360)
            .WithMapViewport(ChartMapViewport.Africa())
            .AddDottedMap("Cities", new[] {
                new ChartMapPoint("Cairo", 31.2357, 30.0444),
                new ChartMapPoint("Cape Town", 18.4241, -33.9249)
            })
            .AddMapRouteBetweenPoints("Cairo to Cape Town", "Cairo", "Cape Town")
            .ToSvg();
        var cairoX = GetAttribute(verticalRouteSvg, "data-cfx-label=\"Cairo\"", "cx");
        var capeTownX = GetAttribute(verticalRouteSvg, "data-cfx-label=\"Cape Town\"", "cx");
        var controlX = GetAttribute(verticalRouteSvg, "data-cfx-label=\"Cairo to Cape Town\"", "data-cfx-control-x");
        Assert(Math.Abs(controlX - (cairoX + capeTownX) / 2) > 6, "Mostly north-south dotted-map routes should bow sideways instead of using a fragile vertical midpoint arc.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddMapConnector(" ", 0, 0, 1, 1), "Map connectors should reject empty labels.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddMapConnector("Bad", -181, 0, 1, 1), "Map connectors should reject invalid coordinates.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddMapRouteBetweenPoints("Bad", "Spain", "Warsaw"), "Point-bound map routes should require dotted map points to exist first.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddDottedMap("Visited", new[] { new ChartMapPoint("Spain", -3.7038, 40.4168) }).AddMapRouteBetweenPoints("Bad", "Spain", "Missing"), "Point-bound map routes should reject unknown point labels.");
    }
}
