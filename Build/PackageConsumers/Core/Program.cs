using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

internal static partial class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
        ThemeSnapshots();
        TypedSeriesLifecycle();
        NumericOwnershipAndWindowDimensions();
        CallbackOwnership();
        PolarPaint();
        ReversedStackCaptionLanes();
        FinancialSessionLifecycle();
        NativeCoordinateDrawings();
        HierarchySubtreeContracts();
        ConcentricPieBands();
        GeographicDatasetLifecycle();
        PackageAssertions.Payload("ChartForgeX");
        Console.WriteLine("Core package boundaries, typed ownership, themes and static exports passed.");
    }

    private static void ReversedStackCaptionLanes() {
        var chart = Chart.Create().WithSize(360, 360).WithHeader(false).WithLegend(false)
            .WithXLabels("North", "South", "East", "West").WithStackTotals().WithDataLabels(false);
        for (var index = 0; index < 4; index++) {
            chart.AddSeries(ChartSeries.CreateBar(new ChartSeriesId("stack-" + index), "Measure " + index,
                ChartPoints.FromValues(40 + index, 50 + index, 60 + index, 70 + index))
                .WithStackGroup(index < 2 ? "first" : "second").WithNormalization(100));
        }
        chart.Options.YAxis.WithBounds(0, 100).WithReversal()
            .WithLabelFormatter(value => value.ToString(CultureInfo.InvariantCulture) + "%");
        var texts = XDocument.Parse(chart.ToSvg()).Descendants().Where(node => node.Name.LocalName == "text").ToArray();
        var categories = texts.Where(node => new[] { "North", "South", "East", "West" }.Contains(node.Value)).ToArray();
        var totals = texts.Where(node => node.Value == "100").ToArray();
        PackageAssertions.Require(categories.Length == 4 && totals.Length == 8,
            "Packed grouped stacks lost category captions or complete normalized totals.");
        PackageAssertions.Require(categories.All(category => totals.All(total =>
            double.Parse(category.Attribute("y")!.Value, CultureInfo.InvariantCulture)
                - double.Parse(total.Attribute("y")!.Value, CultureInfo.InvariantCulture) >= 10)),
            "Packed reversed stacks put category captions in the total-label lane.");
        PackageAssertions.Png(chart.ToPng());
    }

    private static void ThemeSnapshots() {
        var canonical = VisualTheme.Graphite();
        var dark = canonical.Resolve(VisualThemeMode.Dark);
        PackageAssertions.Require(dark.Background.Equals(ChartColor.FromHex("#0c0d10"))
            && dark.ElevatedSurface.Equals(ChartColor.FromHex("#16181c")) && dark.Palette[0].Equals(ChartColor.FromHex("#3987e5")),
            "The packed canonical dark palette does not match the shared theme resource.");
        var original = canonical.ToJson();
        VisualThemeBuilder? retained = null;
        var custom = canonical.Configure(builder => {
            retained = builder;
            builder.Light.Accent = ChartColor.FromHex("#123456"); builder.Dark.Accent = ChartColor.FromHex("#abcdef");
            builder.Typography = new VisualTypography("Arial, sans-serif", 21, 15, 11, 12, 10, 31, 18, 14, "Consolas, monospace");
            builder.Spacing = 17; builder.SeriesStrokeWidth = 4; builder.MarkerRadius = 5; builder.AreaOpacity = .27;
            builder.BarRadius = 6; builder.GridStrokeWidth = 2; builder.AxisStrokeWidth = 3;
            builder.CardRadius = 8; builder.PlotRadius = 9; builder.CardShadowOpacity = .24;
            builder.CardShadowColor = ChartColor.FromHex("#12345678"); builder.GaugeStrokeWidth = 19; builder.GaugeBandWidth = 7;
        });
        var json = custom.ToJson();
        var roundTrip = VisualTheme.FromJson(json);
        PackageAssertions.Require(roundTrip.ToJson() == json && canonical.ToJson() == original,
            "Complete paired theme JSON did not preserve colors, typography, geometry and effects.");
        retained!.Dark.Accent = ChartColor.FromHex("#ff0000"); retained.Spacing = 99;
        PackageAssertions.Require(custom.ToJson() == json, "A retained theme builder changed its immutable result.");
        var chart = PackageAssertions.Chart().WithTheme(roundTrip, VisualThemeMode.Dark);
        chart.Options.TransparentBackground = false;
        PackageAssertions.Require(ReferenceEquals(chart.Options.Theme, roundTrip) && chart.Options.ThemeMode == VisualThemeMode.Dark
            && PackageAssertions.Contains(chart.ToSvg(), roundTrip.Resolve(VisualThemeMode.Dark).Background.ToCss()),
            "Explicit dark appearance did not use the packed paired theme.");
    }

    private static void TypedSeriesLifecycle() {
        var input = new[] { new ChartBubble(1, 3, 5).WithColorValue(7) };
        var actual = ChartSeries.CreateBubble(new ChartSeriesId("actual"), "Reading", input)
            .WithObservationId(0, new ChartObservationId("row-a")).WithPointColor(0, ChartColor.FromHex("#123456"));
        var forecast = ChartSeries.CreateBubble(new ChartSeriesId("forecast"), "Reading", new[] { new ChartBubble(2, 6, 8) });
        var chart = Chart.Create().WithSize(320, 200).WithLegend(false).AddSeries(actual).AddSeries(forecast);
        input[0] = new ChartBubble(9, 9, 9);
        PackageAssertions.Require(actual.Bubbles[0].Y == 3 && actual.ObservationCount == 1 && actual.SourcePointCount == 1
            && actual.Points.Count == 0 && actual.Points.IsReadOnly, "Typed bubble ingestion leaked numeric slots or retained the input array.");
        chart.MoveSeries(actual, 1);
        actual.WithPointLabel(0, "Retained reading").UpdateBubble(0, new ChartBubble(1, 9, 11).WithColorValue(13));
        PackageAssertions.Require(ReferenceEquals(chart.Series[1], actual) && chart.Series[0].Name == actual.Name
            && actual.Id.Value == "actual" && actual.ObservationIds[0].Value == "row-a" && actual.PointColors[0].HasValue
            && actual.Bubbles[0].ColorValue == 13, "Reorder or typed update changed stable identity, independent facts or observation paint.");
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 200)), frame: new VisualFrame(showLegend: false)));
        var capturedSvg = prepared.ToSvg();
        var source = XDocument.Parse(capturedSvg).Descendants().Single(element =>
            (string?)element.Attribute("data-cfx-role") == "point" && (string?)element.Attribute("data-cfx-series-key") == "actual");
        PackageAssertions.Require((string?)source.Attribute("data-cfx-size") == "11"
            && (string?)source.Attribute("data-cfx-source-count") == "1" && (string?)source.Attribute("data-cfx-source-point") == "0"
            && (string?)source.Attribute("data-cfx-observation-id") == "row-a", "Packed typed rendering lost logical source facts or explicit identity.");
        actual.ReplaceBubbles(new[] { new ChartBubble(1, 4, 6), new ChartBubble(2, 5, 7) });
        PackageAssertions.Require(actual.ObservationCount == 2 && actual.SourcePointCount == 2 && actual.Id.Value == "actual"
            && actual.ObservationIds.Count == 0 && actual.PointColors.Count == 0 && actual.PointLabels.Count == 0,
            "Typed replacement retained stale observation metadata or configuration-slot counts.");
        PackageAssertions.Require(prepared.ToSvg() == capturedSvg, "Typed replacement changed an already prepared artifact.");
        PackageAssertions.Png(prepared.ToPng()); PackageAssertions.Png(chart.ToPng());
    }

    private static void NumericOwnershipAndWindowDimensions() {
        var origin = new DateTime(2026, 1, 28).ToOADate();
        var readings = ChartSeries.CreateLine(new ChartSeriesId("readings"), "Readings",
            new[] { new ChartPoint(origin, 2), new ChartPoint(origin + 10, 8) })
            .WithObservationId(1, new ChartObservationId("last"));
        readings.Points.Insert(0, new ChartPoint(origin - 1, 1));
        PackageAssertions.Require(readings.SourcePointCount == 3 && readings.ObservationIds[2].Value == "last"
            && readings.Points[2].X == origin + 10, "Packed numeric insertion detached an identity or source count.");
        var chart = Chart.Create().WithSize(420, 280).WithXAxisScale(ChartScaleKind.Time).AddSeries(readings)
            .AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("volume"), "Volume", new[] { new ChartBubble(origin + 5, 4, 9) })
                .ConfigureMarkers(markers => markers.Shape = ChartMarkerShape.Star))
            .ConfigureBubble(options => options.SizeEncoding = ChartBubbleSizeEncoding.Area);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 280))), new ChartCartesianViewport(x: new ChartAxisWindow(origin + 2, origin + 8)));
        PackageAssertions.Require(prepared.XProjection != null && prepared.XProjection.Scale == ChartScaleKind.Time
            && prepared.XProjection.Minimum == origin + 2, "Packed time/Bubble window lost its native projection.");
        PackageAssertions.Png(prepared.ToPng());
        var derived = ChartSeries.CreateTrendLine(new ChartSeriesId("fit"), "Fit", readings.Points);
        PackageAssertions.Require(derived.Points.IsReadOnly, "Packed derived points permit provenance-changing mutation.");
        var radar = Chart.Create().AddRadar("Categories", new[] { new ChartPoint(1, 3), new ChartPoint(2, 3), new ChartPoint(3, 3) })
            .AddRadar("Observed", new[] { new ChartPoint(1, 2), new ChartPoint(3, 4) })
            .WithXLabels("First", "Missing", "Third");
        radar.Series[1].ConfigureRadar(options => options.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Gap);
        PackageAssertions.Png(radar.ToPng());
    }

    private static void CallbackOwnership() {
        var chart = PackageAssertions.Chart();
        ChartAxis? retained = null;
        chart.ConfigureXAxis(axis => {
            retained = axis; axis.WithBounds(0, 4); axis.Labels.Add(new ChartAxisLabel(1, "One"));
        });
        var svg = chart.ToSvg();
        retained!.Maximum = 20; retained.Labels.Clear();
        PackageAssertions.Require(chart.Options.XAxis.Maximum == 4 && chart.Options.XAxis.Labels.Count == 1 && chart.ToSvg() == svg,
            "A retained axis callback candidate changed installed scalar or nested label state.");
        var threw = false;
        try {
            chart.ConfigureXAxis(axis => {
                axis.Minimum = 1; axis.Labels.Add(new ChartAxisLabel(2, "Two"));
                throw new InvalidOperationException("Expected callback failure.");
            });
        } catch (InvalidOperationException) { threw = true; }
        PackageAssertions.Require(threw && chart.Options.XAxis.Minimum == 0 && chart.Options.XAxis.Labels.Count == 1 && chart.ToSvg() == svg,
            "A throwing axis callback partially installed its candidate.");
    }
    private static void PolarPaint() {
        var red = ChartColor.FromHex("#DC2626");
        var chart = Chart.Create().WithSize(520, 380).WithHeader(false).WithLegend(true)
            .AddRadarArea("Signal", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40), new ChartPoint(3, 30) });
        chart.Options.PolarAxisStroke = new ChartStrokeStyle(width: 3);
        chart.Options.PolarGridStroke = new ChartStrokeStyle(width: 0);
        chart.Options.PolarGridFill = ChartColor.FromHex("#64748B");
        chart.Options.PolarGridFillOpacity = .1;
        chart.Series[0].ConfigureRadar(options => options.OutlineStroke = new ChartStrokeStyle(red, 4));
        chart.WithSvgColorVariables(new SvgColorVariables().Add("--outline", red, SvgColorRole.Series));
        var outline = XDocument.Parse(chart.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "radar-outline");
        PackageAssertions.Require((string?)outline.Attribute("stroke") == "#DC2626", "Packed radar outline lost literal paint provenance.");
        PackageAssertions.Png(chart.ToPng());
    }
}
