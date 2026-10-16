using System.Linq;
using System;
using System.Reflection;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualCanvasTileTypographyTests {
    [Fact]
    public void DefaultTileOutputMatchesExplicitCurrentTypography() {
        var baseline = VisualCanvas.Create(420, 150)
            .AddInfoTile(12, 12, 396, 126, "PC", "HOSTNAME", "DEV-WKS-01", "Windows workstation");
        var explicitDefaults = VisualCanvas.Create(420, 150)
            .AddInfoTileWithTextOptions(12, 12, 396, 126, "PC", "HOSTNAME",
                "DEV-WKS-01",
                new VisualCanvasInfoTileTextOptions(textScale: 1, labelFontSize: 14,
                    valueFontSize: 22, detailFontSize: 13), "Windows workstation");

        Assert.Equal(baseline.ToSvg(), explicitDefaults.ToSvg());
        Assert.Equal(baseline.ToPng(), explicitDefaults.ToPng());
    }

    [Theory]
    [InlineData(0.8)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void TileScaleChangesAllTextAndSupportingGeometry(double scale) {
        var canvas = VisualCanvas.Create(760, 220)
            .ConfigureStyle(style => style.TileTextScale = scale)
            .AddInfoTile(12, 12, 736, 196, "CPU", "PROCESSOR", "Intel Xeon", "48 cores",
                miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline,
                miniChartValues: new[] { 20d, 40d, 30d, 55d });
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height,
            metrics.TextX, metrics.TextMax, style);

        Assert.Equal(14 * scale, layout.Lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Label).FontSize, 6);
        Assert.Equal(22 * scale, layout.Lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Value).FontSize, 6);
        Assert.Equal(13 * scale, layout.Lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Detail).FontSize, 6);
        Assert.Equal(54 * scale, metrics.IconBox, 6);
        Assert.True(metrics.ChartHeight >= 28 * scale);
    }

    [Fact]
    public void PerRoleSizeOverridesThemeAndTileScale() {
        var theme = VisualTheme.Graphite().WithTypography(new VisualTypography(
            tileLabelSize: 16, tileValueSize: 26, tileDetailSize: 15));
        var canvas = VisualCanvas.Create(440, 160).WithTheme(theme)
            .AddInfoTileWithTextOptions(12, 12, 416, 136, "OS", "SYSTEM",
                "Windows 11", new VisualCanvasInfoTileTextOptions(textScale: 0.8,
                    labelFontSize: 18), "Enterprise");
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var lines = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height,
            metrics.TextX, metrics.TextMax, style).Lines;

        Assert.Equal(14.4, lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Label).FontSize, 6);
        Assert.Equal(20.8, lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Value).FontSize, 6);
        Assert.Equal(12, lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Detail).FontSize, 6);
    }

    [Fact]
    public void AScaleThatDoesNotFitIsReducedAndReported() {
        var canvas = VisualCanvas.Create(300, 100).AddInfoTileWithTextOptions(
            8, 8, 284, 58, "PC", "VERY LONG HOST LABEL",
            "A workstation value that needs room",
            new VisualCanvasInfoTileTextOptions(textScale: 1.5), "extra detail");

        Assert.Contains(canvas.AnalyzeLayout().Diagnostics,
            diagnostic => diagnostic.Code == "InfoTileTextScaleReduced");
    }

    [Fact]
    public void FixedSizeFallbackKeepsTheIconVisibleAndTextAfterItsBox() {
        var accent = ChartColor.FromHex("#38BDF8");
        var withIcon = VisualCanvas.Create(320, 100).AddInfoTileWithTextOptions(
            8, 8, 304, 48, string.Empty, "HOSTNAME", "WORKSTATION-01",
            new VisualCanvasInfoTileTextOptions(textScale: 4),
            "Windows desktop", accent,
            surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Outline,
            iconKind: VisualCanvasInfoTileIconKind.Computer);
        var withoutIcon = VisualCanvas.Create(320, 100).AddInfoTileWithTextOptions(
            8, 8, 304, 48, string.Empty, "HOSTNAME", "WORKSTATION-01",
            new VisualCanvasInfoTileTextOptions(textScale: 4),
            "Windows desktop", accent,
            surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Outline);
        var tile = (VisualCanvasInfoTileLayer)withIcon.Layers[0];
        var layout = VisualCanvasInfoTileTextLayout.CalculateLayout(tile,
            withIcon.ResolveStyle());
        var metrics = layout.Metrics;
        XNamespace svgNamespace = "http://www.w3.org/2000/svg";
        var svg = XDocument.Parse(withIcon.ToSvg());
        var svgIconBox = svg.Descendants(svgNamespace + "rect").Single(rect =>
            (double?)rect.Attribute("x") == metrics.IconX &&
            (double?)rect.Attribute("y") == metrics.IconY);
        var svgLabel = svg.Descendants(svgNamespace + "text").Single(text =>
            text.Value == "HOSTNAME");
        var withIconPixels = withIcon.ToRgbaImage();
        var withoutIconPixels = withoutIcon.ToRgbaImage();
        var changedPixels = 0;
        var left = Math.Max(0, (int)Math.Floor(metrics.IconX));
        var right = Math.Min(withIconPixels.Width,
            (int)Math.Ceiling(metrics.IconX + metrics.IconBox));
        var top = Math.Max(0, (int)Math.Floor(metrics.IconY));
        var bottom = Math.Min(withIconPixels.Height,
            (int)Math.Ceiling(metrics.IconY + metrics.IconBox));

        for (var y = top; y < bottom; y++) {
            for (var x = left; x < right; x++) {
                var offset = (y * withIconPixels.Width + x) * 4;
                if (withIconPixels.Pixels[offset] != withoutIconPixels.Pixels[offset] ||
                    withIconPixels.Pixels[offset + 1] != withoutIconPixels.Pixels[offset + 1] ||
                    withIconPixels.Pixels[offset + 2] != withoutIconPixels.Pixels[offset + 2] ||
                    withIconPixels.Pixels[offset + 3] != withoutIconPixels.Pixels[offset + 3]) {
                    changedPixels++;
                }
            }
        }

        Assert.True(metrics.IconBox >= 24);
        Assert.True(layout.Text.WasScaledDown);
        Assert.True(metrics.TextX >= metrics.IconX + metrics.IconBox);
        Assert.Equal(metrics.IconBox * (22d / 54),
            metrics.TextX - metrics.IconX - metrics.IconBox, 6);
        Assert.Equal(metrics.IconBox, (double)svgIconBox.Attribute("width")!, 6);
        Assert.Equal(metrics.IconBox, (double)svgIconBox.Attribute("height")!, 6);
        Assert.Equal(metrics.TextX, (double)svgLabel.Attribute("x")!, 3);
        Assert.True(changedPixels > 0);
        Assert.Contains(withIcon.AnalyzeLayout().Diagnostics,
            diagnostic => diagnostic.Code == "InfoTileTextScaleReduced");
    }

    [Fact]
    public void SmallRequestedScaleKeepsTheSmallIconBoxWithoutFallback() {
        var canvas = VisualCanvas.Create(320, 100).AddInfoTileWithTextOptions(
            8, 8, 304, 48, string.Empty, "HOSTNAME", "WORKSTATION-01",
            new VisualCanvasInfoTileTextOptions(textScale: 0.2), "Windows desktop");
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var layout = VisualCanvasInfoTileTextLayout.CalculateLayout(tile,
            canvas.ResolveStyle());

        Assert.False(layout.Text.WasScaledDown);
        Assert.True(layout.Metrics.IconBox < 24);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(4)]
    public void ShrinkToFitKeepsTextInsideShortTiles(double requestedScale) {
        var canvas = VisualCanvas.Create(300, 100).AddInfoTileWithTextOptions(
            8, 8, 284, 58, "PC", "VERY LONG HOST LABEL",
            "A workstation value that needs room",
            new VisualCanvasInfoTileTextOptions(textScale: requestedScale), "extra detail");
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y,
            metrics.Height, metrics.TextX, metrics.TextMax, style);

        Assert.True(layout.WasScaledDown);
        Assert.False(layout.HasVerticalOverflow);
        Assert.All(layout.Lines, line => Assert.True(
            line.Y + line.FontSize <= metrics.Y + metrics.Height + 0.5));
    }

    [Fact]
    public void ShrinkLoopGoesBelowItsPreferredFloorToPreventVerticalOverflow() {
        var canvas = VisualCanvas.Create(240, 100).AddInfoTileWithTextOptions(8, 8,
            224, 84, "PC", "A long label", "A value that cannot fit at scale three",
            new VisualCanvasInfoTileTextOptions(textScale: 3));
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y,
            metrics.Height, metrics.TextX, metrics.TextMax, style);

        Assert.True(layout.Scale < 1.35);
        Assert.True(layout.WasScaledDown);
        Assert.False(layout.HasVerticalOverflow);
    }

    [Fact]
    public void ZeroWidthMiniChartDoesNotReserveTextSpace() {
        var canvas = VisualCanvas.Create(200, 220).AddInfoTileWithTextOptions(0, 0,
            200, 196, "CPU", "PROCESSOR", "Intel Xeon",
            new VisualCanvasInfoTileTextOptions(textScale: 2),
            miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline,
            miniChartValues: new[] { 1d, 2d, 3d });
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile,
            canvas.ResolveStyle());

        Assert.False(metrics.HasMiniChart);
        Assert.True(metrics.TextMax > 20);
    }

    [Fact]
    public void MiniChartWithInsufficientHeightIsSuppressed() {
        var canvas = VisualCanvas.Create(300, 80).AddInfoTileWithTextOptions(8, 8,
            284, 56, "CPU", "PROCESSOR", "Intel Xeon",
            new VisualCanvasInfoTileTextOptions(textScale: 3),
            miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline,
            miniChartValues: new[] { 1d, 2d, 3d });
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile,
            canvas.ResolveStyle());

        Assert.False(metrics.HasMiniChart);
        Assert.True(metrics.TextMax > 20);
        Assert.DoesNotContain("data-cfx-role=\"visual-canvas-info-tile-mini-chart\"",
            canvas.ToSvg());
        var withoutMiniChart = VisualCanvas.Create(300, 80).AddInfoTileWithTextOptions(8, 8,
            284, 56, "CPU", "PROCESSOR", "Intel Xeon",
            new VisualCanvasInfoTileTextOptions(textScale: 3));
        Assert.Equal(withoutMiniChart.ToPng(), canvas.ToPng());
    }

    [Fact]
    public void ExistingAddInfoTileClrSignaturesRemainAvailable() {
        var methods = typeof(VisualCanvas).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name == nameof(VisualCanvas.AddInfoTile)).ToArray();

        Assert.Contains(methods, method => method.GetParameters().Length == 16 &&
            method.GetParameters()[15].ParameterType == typeof(VisualCanvasTextFitPolicy));
        Assert.Contains(methods, method => method.GetParameters().Length == 15 &&
            method.GetParameters()[14].ParameterType == typeof(VisualCanvasTextFitPolicy));
    }

    [Fact]
    public void DefaultLiteralStillBindsToTheExistingDetailParameter() {
        var canvas = VisualCanvas.Create(320, 100)
            .AddInfoTile(8, 8, 304, 84, "PC", "HOST", "WORKSTATION", default);

        Assert.Equal(string.Empty, ((VisualCanvasInfoTileLayer)canvas.Layers[0]).Detail);
    }

    [Fact]
    public void DefaultLiteralStillBindsToTheExistingAccentParameter() {
        var coordinateCanvas = VisualCanvas.Create(320, 100)
            .AddInfoTile(8, 8, 304, 84, "PC", "HOST", "WORKSTATION", "detail", default);
        var placedCanvas = VisualCanvas.Create(320, 100)
            .AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.TopLeft, 8, 8),
                304, 84, "PC", "HOST", "WORKSTATION", "detail", default);

        Assert.Null(((VisualCanvasInfoTileLayer)coordinateCanvas.Layers[0]).AccentOverride);
        Assert.Null(((VisualCanvasInfoTileLayer)placedCanvas.Layers[0]).AccentOverride);
    }

    [Fact]
    public void NarrowTileGeometryStaysInsideTileAndReportsUnfittedText() {
        var canvas = VisualCanvas.Create(80, 72).AddInfoTileWithTextOptions(5, 8,
            60, 56, "PC", "HOST", "WORKSTATION",
            new VisualCanvasInfoTileTextOptions(textScale: 2));
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, canvas.ResolveStyle());

        Assert.True(metrics.IconX >= metrics.X);
        Assert.True(metrics.IconX + metrics.IconBox <= metrics.X + metrics.Width);
        Assert.True(metrics.TextX <= metrics.X + metrics.Width);
        Assert.True(metrics.TextMax <= metrics.X + metrics.Width - metrics.TextX);
        Assert.Contains(canvas.AnalyzeLayout().Diagnostics, diagnostic =>
            diagnostic.Code == "InfoTileTextScaleReduced" || diagnostic.Code == "InfoTileTextTrimmed");
    }

    [Fact]
    public void SmallRequestedScaleIsNotRaisedOrReportedAsReduced() {
        var canvas = VisualCanvas.Create(320, 100).AddInfoTileWithTextOptions(8, 8,
            304, 84, "PC", "HOST", "WORKSTATION",
            new VisualCanvasInfoTileTextOptions(textScale: 0.2));
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height,
            metrics.TextX, metrics.TextMax, style);

        Assert.Equal(2.8, layout.Lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Label).FontSize, 6);
        Assert.False(layout.WasScaledDown);
        Assert.DoesNotContain(canvas.AnalyzeLayout().Diagnostics,
            diagnostic => diagnostic.Code == "InfoTileTextScaleReduced");
    }

    [Theory]
    [InlineData(0.5, 4)]
    [InlineData(1.5, 12)]
    public void ProgressRailUsesResolvedTypographyScale(double requestedScale, double expectedHeight) {
        var canvas = VisualCanvas.Create(460, 220)
            .AddInfoTileWithTextOptions(12, 12, 436, 196, "CPU", "PROCESSOR",
                "Intel Xeon", new VisualCanvasInfoTileTextOptions(textScale: requestedScale),
                progress: 0.45);
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height,
            metrics.TextX, metrics.TextMax, style);

        Assert.Equal(requestedScale, layout.Scale, 6);
        var svg = XDocument.Parse(canvas.ToSvg());
        var rails = svg.Descendants().Where(element =>
            element.Name.LocalName == "rect" &&
            (string?)element.Attribute("fill") == style.TileProgressTrackColor.ToCss() &&
            Math.Abs(double.Parse((string)element.Attribute("height")!,
                System.Globalization.CultureInfo.InvariantCulture) - expectedHeight) < 0.001)
            .ToArray();
        Assert.Single(rails);
        Assert.Equal(expectedHeight,
            double.Parse((string)rails[0].Attribute("height")!,
                System.Globalization.CultureInfo.InvariantCulture), 6);

        var image = canvas.ToRgbaImage();
        var withoutProgress = VisualCanvas.Create(460, 220)
            .AddInfoTileWithTextOptions(12, 12, 436, 196, "CPU", "PROCESSOR",
                "Intel Xeon", new VisualCanvasInfoTileTextOptions(textScale: requestedScale));
        var baseline = withoutProgress.ToRgbaImage();
        var trackX = Math.Clamp((int)Math.Round(metrics.TextX + metrics.TextMax * 0.8),
            0, image.Width - 1);
        var expectedY = metrics.Y + metrics.Height - 16 * requestedScale;
        var matchingRows = 0;
        for (var row = Math.Max(0, (int)Math.Floor(expectedY) - 2);
             row < Math.Min(image.Height, (int)Math.Ceiling(expectedY + expectedHeight) + 2);
             row++) {
            var pixel = (row * image.Width + trackX) * 4;
            if (image.Pixels[pixel] != baseline.Pixels[pixel] ||
                image.Pixels[pixel + 1] != baseline.Pixels[pixel + 1] ||
                image.Pixels[pixel + 2] != baseline.Pixels[pixel + 2] ||
                image.Pixels[pixel + 3] != baseline.Pixels[pixel + 3]) matchingRows++;
        }

        Assert.InRange(Math.Abs(matchingRows - expectedHeight), 0, 2);
    }

    [Fact]
    public void LargeProgressRailScaleStaysInsideShortTileInBothRenderers() {
        var canvas = VisualCanvas.Create(320, 80).AddInfoTileWithTextOptions(8, 8,
            304, 56, "PC", "HOST", "WORKSTATION",
            new VisualCanvasInfoTileTextOptions(textScale: 4), progress: 0.45);
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var tileBottom = metrics.Y + metrics.Height;

        Assert.True(metrics.ProgressRailY >= metrics.Y);
        Assert.True(metrics.ProgressRailY + metrics.ProgressRailHeight <= tileBottom);
        var svg = XDocument.Parse(canvas.ToSvg());
        var track = Assert.Single(svg.Descendants(), element =>
            element.Name.LocalName == "rect" &&
            (string?)element.Attribute("fill") == style.TileProgressTrackColor.ToCss() &&
            Math.Abs(double.Parse((string)element.Attribute("height")!,
                System.Globalization.CultureInfo.InvariantCulture) -
                metrics.ProgressRailHeight) < 0.001);
        var railY = double.Parse((string)track.Attribute("y")!,
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(railY, metrics.Y, tileBottom - metrics.ProgressRailHeight);

        var image = canvas.ToRgbaImage();
        var baseline = VisualCanvas.Create(320, 80).AddInfoTileWithTextOptions(8, 8,
            304, 56, "PC", "HOST", "WORKSTATION",
            new VisualCanvasInfoTileTextOptions(textScale: 4)).ToRgbaImage();
        var railX = Math.Clamp((int)Math.Round(metrics.TextX + metrics.TextMax * 0.8),
            0, image.Width - 1);
        var changedRows = new List<int>();
        for (var row = 0; row < image.Height; row++) {
            var pixel = (row * image.Width + railX) * 4;
            if (image.Pixels[pixel] != baseline.Pixels[pixel] ||
                image.Pixels[pixel + 1] != baseline.Pixels[pixel + 1] ||
                image.Pixels[pixel + 2] != baseline.Pixels[pixel + 2] ||
                image.Pixels[pixel + 3] != baseline.Pixels[pixel + 3]) changedRows.Add(row);
        }

        Assert.NotEmpty(changedRows);
        Assert.InRange(changedRows[0], (int)metrics.Y, (int)tileBottom - 1);
        Assert.InRange(changedRows[^1], (int)metrics.Y, (int)tileBottom - 1);
        var railTop = Math.Max(0, (int)Math.Floor(metrics.ProgressRailY));
        var railBottom = Math.Min(image.Height,
            (int)Math.Ceiling(metrics.ProgressRailY + metrics.ProgressRailHeight));
        var changedRailRows = changedRows.Count(row => row >= railTop && row < railBottom);
        Assert.InRange(Math.Abs(changedRailRows - metrics.ProgressRailHeight), 0, 2);
    }

    [Fact]
    public void DefaultProgressRailKeepsItsExistingGeometryOnShortTiles() {
        var canvas = VisualCanvas.Create(320, 80)
            .AddInfoTile(8, 8, 304, 56, "PC", "HOST", "WORKSTATION", progress: 0.45);
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile,
            canvas.ResolveStyle());

        Assert.Equal(8, metrics.ProgressRailHeight);
        Assert.Equal(metrics.Y + metrics.Height - 16, metrics.ProgressRailY);
    }
}
