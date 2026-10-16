using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects complete categorical captions and coherent compact keys in the shared static scene.</summary>
public sealed class FamilyLabelLayoutRegressionTests {
    private static readonly string[] Categories = { "Identity", "Network", "Storage", "Backup", "Access", "Recovery" };

    [Theory]
    [InlineData(false, 400, 280, VisualThemeMode.Light)]
    [InlineData(false, 400, 280, VisualThemeMode.Dark)]
    [InlineData(true, 400, 280, VisualThemeMode.Light)]
    [InlineData(true, 400, 280, VisualThemeMode.Dark)]
    [InlineData(false, 800, 440, VisualThemeMode.Light)]
    [InlineData(true, 800, 440, VisualThemeMode.Dark)]
    public void MatrixCategoriesRemainCompleteSeparatedAndReadableInBothPainters(bool hexagonal, int width, int height, VisualThemeMode mode) {
        var chart = hexagonal ? HexbinHeatmapRecipe.Create() : HeatmapRecipe.Create();
        var prepared = Prepare(chart, width, height, mode);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"family-matrix-{hexagonal}-{width}-{mode}");
        var labels = Texts(prepared, "heatmap-column-label");
        Assert.Equal(Categories, labels.Select(text => string.Join(" ", text.Text.Lines.Select(line => line.Text))));
        Assert.Equal(Categories, prepared.Regions.Where(region => region.Role == "heatmap-column-label").Select(region => region.Label));
        Assert.All(labels, label => Assert.Equal(13.5, label.Text.Size));
        var geometry = LabelGeometry(prepared, "heatmap-column-label");
        Assert.All(geometry.SelectMany(corners => corners), point => { Assert.InRange(point.X, 0, width); Assert.InRange(point.Y, 0, height); });
        for (var index = 1; index < geometry.Count; index++) Assert.True(Separated(geometry[index - 1], geometry[index]), "Adjacent matrix captions must have separate measured footprints.");
        AssertExportedCaptions(prepared, "heatmap-column-label", Categories);
        AssertPainted(prepared, labels);
    }

    [Fact]
    public void MatrixWordCaptionsWrapBeforeRotationAndDenseColumnsThinUniformly() {
        var captions = new[] { "Access policy", "Backup health", "Network state" };
        var wrapped = Prepare(Chart.Create().WithXLabels(captions).AddHeatmapRow("Service", new[] { 1d, 2, 3 }), 280, 280);
        var labels = Texts(wrapped, "heatmap-column-label");
        Assert.Equal(captions, labels.Select(label => string.Join(" ", label.Text.Lines.Select(line => line.Text))));
        Assert.Contains(labels, label => label.Text.Lines.Count == 2);
        Assert.All(LabelGeometry(wrapped, "heatmap-column-label"), corners => Assert.Equal(corners[0].Y, corners[1].Y));

        var denseCaptions = Enumerable.Range(1, 60).Select(index => "Check " + index).ToArray();
        var dense = Prepare(Chart.Create().WithXLabels(denseCaptions).AddHexbinHeatmapRow("Service", Enumerable.Range(1, 60).Select(index => (double)index).ToArray()), 400, 380);
        var drawn = Texts(dense, "heatmap-column-label");
        Assert.InRange(drawn.Length, 2, denseCaptions.Length - 1);
        var indices = drawn.Select(text => int.Parse(text.Id!.Substring("matrix-column-".Length))).ToArray();
        var intervals = indices.Zip(indices.Skip(1), (first, second) => second - first).Distinct();
        Assert.Single(intervals);
        Assert.All(drawn, label => Assert.Equal(denseCaptions[int.Parse(label.Id!.Substring("matrix-column-".Length))], Assert.Single(label.Text.Lines).Text));
        Assert.Equal(denseCaptions, dense.Regions.Where(region => region.Role == "heatmap-column-label").Select(region => region.Label));
    }

    [Theory]
    [InlineData(320, 280, VisualThemeMode.Light)]
    [InlineData(320, 280, VisualThemeMode.Dark)]
    [InlineData(320, 360, VisualThemeMode.Light)]
    [InlineData(400, 280, VisualThemeMode.Light)]
    [InlineData(400, 280, VisualThemeMode.Dark)]
    [InlineData(800, 440, VisualThemeMode.Light)]
    [InlineData(800, 440, VisualThemeMode.Dark)]
    public void FunnelCaptionUsesOutsideRailWhenItCannotFitCompletelyInsideItsBar(int width, int height, VisualThemeMode mode) {
        var prepared = Prepare(FunnelRecipe.Create(), width, height, mode);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"family-funnel-{width}-{height}-{mode}");
        var final = Texts(prepared, "funnel-label").Last();
        Assert.Equal("Completed: 41", string.Join(" ", final.Text.Lines.Select(line => line.Text)));
        var stage = prepared.Regions.Single(region => region.Id == "series-0-point-3" && region.Role == "funnel-stage").Bounds;
        var label = prepared.Regions.Single(region => region.Id == final.Id).Bounds;
        var ratio = prepared.Regions.Single(region => region.Id == "series-0-point-3-ratio");
        Assert.Equal("34.2% retained\n44.6% drop-off", ratio.Label);
        var ratioText = Texts(prepared, "funnel-ratio").Last();
        Assert.Equal(ratio.Label, string.Join("\n", ratioText.Text.Lines.Select(line => line.Text)));
        if (width < 400) {
            Assert.True(label.Left >= stage.Right, "A short final bar uses its empty outside rail.");
            Assert.True(label.Bottom <= ratio.Bounds.Top || label.Right <= ratio.Bounds.Left);
        } else Assert.True(label.Left >= stage.Left && label.Right <= stage.Right, "A wide stage retains its inside caption.");
        Assert.All(new[] { final, ratioText }, text => Assert.Equal(13.5, text.Text.Size));
        AssertExportedCaptions(prepared, "funnel-label", new[] { "Received: 120", "Qualified: 95", "Reviewed: 74", "Completed: 41" });
        AssertPainted(prepared, new[] { final, ratioText });
    }

    [Theory]
    [InlineData(false, 400, 280, VisualThemeMode.Light)]
    [InlineData(false, 800, 440, VisualThemeMode.Dark)]
    [InlineData(true, 400, 280, VisualThemeMode.Light)]
    [InlineData(true, 400, 280, VisualThemeMode.Dark)]
    [InlineData(true, 800, 440, VisualThemeMode.Light)]
    public void MapMissingSwatchAndCaptionFormOneMeasuredItemBesideAReadableColorKey(bool tiles, int width, int height, VisualThemeMode mode) {
        var chart = tiles ? TileMapRecipe.Create() : RegionMapRecipe.Create();
        var prepared = Prepare(chart, width, height, mode);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"family-map-{tiles}-{width}-{mode}");
        var swatch = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "map-scale-no-data");
        var caption = Assert.Single(Texts(prepared, "map-scale-no-data-label"));
        var line = Assert.Single(caption.Text.Lines);
        Assert.Equal("No data", line.Text);
        Assert.Equal(4, caption.LineLeft(line) - swatch.Bounds.Right, 6);
        var textCenter = caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height / 2;
        Assert.Equal(swatch.Bounds.Top + swatch.Bounds.Height / 2, textCenter, 6);
        Assert.Contains(Texts(prepared, "map-scale-label"), text => text.Text.Lines[0].Text == "Less · 39");
        Assert.Contains(Texts(prepared, "map-scale-label"), text => text.Text.Lines[0].Text == "More · 91");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "map-scale-step");
        if (tiles) {
            var codes = chart.Series[0].TileMapDefinition!.Regions.Select(region => region.Code).ToArray();
            var tileLabels = Texts(prepared, "tile-map-label");
            Assert.Equal(51, tileLabels.Length);
            Assert.Equal(codes, prepared.Regions.Where(region => region.Role == "tile-map-label").Select(region => region.Label));
            Assert.Equal(51, prepared.Regions.Count(region => region.Role == "tile-map-region"));
            Assert.All(tileLabels, text => {
                Assert.Equal(13.5, text.Text.Size);
                var tile = prepared.Regions.Single(region => region.Id + "-label" == text.Id && region.Role == "tile-map-region").Bounds;
                var stroke = chart.Options.MapRegionStrokeWidth;
                Assert.True(text.Text.Metrics.Height + stroke <= tile.Height);
                Assert.True(text.Text.Metrics.Width + stroke + .44 * (text.Text.Metrics.Height + stroke) <= tile.Width,
                    "Every complete code must clear its hexagon's slanted edges and stroke.");
            });
            var legend = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "legend-entry");
            Assert.Equal(chart.Series[0].InteractionIdentityKey, legend.Metadata["data-cfx-series-key"]);
            var occupied = prepared.Regions.Where(region => region.Role == "tile-map-region").Select(region => region.Bounds).ToArray();
            var keyBounds = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role is "map-scale-step" or "map-scale-no-data").Select(node => node.Bounds)
                .Concat(prepared.Regions.Where(region => region.Role == "legend").Select(region => region.Bounds))
                .Concat(new[] { "map-scale-label", "map-scale-no-data-label" }.SelectMany(role => LabelGeometry(prepared, role))
                    .Select(corners => new ChartRect(corners.Min(point => point.X), corners.Min(point => point.Y),
                        corners.Max(point => point.X) - corners.Min(point => point.X), corners.Max(point => point.Y) - corners.Min(point => point.Y))));
            Assert.All(keyBounds, bounds => Assert.DoesNotContain(occupied, tile => Overlaps(bounds, tile)));
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "frame.insufficient-space");
            AssertExportedCaptions(prepared, "tile-map-label", codes);
        }
        AssertPainted(prepared, new[] { caption });
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var offset = ((int)(swatch.Bounds.Top + swatch.Bounds.Height / 2) * image.Width + (int)(swatch.Bounds.Left + swatch.Bounds.Width / 2)) * 4;
        Assert.Equal(swatch.Fill!.Value.R, image.Pixels[offset]);
        Assert.Equal(swatch.Fill.Value.G, image.Pixels[offset + 1]);
        Assert.Equal(swatch.Fill.Value.B, image.Pixels[offset + 2]);
        Assert.Contains("No data", prepared.ToSvg());
    }

    [Theory]
    [InlineData("frame-position")]
    [InlineData("model-position")]
    [InlineData("scale-position")]
    [InlineData("filled-grid")]
    public void AuthoredMapLegendPositionsAndOccupiedGridKeepSeparateKeys(string control) {
        var chart = TileMapRecipe.Create();
        VisualFrame? frame = null;
        if (control == "frame-position") frame = new VisualFrame("", "", showLegend: true, legendPosition: ChartLegendPosition.TopLeft);
        else if (control == "model-position") chart.Options.LegendPosition = ChartLegendPosition.TopLeft;
        else if (control == "scale-position") chart.Options.MapScaleLegendPosition = ChartMapScaleLegendPosition.Bottom;
        else {
            var source = chart.Series[0].TileMapDefinition!;
            var regions = source.Regions.ToList(); var added = 0;
            for (var row = 0; row < source.RowCount; row++)
                for (var column = 0; column < source.ColumnCount; column++)
                    if (!source.Regions.Any(region => region.Row == row && region.Column == column)) {
                        var code = (++added).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                        regions.Add(new ChartTileMapRegion(code, "Additional region " + code, column, row));
                    }
            chart = Chart.Create().AddTileMap("Observations", new ChartTileMapDefinition("filled", "Occupied grid", regions),
                new[] { new ChartRegionMapItem("CA", 39), new ChartRegionMapItem("NY", 91) }).WithMapLabels();
        }
        var prepared = Prepare(chart, 400, 280, frame: frame);
        var tiles = prepared.Regions.Where(region => region.Role == "tile-map-region").ToArray();
        var ramp = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "map-scale-step").ToArray();
        Assert.NotEmpty(ramp);
        Assert.All(ramp, step => Assert.True(step.Bounds.Top >= tiles.Max(tile => tile.Bounds.Bottom)));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "legend-entry");
        Assert.Single(Texts(prepared, "map-scale-no-data-label"));
        Assert.Contains("Observations", prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MapInsetMeasuresCompleteResolvedDomainCaptionsBeforeChoosingItsSlot(bool precisionRefinement) {
        var source = ChartTileMapCatalog.Get("us-states");
        var definition = precisionRefinement ? new ChartTileMapDefinition("sparse-key", "Sparse key space",
            source.Regions.Where(region => region.Code is not ("VT" or "NH"))) : source;
        var minimum = precisionRefinement ? 100000000000001d : 39;
        var maximum = precisionRefinement ? 100000100000001d : 91;
        var chart = Chart.Create().AddTileMap("Observations", definition,
            new[] { new ChartRegionMapItem("CA", minimum), new ChartRegionMapItem("NY", maximum) }).WithMapLabels();
        var calls = new Dictionary<double, int>();
        if (!precisionRefinement) {
            chart.Options.HeatmapRelativeScale = true;
            chart.WithValueFormat(ChartValueFormat.Custom(value => {
                calls[value] = calls.GetValueOrDefault(value) + 1;
                return value == 0 ? "No recorded observations" : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            }));
        }
        var prepared = Prepare(chart, 400, 280);
        ScatterBubbleColorRenderingTests.Capture(prepared, "family-map-caption-" + (precisionRefinement ? "precision" : "relative"));
        var expected = precisionRefinement ? new[] { "Less · 100000000000001", "More · 100000100000001" }
            : new[] { "Less · No recorded observations", "More · 91" };
        Assert.Equal(expected, Texts(prepared, "map-scale-label").Select(text => Assert.Single(text.Text.Lines).Text));
        var missing = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "map-scale-no-data").Bounds;
        var captions = new[] { "map-scale-label", "map-scale-no-data-label" }.SelectMany(role => LabelGeometry(prepared, role))
            .Append(new[] { new ChartPoint(missing.Left, missing.Top), new ChartPoint(missing.Right, missing.Top),
                new ChartPoint(missing.Right, missing.Bottom), new ChartPoint(missing.Left, missing.Bottom) }).ToArray();
        for (var index = 0; index < captions.Length; index++)
            for (var other = index + 1; other < captions.Length; other++)
                Assert.True(Separated(captions[index], captions[other]), "Complete resolved captions must have separate painted footprints.");
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
        AssertExportedCaptions(prepared, "map-scale-label", expected);
        AssertPainted(prepared, Texts(prepared, "map-scale-label"));
        if (!precisionRefinement) {
            Assert.Equal(new[] { 0d, 39, 45.5, 91 }, calls.Keys.OrderBy(value => value));
            Assert.All(calls.Values, count => Assert.Equal(1, count));
        }
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.WithValueFormat(ChartValueFormat.Custom(_ => throw new InvalidOperationException("Prepared export must retain its captured captions.")));
        chart.Options.Labels.NoData = "Changed missing";
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    private static PreparedVisual Prepare(Chart chart, int width, int height, VisualThemeMode mode = VisualThemeMode.Light, VisualFrame? frame = null) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(width, height), 14), VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 13.5, legendSize: 13.5, dataLabelSize: 13.5)),
        mode, frame ?? new VisualFrame("", "", showLegend: true), FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))));

    private static bool Overlaps(ChartRect first, ChartRect second) => first.Left < second.Right - .000001 && second.Left < first.Right - .000001
        && first.Top < second.Bottom - .000001 && second.Top < first.Bottom - .000001;

    private static VisualSceneText[] Texts(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();

    private static void AssertExportedCaptions(PreparedVisual prepared, string role, string[] captions) {
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(captions, document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role)
            .Select(node => node.Name.LocalName == "text" ? node.Value : string.Join(" ", node.Descendants().Where(child => child.Name.LocalName == "text").Select(child => child.Value))));
        Assert.True(prepared.ToPng(new VisualRenderOptions(supersampling: 1)).Length > 64);
    }

    private static void AssertPainted(PreparedVisual prepared, IEnumerable<VisualSceneText> texts) {
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var text in texts) {
            var corners = TextGeometry(prepared, text.Role!).Single(label => label.Text == text).Corners;
            var region = new ChartRect(corners.Min(point => point.X), corners.Min(point => point.Y),
                corners.Max(point => point.X) - corners.Min(point => point.X), corners.Max(point => point.Y) - corners.Min(point => point.Y));
            // Search the painted footprint after its scene transform; semantic regions keep unrotated layout bounds.
            Assert.True(Enumerable.Range(Math.Max(0, (int)region.Top), Math.Max(0, Math.Min(image.Height, (int)Math.Ceiling(region.Bottom)) - Math.Max(0, (int)region.Top)))
                .Any(y => Enumerable.Range(Math.Max(0, (int)region.Left), Math.Max(0, Math.Min(image.Width, (int)Math.Ceiling(region.Right)) - Math.Max(0, (int)region.Left)))
                    .Any(x => { var pixel = (y * image.Width + x) * 4; return Math.Abs(image.Pixels[pixel] - text.Color.R) < 20
                        && Math.Abs(image.Pixels[pixel + 1] - text.Color.G) < 20 && Math.Abs(image.Pixels[pixel + 2] - text.Color.B) < 20; })), "The caption must be painted by the native PNG backend.");
        }
    }

    private static List<(VisualSceneText Text, ChartPoint[] Corners)> TextGeometry(PreparedVisual prepared, string role) {
        var result = new List<(VisualSceneText, ChartPoint[])>(); var stack = new Stack<VisualSceneTransform>(); var transform = VisualSceneTransform.Identity;
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) {
                stack.Push(transform);
                if (group.Rotation.HasValue) transform = transform.Rotate(group.Rotation.Value);
                if (group.Translation.HasValue) transform = transform.Translate(group.Translation.Value);
            }
            else if (node is VisualSceneEndGroup) transform = stack.Pop();
            else if (node is VisualSceneText text && node.Role == role) {
                var left = text.X - text.Text.Metrics.Width / 2;
                if (text.Alignment == TextAlignment.Right) left -= text.Text.Metrics.Width / 2;
                else if (text.Alignment == TextAlignment.Left) left += text.Text.Metrics.Width / 2;
                var top = text.Baseline - text.Text.Ascent; var width = text.Text.Metrics.Width; var height = text.Text.Metrics.Height;
                result.Add((text, new[] { new ChartPoint(left, top), new ChartPoint(left + width, top), new ChartPoint(left + width, top + height), new ChartPoint(left, top + height) }.Select(transform.Apply).ToArray()));
            }
        }
        return result;
    }

    private static List<ChartPoint[]> LabelGeometry(PreparedVisual prepared, string role) => TextGeometry(prepared, role).Select(label => label.Corners).ToList();

    private static bool Separated(ChartPoint[] first, ChartPoint[] second) => new[] { first, second }.Any(corners =>
        Enumerable.Range(0, 2).Any(index => {
            var dx = corners[index + 1].X - corners[index].X; var dy = corners[index + 1].Y - corners[index].Y;
            double Project(ChartPoint point) => -dy * point.X + dx * point.Y;
            return first.Max(Project) <= second.Min(Project) || second.Max(Project) <= first.Min(Project);
        }));
}
