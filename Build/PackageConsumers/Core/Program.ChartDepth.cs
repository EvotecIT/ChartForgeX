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
        var readings = chart.Series[0].WithObservationIds(new[] { DepthId("first"), DepthId("middle"), DepthId("later"), DepthId("last") })
            .WithPointLabel(1, "Retained measurement").WithPointColor(1, ChartColor.FromHex("#3987E5"));
        readings.ConfigureMarkers(options => options.Enabled = true);
        chart.AddNote(new ChartPoint(3, 40), "Review", id: new ChartAnnotationId("review"))
            .AddLineSegment(new ChartPoint(3, 40), new ChartPoint(6, 60), "Plan", id: new ChartAnnotationId("plan"))
            .AddParallelChannel(new ChartPoint(2, 50), new ChartPoint(7, 65), 8, "Target", id: new ChartAnnotationId("target"));
        var anonymous = new ChartAnnotation(ChartAnnotationKind.HorizontalLine, 50, null, "Anonymous guide", ChartColor.FromHex("#64748B"), 1, showLabel: false);
        chart.AddAnnotation(anonymous);
        var context = DepthContext(chart, showLegend: false);
        Func<VisualRenderContext, PreparedVisual> prepare = chart.Prepare;
        var full = chart.Prepare(context, null);
        PackageAssertions.Require(full.ToSvg() == prepare(context).ToSvg() && full.ToSvg() == ((IVisualRenderable)chart).Prepare(context).ToSvg(),
            "Packed full-domain null preparation disagreed with public method-group or IVisualRenderable dispatch.");
        foreach (var viewport in new[] { new ChartCartesianViewport(x: new ChartAxisWindow(2, 8)), new ChartCartesianViewport(y: new ChartAxisWindow(20, 80)) }) {
            var singleAxis = chart.Prepare(context, viewport);
            PackageAssertions.Require(singleAxis.XProjection!.Minimum == (viewport.X?.Minimum ?? 0)
                && singleAxis.XProjection.Maximum == (viewport.X?.Maximum ?? 10)
                && singleAxis.YProjection!.Minimum == (viewport.Y?.Minimum ?? 0)
                && singleAxis.YProjection.Maximum == (viewport.Y?.Maximum ?? 100)
                && XDocument.Parse(singleAxis.ToSvg()).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point") == 4,
                "Packed single-axis Cartesian input changed an omitted full domain or dropped source observations.");
        }
        var prepared = chart.Prepare(context,
            new ChartCartesianViewport(new ChartAxisWindow(2, 8), new ChartAxisWindow(20, 80)));
        var x = prepared.XProjection!; var y = prepared.YProjection!;
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var xml = XDocument.Parse(svg);
        var anchor = xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "annotation-anchor");
        var anchorX = anchor.Attribute("cx")!.Value;
        var anchorY = anchor.Attribute("cy")!.Value;
        PackageAssertions.Require(x.Minimum == 2 && x.Maximum == 8 && x.FullMinimum == 0 && x.FullMaximum == 10
            && y.Minimum == 20 && y.Maximum == 80 && y.FullMinimum == 0 && y.FullMaximum == 100
            // SVG ink uses three fractional decimal places; compare its serialized positions exactly.
            && anchorX == x.Project(3).ToString("0.###", CultureInfo.InvariantCulture)
            && anchorY == y.Project(40).ToString("0.###", CultureInfo.InvariantCulture)
            && xml.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "marker"
                && (string?)node.Attribute("cx") == anchorX && (string?)node.Attribute("cy") == anchorY),
            "Packed XY preparation did not align source notes and native measurements on the same unchanged full domains.");
        PackageAssertions.Require(PackageAssertions.Contains(svg, "data-cfx-channel-offset=\"8\"")
            && PackageAssertions.Contains(svg, "data-cfx-annotation-id=\"plan\"")
            && Math.Abs(x.Unproject(x.Project(3)) - 3) < 1e-7
            && Math.Abs(y.Unproject(y.Project(40)) - 40) < 1e-7,
            "Packed source drawings lost authored channel/line facts or the detached inverse projection.");
        PackageAssertions.Require(chart.Prepare(context, null).ToSvg() == full.ToSvg()
            && chart.Options.XAxis.Minimum == 0 && chart.Options.XAxis.Maximum == 10 && chart.Options.YAxis.Minimum == 0 && chart.Options.YAxis.Maximum == 100,
            "Packed null reset failed to recover the full scene or changed authored axis policy.");
        NumericDepthTransaction(readings);
        OwnedDepthAnnotations(chart, anonymous);
        chart.Options.YAxis.Reversed = true;
        PackageAssertions.Require(prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png),
            "Source edits changed prepared XY drawings or native PNG output.");
        PackageAssertions.Png(png);
        Console.WriteLine("Core authoring facts: full/X/Y/XY and plain-null reset agree; generic numeric atomicity, read-only annotations and detached exports passed.");
    }

    private static void NumericDepthTransaction(ChartSeries readings) {
        var points = readings.Points.ToArray();
        var keys = readings.ObservationIds.OrderBy(entry => entry.Key).ToArray();
        var labels = readings.PointLabels.ToArray();
        var colors = readings.PointColors.ToArray();
        var rejected = false;
        try {
            readings.ApplyPointChanges(new[] {
                ChartObservationChange<ChartPoint>.Replace(DepthId("middle"), new ChartPoint(3, 999)),
                ChartObservationChange<ChartPoint>.Append(DepthId("last"), new ChartPoint(10, 999))
            });
        } catch (ArgumentException) { rejected = true; }
        PackageAssertions.Require(rejected && readings.Points.SequenceEqual(points)
            && readings.ObservationIds.OrderBy(entry => entry.Key).SequenceEqual(keys)
            && readings.PointLabels.SequenceEqual(labels)
            && readings.PointColors.SequenceEqual(colors),
            "Packed invalid generic numeric transaction partially changed source values, identity or presentation.");
        readings.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Remove(DepthId("first")),
            ChartObservationChange<ChartPoint>.Replace(DepthId("middle"), new ChartPoint(3, 45, true).WithColorValue(-7)),
            ChartObservationChange<ChartPoint>.Append(DepthId("new"), new ChartPoint(10, 95))
        }, maximumObservations: 4);
        PackageAssertions.Require(readings.Points.Count == 4 && readings.Points[0].Y == 45 && readings.Points[0].BreakBefore
            && readings.Points[0].ColorValue == -7 && readings.PointLabels[0] == "Retained measurement" && readings.PointColors[0].HasValue
            && readings.ObservationIds.OrderBy(entry => entry.Key).Select(entry => entry.Value.Value).SequenceEqual(new[] { "middle", "later", "last", "new" }),
            "Packed generic numeric transaction lost complete values, source order, surviving identity or presentation.");
    }

    private static void OwnedDepthAnnotations(Chart chart, ChartAnnotation anonymous) {
        var view = chart.Annotations;
        var list = (IList<ChartAnnotation>)view;
        var rejected = false;
        try { list.RemoveAt(0); } catch (NotSupportedException) { rejected = true; }
        PackageAssertions.Require(rejected && list.IsReadOnly && view.Count == 4 && ReferenceEquals(view, chart.Annotations),
            "Packed annotation membership is writable or the retained public view is unstable.");
        var reviewed = ChartAnnotation.Note(new ChartPoint(4, 45), "Reviewed", ChartColor.FromHex("#3987E5"), new ChartAnnotationId("review"));
        var replacementGuide = new ChartAnnotation(ChartAnnotationKind.HorizontalLine, 55, null, "Updated anonymous guide", ChartColor.FromHex("#64748B"), 1, showLabel: false);
        chart.ReplaceAnnotation(reviewed).ReplaceAnnotation(anonymous, replacementGuide);
        PackageAssertions.Require(ReferenceEquals(view[0], reviewed) && ReferenceEquals(view[3], replacementGuide) && !replacementGuide.Id.HasValue,
            "Packed keyed or anonymous replacement lost its identity, paint order or retained view.");
        rejected = false;
        try { chart.AddAnnotation(ChartAnnotation.Note(new ChartPoint(5, 50), "Duplicate", reviewed.Color, reviewed.Id)); }
        catch (InvalidOperationException) { rejected = true; }
        PackageAssertions.Require(rejected && view.Count == 4 && ReferenceEquals(view[0], reviewed) && ReferenceEquals(view[3], replacementGuide),
            "Packed duplicate annotation ID rejection changed installed membership or order.");
        chart.RemoveAnnotation(new ChartAnnotationId("review")).RemoveAnnotation(replacementGuide);
        PackageAssertions.Require(view.Count == 2 && view.All(annotation => annotation.Id?.Value is "plan" or "target"),
            "Packed keyed or anonymous removal removed a different annotation.");
        chart.ClearAnnotations();
        PackageAssertions.Require(view.Count == 0 && ReferenceEquals(view, chart.Annotations), "Packed clear detached or retained stale annotation membership.");
    }

    private static void GeographicDatasetLifecycle() {
        var options = new ChartMapGeoJsonOptions {
            Projection = ChartMapGeoJsonProjection.WebMercator, WeightPropertyName = "traffic", ColorValuePropertyName = "delay",
            MaximumObservations = 3, MaximumExpandedLabelCharacters = 256
        };
        var definition = ChartMapGeoJson.ToMapDefinition("service", "Service area", DepthGeoJson, options);
        var imported = ChartMapGeoJson.ToMapPoints(DepthGeoJson, options);
        var routes = ChartMapGeoJson.ToMapConnectors(DepthGeoJson, options);
        PackageAssertions.Require(definition.GeographicProjection == ChartMapGeoJsonProjection.WebMercator
            && imported.Count == 2 && imported[0].Longitude == -3.7 && imported[0].Latitude == 40.4
            && imported[0].Weight == 8 && imported[0].ColorValue == -5 && imported[1].Weight == 100 && imported[1].ColorValue == 20
            && routes.Count == 1 && routes[0].RoutePoints.Count == 3 && routes[0].RoutePoints[1].Longitude == 10,
            "Packed GeoJSON imports lost their declared polygon projection, raw point/route positions or source order.");
        options.MaximumObservations = 1;
        var limitedImportRejected = false;
        try { ChartMapGeoJson.ToMapPoints(DepthGeoJson, options); }
        catch (ArgumentException error) { limitedImportRejected = error.ParamName == "geoJson"; }
        PackageAssertions.Require(limitedImportRejected && imported.Count == 2 && imported[0].Weight == 8
            && imported[1].ColorValue == 20 && routes[0].RoutePoints.Count == 3,
            "Packed GeoJSON observation limits failed to reject expanded output or changed earlier imported facts.");
        Console.WriteLine("Map import resource limits rejected excess observations while retaining prior point and route facts.");
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
        depots.UpdateMapPoint(1, new ChartMapPoint("Warsaw", 21, 52.2, weight: 100).WithColorValue(null));
        PackageAssertions.Require(depots.ObservationIds[1].Value == "warsaw" && depots.MapPoints[1].Weight == 100
            && depots.MapPoints[1].ColorValue == null && depots.MapConnectors[0].RoutePoints.Count == 3,
            "Packed map update/order changed observation identity, missing color, weight or captured routes.");
        depots.ReplaceMapPoints(depots.MapPoints.ToArray());
        PackageAssertions.Require(depots.MapPoints[0].ColorValue == -5 && depots.MapPoints[1].ColorValue == null
            && prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png),
            "Packed typed replacement conflated color/weight or changed an earlier geographic export.");
        PackageAssertions.Png(png); PackageAssertions.Png(chart.ToPng());
        Console.WriteLine("Map authoring facts: named weight and imported WeightPropertyName remain independent from signed or missing color values.");
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
