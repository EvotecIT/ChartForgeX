using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Numeric size keys describe the final shared Bubble geometry without becoming selectable observations.</summary>
public sealed class BubbleSizeKeyTests {
    [Fact]
    public void SizeKeyVisibilityIsIndependentOfCategoricalEntriesAndEmptySourcesHaveNoInventedDomain() {
        var chart = Bare().AddBubble("Requests", new[] { new ChartBubble(2, 20, 25), new ChartBubble(8, 80, 100) });
        Assert.Empty(Groups(Prepare(chart), "bubble-size-key"));
        chart.ConfigureBubble(options => options.SizeDimension.ShowLegend = true);
        chart.Series[0].ShowInLegend = false;
        Assert.Single(Groups(Prepare(chart), "bubble-size-key"));
        chart.WithLegend(false);
        Assert.Empty(Groups(Prepare(chart), "bubble-size-key"));
        chart.WithLegend();
        var hiddenFrame = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(960, 600)),
            frame: new VisualFrame(showLegend: false));
        Assert.Empty(Groups(chart.Prepare(hiddenFrame), "bubble-size-key"));
        chart.ConfigureBubble(options => options.SizeDimension.ShowLegend = false);
        Assert.Empty(Groups(Prepare(chart), "bubble-size-key"));
        chart.ConfigureBubble(options => { options.SizeDimension.ShowLegend = true; options.WithSizeDomain(0, 100); });
        chart.Series[0].ReplaceBubbles(Array.Empty<ChartBubble>());
        Assert.Empty(Groups(Prepare(chart), "bubble-size-key"));
    }

    [Fact]
    public void OneKeyUsesEverySeriesSourceDomainIncludingHiddenSecondaryObservations() {
        var chart = Bare().AddBubble("First", new[] { new ChartBubble(1, 20, 1), new ChartBubble(3, 40, 200.5) })
            .AddBubble("Second", new[] { new ChartBubble(5, 60, 200.5) })
            .AddBubble("Hidden extent", new[] { new ChartBubble(7, 80, 400) });
        chart.Series[2].UseSecondaryYAxis().ConfigureMarkers(options => options.Enabled = false);
        chart.Series[2].ShowInLegend = false;
        chart.Options.SecondaryYAxis.WithBounds(0, 100);
        chart.AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("empty"), "Empty", Array.Empty<ChartBubble>()));
        chart.ConfigureBubble(options => {
            options.MinimumRadius = 2; options.MaximumRadius = 30; options.SizeDimension.ShowLegend = true;
        });
        var prepared = Prepare(chart);
        Assert.Single(Groups(prepared, "bubble-size-key"));
        var samples = Samples(prepared);
        Assert.Equal(1, samples.Min(sample => sample.Value));
        Assert.Equal(400, samples.Max(sample => sample.Value));
        Assert.Equal(2, samples.Single(sample => sample.Value == 1).Radius);
        Assert.Equal(30, samples.Single(sample => sample.Value == 400).Radius);
        var marks = Marks(prepared);
        Assert.Equal(3, marks.Length);
        Assert.Equal(marks[1].Rx, marks[2].Rx, 8);
        Assert.Equal(marks[1].Rx, samples.Single(sample => sample.Value == 200.5).Radius, 8);
        Assert.Equal(4, Groups(prepared, "point").Length);
        AssertNonDataKey(prepared);
    }

    [Fact]
    public void SizeNameAndFormatStayIndependentOfYColorAndIdentity() {
        var series = ChartSeries.CreateBubble(new ChartSeriesId("service-requests"), "Service", new[] {
            new ChartBubble(2, 12.5, 25).WithColorValue(3.125)
        }).WithObservationId(0, new ChartObservationId("request-batch"));
        var chart = Bare().AddSeries(series).WithDataLabels()
            .WithValueFormat(ChartValueFormat.Number("0.00 'ms'"));
        chart.ConfigureBubble(options => {
            options.SizeDimension.Name = "Requests";
            options.SizeDimension.ValueFormat = ChartValueFormat.Number("0.0 'req'");
            options.SizeDimension.ShowLegend = true;
        });
        series.WithColorScale(ChartColorScale.Sequential(ChartColor.Black, ChartColor.White))
            .ConfigureColorDimension(options => {
                options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Number("0.000 '°C'");
            });
        var prepared = Prepare(chart);
        var point = Assert.Single(Groups(prepared, "point"));
        Assert.Equal("2", point.Metadata["data-cfx-x"]);
        Assert.Equal("12.5", point.Metadata["data-cfx-y"]);
        Assert.Equal("25", point.Metadata["data-cfx-size"]);
        Assert.Equal("3.125", point.Metadata["data-cfx-color-value"]);
        Assert.Equal("Requests", point.Metadata["data-cfx-label-size"]);
        Assert.Equal("25.0 req", point.Metadata["data-cfx-formatted-size"]);
        Assert.Equal("25.0 req", point.Metadata["data-cfx-label"]);
        Assert.Equal("3.125 °C", point.Metadata["data-cfx-formatted-color-value"]);
        Assert.Equal("service-requests", point.Metadata["data-cfx-series-key"]);
        Assert.Equal("request-batch", point.Metadata["data-cfx-observation-id"]);
        Assert.Equal("Requests", Texts(prepared, "bubble-size-key-title").Single());
        Assert.Equal(new[] { "25.0 req" }, Texts(prepared, "bubble-size-key-label"));
        Assert.Contains("25.0 req", Texts(prepared, "data-label"));
        Assert.Contains("Temperature: 3.125 °C", prepared.Regions.Single(region => region.Role == "point").Label);
        Assert.DoesNotContain("ms", string.Join(" ", Texts(prepared, "bubble-size-key-label")), StringComparison.Ordinal);
        AssertNonDataKey(prepared);
    }

    [Fact]
    public void NullNameAndFormatRestoreLocalizedDefaultsWithoutChangingSourceFacts() {
        var chart = Bare().AddBubble("Service", new[] { new ChartBubble(2, 20, 25) })
            .WithValueFormat(ChartValueFormat.Number("0.00 'items'"))
            .ConfigureLabels(labels => labels.Size = "Wielkość");
        chart.ConfigureBubble(options => {
            options.SizeDimension.Name = "Requests"; options.SizeDimension.ShowLegend = true;
            options.SizeDimension.ValueFormat = ChartValueFormat.Number("0.0 'req'");
        });
        chart.ConfigureBubble(options => { options.SizeDimension.Name = null; options.SizeDimension.ValueFormat = null; });
        var prepared = Prepare(chart);
        Assert.Equal("Wielkość", Texts(prepared, "bubble-size-key-title").Single());
        Assert.Equal(new[] { "25.00 items" }, Texts(prepared, "bubble-size-key-label"));
        var point = Assert.Single(Groups(prepared, "point"));
        Assert.Equal("Wielkość", point.Metadata["data-cfx-label-size"]);
        Assert.Equal("25.00 items", point.Metadata["data-cfx-label"]);
        Assert.Equal("25", point.Metadata["data-cfx-size"]);
    }

    [Fact]
    public void AtomicBubbleConfigurationClonesNestedPresentationAndPreparedExportsStayDetached() {
        var chart = Bare().AddBubble("Service", new[] { new ChartBubble(2, 20, 25), new ChartBubble(8, 80, 100) });
        var original = chart.Options.Bubble;
        ChartBubbleOptions? retainedCandidate = null;
        chart.ConfigureBubble(options => {
            retainedCandidate = options; options.SizeDimension.Name = "Requests";
            options.SizeDimension.ShowLegend = true; options.SizeDimension.ValueFormat = ChartValueFormat.Number("0.0 'req'");
        });
        var installed = chart.Options.Bubble;
        Assert.NotSame(original.SizeDimension, installed.SizeDimension);
        Assert.NotSame(retainedCandidate!.SizeDimension, installed.SizeDimension);
        Assert.Null(original.SizeDimension.Name); Assert.False(original.SizeDimension.ShowLegend);
        retainedCandidate.SizeDimension.Name = "Retained callback";
        retainedCandidate.SizeDimension.ShowLegend = false;
        Assert.Equal("Requests", installed.SizeDimension.Name); Assert.True(installed.SizeDimension.ShowLegend);
        var failure = new InvalidOperationException("Reject this candidate.");
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => chart.ConfigureBubble(options => {
            options.SizeDimension.Name = "Failed callback"; options.SizeDimension.ShowLegend = false; throw failure;
        })));
        Assert.Same(installed, chart.Options.Bubble);
        Assert.Equal("Requests", installed.SizeDimension.Name); Assert.True(installed.SizeDimension.ShowLegend);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        installed.SizeDimension.Name = "Later source";
        installed.SizeDimension.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException("Late formatter."));
        installed.SizeDimension.ShowLegend = false;
        chart.Series[0].UpdateBubble(0, new ChartBubble(2, 20, 500));
        chart.ConfigureLabels(labels => labels.Size = "Later label");
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal("Requests", Texts(prepared, "bubble-size-key-title").Single());
        Assert.Contains("25.0 req", Texts(prepared, "bubble-size-key-label"));
    }

    [Fact]
    public void FormatterSideEffectsCannotChangeCapturedKeyAndEachSizeIsFormattedOnce() {
        var chart = Bare().WithDataLabels().AddBubble("First", new[] { new ChartBubble(2, 20, 25), new ChartBubble(4, 40, 100) })
            .AddBubble("Second", new[] { new ChartBubble(6, 60, 25) });
        var calls = new Dictionary<double, int>();
        chart.ConfigureBubble(options => {
            options.SizeDimension.Name = "Requests"; options.SizeDimension.ShowLegend = true;
            options.SizeDimension.ValueFormat = ChartValueFormat.Custom(value => {
                calls[value] = calls.GetValueOrDefault(value) + 1;
                chart.Options.Bubble.SizeDimension.Name = "Changed during preparation";
                chart.Options.Bubble.SizeDimension.ShowLegend = false;
                chart.Options.Bubble.SizeDimension.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException("Uncaptured policy."));
                return value.ToString("0.0", CultureInfo.InvariantCulture) + " req";
            });
        });
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Single(Groups(prepared, "bubble-size-key"));
        Assert.Equal("Requests", Texts(prepared, "bubble-size-key-title").Single());
        Assert.All(Groups(prepared, "point"), point => Assert.Equal("Requests", point.Metadata["data-cfx-label-size"]));
        Assert.Contains(25, calls.Keys); Assert.Contains(100, calls.Keys);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        var capturedCalls = calls.ToArray();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(capturedCalls, calls.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredDomainKeyUsesClampedLogicalRadiiWithoutChangingRawFacts(bool reversed) {
        var chart = Bare().AddBubble("Service", new[] {
            new ChartBubble(2, 20, 5), new ChartBubble(5, 50, 30), new ChartBubble(8, 80, 100)
        }).ConfigureBubble(options => {
            options.WithSizeDomain(10, 50); options.MinimumRadius = 2; options.MaximumRadius = 18;
            options.Reversed = reversed; options.SizeDimension.ShowLegend = true;
        });
        var prepared = Prepare(chart); var samples = Samples(prepared); var marks = Marks(prepared);
        Assert.Equal(new[] { "5", "30", "100" }, Groups(prepared, "point").Select(point => point.Metadata["data-cfx-size"]));
        Assert.Equal(reversed ? 18 : 2, samples.Single(sample => sample.Value == 10).Radius);
        Assert.Equal(reversed ? 2 : 18, samples.Single(sample => sample.Value == 50).Radius);
        Assert.Equal(marks[0].Rx, samples.Single(sample => sample.Value == 10).Radius, 8);
        Assert.Equal(marks[2].Rx, samples.Single(sample => sample.Value == 50).Radius, 8);
        Assert.Equal(marks[1].Rx, samples.Single(sample => sample.Value == 30).Radius, 8);
        AssertNonDataKey(prepared);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstantAndZeroRadiusKeysKeepHonestCaptionsWithoutDataTargets(bool zeroRadius) {
        var chart = Bare().AddBubble("Service", new[] { new ChartBubble(2, 20, 40), new ChartBubble(8, 80, 40) })
            .ConfigureBubble(options => {
                options.MinimumRadius = zeroRadius ? 0 : 2; options.MaximumRadius = zeroRadius ? 0 : 30;
                options.SizeDimension.Name = "Requests"; options.SizeDimension.ShowLegend = true;
            });
        var prepared = Prepare(chart); var sample = Assert.Single(Samples(prepared));
        Assert.Equal(40, sample.Value); Assert.Equal(zeroRadius ? 0 : 16, sample.Radius);
        Assert.Equal(new[] { "40" }, Texts(prepared, "bubble-size-key-label"));
        var glyphs = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "bubble-size-key-marker").ToArray();
        if (zeroRadius) { Assert.Empty(glyphs); Assert.Empty(Marks(prepared)); }
        else Assert.Equal(Marks(prepared)[0].Rx, Assert.Single(glyphs).Rx, 8);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
        AssertNonDataKey(prepared);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(760, 500, false)]
    [InlineData(360, 360, false)]
    [InlineData(390, 520, false)]
    [InlineData(390, 520, true)]
    public void CombinedKeysFitMeasuredCaptionsAndUseFinalResponsivePlotRadii(int width, int height, bool fallbackFont) {
        const string family = "CFX Bubble Size Key Carlito";
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        var chart = Bare().WithSize(width, height).WithFontFamily(fallbackFont ? "CFX Missing Bubble Size Key Font" : family)
            .AddBubble("Service", new[] {
                new ChartBubble(2, 20, 10).WithColorValue(0), new ChartBubble(5, 50, 55).WithColorValue(5),
                new ChartBubble(8, 80, 100).WithColorValue(10)
            }).ConfigureBubble(options => { options.SizeDimension.Name = "Requests"; options.SizeDimension.ShowLegend = true; });
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithValueRange(0, 10));
        var prepared = Prepare(chart);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "cartesian.size-scale-overflow" or "cartesian.color-scale-overflow");
        var key = Assert.Single(prepared.Regions, region => region.Role == "bubble-size-key");
        Assert.InRange(key.Bounds.Left, 0, width); Assert.InRange(key.Bounds.Right, 0, width);
        Assert.InRange(key.Bounds.Top, 0, height); Assert.InRange(key.Bounds.Bottom, 0, height);
        Assert.NotEmpty(Groups(prepared, "color-scale-0"));
        foreach (var caption in prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "bubble-size-key-label" or "bubble-size-key-title")) {
            var region = prepared.Regions.Single(item => item.Id == caption.Id);
            Assert.Equal(region.Label, string.Join("\n", caption.Text.Lines.Select(line => line.Text)));
            Assert.DoesNotContain("…", region.Label!, StringComparison.Ordinal);
            Assert.True(caption.Text.Size >= 10);
            AssertContained(region.Bounds, key.Bounds);
        }
        var samples = Samples(prepared); var marks = Marks(prepared);
        Assert.Equal(samples.Length, Texts(prepared, "bubble-size-key-label").Length);
        Assert.Equal(marks[0].Rx, samples.Single(sample => sample.Value == 10).Radius, 8);
        Assert.Equal(marks[2].Rx, samples.Single(sample => sample.Value == 100).Radius, 8);
        foreach (var sample in samples.Where(sample => sample.Value == 55)) Assert.Equal(marks[1].Rx, sample.Radius, 8);
        var glyphs = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "bubble-size-key-marker").ToArray();
        Assert.Equal(samples.Count(sample => sample.Radius > 0), glyphs.Length);
        Assert.All(glyphs, glyph => AssertContained(new ChartRect(glyph.Cx - glyph.Rx, glyph.Cy - glyph.Ry, glyph.Rx * 2, glyph.Ry * 2), key.Bounds));
        AssertNonDataKey(prepared);
        AssertKeyPaintedInSvgAndPng(prepared, glyphs);
        Capture(prepared, "combined-key-" + width + (fallbackFont ? "-fallback" : "-registered"));
    }

    [Fact]
    public void OversizedExplicitKeyIsDiagnosedAndOmittedWithoutShrinkingTheScale() {
        var chart = Bare().WithSize(360, 360).AddBubble("Service", new[] { new ChartBubble(5, 50, 100) })
            .ConfigureBubble(options => {
                options.WithSizeDomain(0, 100); options.MinimumRadius = 2; options.MaximumRadius = 320;
                options.SizeDimension.Name = "Requests"; options.SizeDimension.ShowLegend = true;
            });
        var prepared = Prepare(chart);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.size-scale-overflow");
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role is "bubble-size-key-marker" or "bubble-size-key-label");
        Assert.Equal(320, Assert.Single(Marks(prepared)).Rx);
        var key = Assert.Single(prepared.Regions, region => region.Role == "bubble-size-key");
        Assert.Contains("Requests", key.Label); Assert.Contains("100", key.Label);
        Assert.Equal("100", Assert.Single(Groups(prepared, "point")).Metadata["data-cfx-size"]);
        Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart Bare() {
        var chart = Chart.Create().WithSize(960, 600).WithHeader(false).WithLegend().WithDataLabels(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = false;
        return chart;
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneGroup[] Groups(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == role).ToArray();
    private static VisualSceneEllipse[] Marks(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "bubble").ToArray();
    private static string[] Texts(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(node => node.Role == role).Select(node => string.Join("\n", node.Text.Lines.Select(line => line.Text))).ToArray();
    private static (double Value, double Radius)[] Samples(PreparedVisual prepared) => Groups(prepared, "bubble-size-key-sample")
        .Select(group => (double.Parse(group.Metadata["data-cfx-size-value"], CultureInfo.InvariantCulture),
            double.Parse(group.Metadata["data-cfx-radius"], CultureInfo.InvariantCulture))).ToArray();

    private static void AssertNonDataKey(PreparedVisual prepared) {
        var document = XDocument.Parse(prepared.ToSvg());
        var key = Assert.Single(document.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "bubble-size-key");
        foreach (var element in key.DescendantsAndSelf()) {
            Assert.Null(element.Attribute("data-cfx-series")); Assert.Null(element.Attribute("data-cfx-series-key"));
            Assert.Null(element.Attribute("data-cfx-point")); Assert.Null(element.Attribute("data-cfx-observation-id"));
            Assert.Null(element.Attribute("data-cfx-source-point")); Assert.Null(element.Attribute("tabindex"));
        }
    }

    private static void AssertContained(ChartRect inner, ChartRect outer) {
        const double tolerance = .001;
        Assert.True(inner.Left >= outer.Left - tolerance && inner.Right <= outer.Right + tolerance);
        Assert.True(inner.Top >= outer.Top - tolerance && inner.Bottom <= outer.Bottom + tolerance);
    }

    private static void AssertKeyPaintedInSvgAndPng(PreparedVisual prepared, VisualSceneEllipse[] glyphs) {
        var document = XDocument.Parse(prepared.ToSvg());
        var svg = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "bubble-size-key-marker").ToArray();
        Assert.Equal(glyphs.Length, svg.Length);
        var image = ChartForgeX.Raster.RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal((int)prepared.Size.Width, image.Width); Assert.Equal((int)prepared.Size.Height, image.Height);
        foreach (var glyph in glyphs) {
            Assert.Contains(svg, element => Math.Abs(double.Parse(element.Attribute("rx")!.Value, CultureInfo.InvariantCulture) - glyph.Rx) < .001
                && Math.Abs(double.Parse(element.Attribute("cx")!.Value, CultureInfo.InvariantCulture) - glyph.Cx) < .001);
            var ink = false;
            var edgeX = (int)Math.Round(glyph.Cx + glyph.Rx); var edgeY = (int)Math.Round(glyph.Cy);
            for (var y = Math.Max(0, edgeY - 2); y <= Math.Min(image.Height - 1, edgeY + 2); y++)
                for (var x = Math.Max(0, edgeX - 2); x <= Math.Min(image.Width - 1, edgeX + 2); x++) {
                    var offset = (y * image.Width + x) * 4;
                    if (Enumerable.Range(0, 4).Any(channel => image.Pixels[offset + channel] != image.Pixels[channel])) ink = true;
                }
            Assert.True(ink, "The native PNG must paint the key circle at its prepared logical edge.");
        }
    }

    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BUBBLE_CAPTURE_DIRECTORY");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }
}
