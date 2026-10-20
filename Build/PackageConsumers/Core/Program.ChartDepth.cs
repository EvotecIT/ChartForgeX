using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

internal static partial class Program {
    private static void FinancialSessionLifecycle() {
        var friday = new DateTime(2026, 10, 16, 9, 0, 0, DateTimeKind.Utc);
        var monday = friday.AddDays(3);
        var prices = ChartSeries.CreateCandlestick(new ChartSeriesId("101"), "Prices", new[] {
            new ChartCandlestick(friday.AddHours(1), 102, 106, 100, 105),
            new ChartCandlestick(friday.AddHours(3), 105, 108, 102, 104),
            new ChartCandlestick(monday.AddHours(1), 108, 112, 106, 110)
        }).WithObservationIds(new[] { DepthId("1001"), DepthId("1002"), DepthId("1003") });
        var retainedInk = ChartColor.FromHex("#3987E5");
        ChartFinancialItemOptions? escaped = null;
        prices.ConfigureFinancialValue(1, options => {
            escaped = options; options.Stroke = retainedInk; options.StrokeWidth = 2.5;
        });
        escaped!.Stroke = ChartColor.FromHex("#FF0000");
        var range = ChartSeries.CreateRangeArea(new ChartSeriesId("102"), "Expected range",
            prices.FinancialValues.Select(value => new ChartRangeBand(value.X, 98, 118)));
        var reference = ChartSeries.CreateLine(new ChartSeriesId("103"), "Reference",
            prices.FinancialValues.Select(value => new ChartPoint(value.X, value.Close)));
        var chart = Chart.Create().WithSize(520, 340).WithHeader(false).WithLegend()
            .WithYAxisBounds(95, 125).WithXAxisTimeScale(TimeZoneInfo.Utc)
            .WithXAxisBounds(friday.ToOADate(), monday.AddHours(8).ToOADate())
            .ConfigureXAxis(axis => axis.WithTimeSessions(new ChartTimeSessionDomain(new[] {
                new ChartTimeSession(friday, friday.AddHours(8)), new ChartTimeSession(monday, monday.AddHours(8))
            }))).AddSeries(range).AddSeries(prices).AddSeries(reference);
        var prepared = chart.Prepare(DepthContext(chart, showLegend: true));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var projection = prepared.XProjection!;
        PackageAssertions.Require(projection.TimeSessions != null && Math.Abs(projection.Project(friday.AddHours(8).ToOADate())
            - projection.Project(monday.ToOADate())) < 1e-7, "Packed financial companions did not compress the declared closed session.");
        prices.ApplyFinancialChanges(new[] {
            ChartObservationChange<ChartCandlestick>.Remove(DepthId("1001")),
            ChartObservationChange<ChartCandlestick>.Replace(DepthId("1002"), new ChartCandlestick(friday.AddHours(3), 105, 109, 102, 107)),
            ChartObservationChange<ChartCandlestick>.Append(DepthId("1004"), new ChartCandlestick(monday.AddHours(3), 110, 116, 108, 114))
        }, maximumObservations: 3);
        var updated = XDocument.Parse(chart.ToSvg());
        var retained = updated.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point"
            && (string?)node.Attribute("data-cfx-series-key") == "101" && (string?)node.Attribute("data-cfx-observation-id") == "1002");
        PackageAssertions.Require(prices.ObservationIds[0].Value == "1002" && prices.FinancialValues[0].High == 109
            && retained.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "candlestick-body"
                && (string?)node.Attribute("stroke") == retainedInk.ToCss()),
            "Packed keyed price updates lost complete prices, numeric identity or detached datum styling.");
        PackageAssertions.Require(updated.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point")
            .Select(node => (string?)node.Attribute("data-cfx-series-key")).Distinct().Count() == 3,
            "Packed financial preparation lost its range or line companion.");
        PackageAssertions.Require(prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png),
            "A keyed source update changed a prepared financial export.");
        PackageAssertions.Png(png); PackageAssertions.Png(chart.ToPng());
    }

    private static void NativeCoordinateDrawings() {
        var chart = Chart.Create().WithSize(520, 340).WithHeader(false).WithLegend(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100)
            .AddLine("Measurements", new[] { new ChartPoint(0, 10), new ChartPoint(3, 40), new ChartPoint(6, 60), new ChartPoint(10, 90) });
        chart.Series[0].ConfigureMarkers(options => options.Enabled = true);
        chart.AddNote(new ChartPoint(3, 40), "Review", id: new ChartAnnotationId("review"))
            .AddLineSegment(new ChartPoint(3, 40), new ChartPoint(6, 60), "Plan", id: new ChartAnnotationId("plan"))
            .AddParallelChannel(new ChartPoint(2, 50), new ChartPoint(7, 65), 8, "Target", id: new ChartAnnotationId("target"));
        var prepared = chart.Prepare(DepthContext(chart, showLegend: false),
            new ChartCartesianViewport(new ChartXViewport(2, 8), new ChartYViewport(20, 80)));
        var x = prepared.XProjection!; var y = prepared.YProjection!;
        var anchor = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Single(node => node.Role == "annotation-anchor");
        PackageAssertions.Require(x.Minimum == 2 && x.Maximum == 8 && x.FullMinimum == 0 && x.FullMaximum == 10
            && y.Minimum == 20 && y.Maximum == 80 && y.FullMinimum == 0 && y.FullMaximum == 100
            && Math.Abs(anchor.Cx - x.Project(3)) < 1e-7 && Math.Abs(anchor.Cy - y.Project(40)) < 1e-7
            && prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Any(node => node.Role == "marker"
                && Math.Abs(node.Cx - anchor.Cx) < 1e-7 && Math.Abs(node.Cy - anchor.Cy) < 1e-7),
            "Packed XY preparation did not align source notes and native measurements on the same unchanged full domains.");
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        PackageAssertions.Require(PackageAssertions.Contains(svg, "data-cfx-channel-offset=\"8\"")
            && PackageAssertions.Contains(svg, "data-cfx-annotation-id=\"plan\"")
            && Math.Abs(y.Unproject(anchor.Cy) - 40) < 1e-7,
            "Packed source drawings lost authored channel/line facts or the detached inverse projection.");
        chart.Annotations.Clear(); chart.Series[0].Points[1] = new ChartPoint(3, 15);
        chart.Options.YAxis.Reversed = true;
        PackageAssertions.Require(prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png),
            "Source edits changed prepared XY drawings or native PNG output.");
        PackageAssertions.Png(png);
    }

    private static void GeographicDatasetLifecycle() {
        var options = new ChartMapGeoJsonOptions {
            Projection = ChartMapGeoJsonProjection.WebMercator, ValuePropertyName = "traffic", ColorValuePropertyName = "delay"
        };
        var definition = ChartMapGeoJson.ToMapDefinition("service", "Service area", DepthGeoJson, options);
        var imported = ChartMapGeoJson.ToMapPoints(DepthGeoJson, options);
        var routes = ChartMapGeoJson.ToMapConnectors(DepthGeoJson, options);
        PackageAssertions.Require(definition.GeographicProjection == ChartMapGeoJsonProjection.WebMercator
            && imported.Count == 2 && imported[0].Longitude == -3.7 && imported[0].Latitude == 40.4
            && routes.Count == 1 && routes[0].RoutePoints.Count == 3 && routes[0].RoutePoints[1].Longitude == 10,
            "Packed GeoJSON imports lost their declared polygon projection, raw point/route positions or source order.");
        var routeInput = imported.ToArray();
        var snapshotRoute = new ChartMapConnector("Dispatch facts", routeInput);
        routeInput[0] = new ChartMapPoint("Changed", 0, 0);
        var writableView = (IList<ChartMapPoint>)snapshotRoute.RoutePoints;
        var rejected = false;
        try { writableView[0] = routeInput[0]; } catch (NotSupportedException) { rejected = true; }
        PackageAssertions.Require(rejected && writableView.IsReadOnly && snapshotRoute.RoutePoints.Count == 2
            && snapshotRoute.RoutePoints[0].Longitude == -3.7 && snapshotRoute.RoutePoints[0].ColorValue == -5,
            "Packed route snapshots expose mutation or lost independent waypoint color facts.");
        var coverage = ChartSeries.CreateRegionMap(new ChartSeriesId("coverage"), "Coverage", definition,
            new[] { new ChartRegionMapItem("zone", 75) })
            .WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#1D4ED8")).WithValueRange(0, 100));
        coverage.ConfigureColorDimension(dimension => { dimension.Name = "Coverage"; dimension.ValueFormat = ChartValueFormat.Number("0 '%'", CultureInfo.InvariantCulture); });
        var depots = ChartSeries.CreateDottedMap(new ChartSeriesId("depots"), "Depot delays", imported)
            .WithObservationIds(new[] { DepthId("madrid"), DepthId("warsaw") })
            .WithColorScale(ChartColorScale.Diverging(ChartColor.FromHex("#0D9488"), ChartColor.FromHex("#E2E8F0"), ChartColor.FromHex("#EA580C"), 0).WithValueRange(-10, 30))
            .ReplaceMapConnectors(routes);
        depots.ConfigureColorDimension(dimension => { dimension.Name = "Delay"; dimension.ValueFormat = ChartValueFormat.Number("0 'min'", CultureInfo.InvariantCulture); });
        depots.ConfigureMapPointSize(size => {
            size.WithSizeDomain(0, 50); size.MinimumRadius = 3; size.MaximumRadius = 9;
            size.SizeDimension.Name = "Traffic"; size.SizeDimension.ShowLegend = true;
        });
        var chart = Chart.Create().WithSize(640, 500).WithHeader(false).WithLegend().WithMapLabels(false).WithDataLabels()
            .AddSeries(coverage).AddSeries(depots);
        var prepared = chart.Prepare(DepthContext(chart, showLegend: true));
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var xml = XDocument.Parse(svg);
        var source = xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-point-source"
            && (string?)node.Attribute("data-cfx-observation-id") == "warsaw");
        PackageAssertions.Require((string?)source.Attribute("data-cfx-value") == "100" && (string?)source.Attribute("data-cfx-color-value") == "20"
            && source.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-point" && (string?)node.Attribute("rx") == "9")
            && xml.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "map-color-scale-source") == 2
            && PackageAssertions.Contains(svg, "map-size-key-1") && PackageAssertions.Contains(svg, "Traffic") && PackageAssertions.Contains(svg, "Delay")
            && PackageAssertions.Contains(svg, "dotted-map-connector"),
            "Packed geographic composition lost independent numeric keys, authored size clamping, raw weight/color facts or routes.");
        chart.MoveSeries(depots, 0);
        depots.UpdateMapPoint(1, new ChartMapPoint("Warsaw", 21, 52.2, 100).WithColorValue(null));
        PackageAssertions.Require(depots.ObservationIds[1].Value == "warsaw" && depots.MapPoints[1].Value == 100
            && depots.MapPoints[1].ColorValue == null && depots.MapConnectors[0].RoutePoints.Count == 3,
            "Packed map update/order changed observation identity, missing color, weight or captured routes.");
        depots.ReplaceMapPoints(depots.MapPoints.ToArray());
        PackageAssertions.Require(depots.MapPoints[0].ColorValue == -5 && depots.MapPoints[1].ColorValue == null
            && prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png),
            "Packed typed replacement conflated color/weight or changed an earlier geographic export.");
        PackageAssertions.Png(png); PackageAssertions.Png(chart.ToPng());
    }

    private static ChartObservationId DepthId(string value) => new ChartObservationId(value);

    private static VisualRenderContext DepthContext(Chart chart, bool showLegend) => new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(chart.Options.Size.Width, chart.Options.Size.Height)),
        chart.Options.Theme, chart.Options.ThemeMode, new VisualFrame(showLegend: showLegend));

    private const string DepthGeoJson = """
        {"type":"FeatureCollection","features":[
          {"type":"Feature","properties":{"code":"zone","name":"Service area"},"geometry":{"type":"Polygon","coordinates":[[[-10,35],[30,35],[30,65],[-10,65],[-10,35]]]}},
          {"type":"Feature","properties":{"name":"Madrid","traffic":8,"delay":-5},"geometry":{"type":"Point","coordinates":[-3.7,40.4]}},
          {"type":"Feature","properties":{"name":"Warsaw","traffic":100,"delay":20},"geometry":{"type":"Point","coordinates":[21,52.2]}},
          {"type":"Feature","properties":{"name":"Delivery"},"geometry":{"type":"LineString","coordinates":[[-3.7,40.4],[10,45],[21,52.2]]}}
        ]}
        """;
}
