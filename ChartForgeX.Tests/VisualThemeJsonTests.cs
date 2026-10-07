using System.Text.Json.Nodes;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualThemeJsonTests {
    [Fact]
    public void VersionedThemeRoundTripsPairedColorsAlphaStatusRampsTypographyAndGeometry() {
        var light = VisualDesignTokens.GraphiteLight();
        var dark = VisualDesignTokens.GraphiteDark();
        light.Background = ChartColor.FromHex("#11223344");
        light.SecondaryAccent = ChartColor.FromHex("#A1B2C3D4");
        light.Positive = ChartColor.FromHex("#10203040");
        light.Muted = null;
        light.Grid = ChartColor.FromHex("#55667788");
        dark.Status.Info = new VisualTokenColor(ChartColor.FromHex("#12345678"), ChartColor.FromHex("#ABCDEF12"));
        var typography = new VisualTypography("Example \"Family\", C:\\Fonts\tΩ", 27.25, 14.5, 10.75, 12.25, 11.5);
        var original = new VisualTheme(light, dark, typography, 13.5, 2.75, 4.25, 0.375, 5.5, 0.5, 1.25);
        string json = original.ToThemeJson();
        var imported = VisualTheme.FromThemeJson(json);
        Assert.Equal(json, imported.ToThemeJson());
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var expected = original.Resolve(mode);
            var actual = imported.Resolve(mode);
            Assert.Equal(new[] { expected.Background, expected.Surface, expected.ElevatedSurface, expected.Foreground, expected.MutedForeground, expected.Border, expected.Accent },
                new[] { actual.Background, actual.Surface, actual.ElevatedSurface, actual.Foreground, actual.MutedForeground, actual.Border, actual.Accent });
            Assert.Equal(expected.Palette, actual.Palette);
            Assert.Equal(expected.SequentialRamp, actual.SequentialRamp);
            Assert.Equal(expected.DivergingRamp!.Negative, actual.DivergingRamp!.Negative);
            Assert.Equal(expected.DivergingRamp.Neutral, actual.DivergingRamp.Neutral);
            Assert.Equal(expected.DivergingRamp.Positive, actual.DivergingRamp.Positive);
            Assert.Equal(StatusPairs(expected.Status), StatusPairs(actual.Status));
        }
        Assert.Equal(typography.Family, imported.Typography.Family);
        Assert.Equal(new[] { 27.25, 14.5, 10.75, 12.25, 11.5 }, new[] { imported.Typography.TitleSize, imported.Typography.SubtitleSize, imported.Typography.AxisSize, imported.Typography.LegendSize, imported.Typography.DataLabelSize });
        Assert.Equal(new[] { 13.5, 2.75, 4.25, 0.375, 5.5, 0.5, 1.25 }, new[] { imported.Spacing, imported.SeriesStrokeWidth, imported.MarkerRadius, imported.AreaOpacity, imported.BarRadius, imported.GridStrokeWidth, imported.AxisStrokeWidth });
        var document = JsonNode.Parse(json)!;
        Assert.Equal(1, document["schemaVersion"]!.GetValue<int>());
        Assert.Equal("#11223344", document["light"]!["surface"]!["page"]!.GetValue<string>());
        Assert.Equal("#A1B2C3D4", document["light"]!["chrome"]!["accent"]!.GetValue<string>());
        Assert.Equal("#10203040", document["light"]!["roles"]!["positive"]!.GetValue<string>());
        Assert.Null(document["light"]!["roles"]!["muted"]);
        Assert.Equal("#55667788", document["light"]!["roles"]!["grid"]!.GetValue<string>());
    }

    [Fact]
    public void CanonicalPaletteImportRemainsSeparateFromVersionedThemeImport() {
        var document = JsonNode.Parse(VisualTheme.Graphite().ToThemeJson())!;
        document.AsObject().Remove("schemaVersion");
        document.AsObject().Remove("typography");
        document.AsObject().Remove("geometry");
        string palette = document.ToJsonString();
        var imported = VisualTheme.FromJson(palette, new VisualTypography(titleSize: 30));
        Assert.Equal(VisualTheme.Graphite().Resolve(VisualThemeMode.Dark).Palette, imported.Resolve(VisualThemeMode.Dark).Palette);
        Assert.Equal(30, imported.Typography.TitleSize);
        Assert.Throws<ArgumentException>(() => VisualTheme.FromThemeJson(palette));
    }

    [Fact]
    public void OptionalRampsAndIndependentRoleNullsRemainOptional() {
        var light = new VisualDesignTokens { Muted = null, Grid = null, Axis = null };
        var original = new VisualTheme(light, light);
        var imported = VisualTheme.FromThemeJson(original.ToThemeJson());
        Assert.Empty(imported.Resolve(VisualThemeMode.Light).SequentialRamp);
        Assert.Null(imported.Resolve(VisualThemeMode.Dark).DivergingRamp);
        Assert.Equal(original.ToThemeJson(), imported.ToThemeJson());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(1.5)]
    public void UnsupportedSchemaVersionsCannotSilentlyDropThemeSettings(double version) {
        var document = JsonNode.Parse(VisualTheme.Graphite().ToThemeJson())!;
        document["schemaVersion"] = version;
        Assert.ThrowsAny<ArgumentException>(() => VisualTheme.FromThemeJson(document.ToJsonString()));
    }

    [Theory]
    [InlineData("areaOpacity", -0.1)]
    [InlineData("areaOpacity", 1.1)]
    [InlineData("spacing", -1)]
    [InlineData("seriesStrokeWidth", -1)]
    public void InvalidGeometryIsRejectedAtTheThemeBoundary(string property, double value) {
        var document = JsonNode.Parse(VisualTheme.Graphite().ToThemeJson())!;
        document["geometry"]![property] = value;
        Assert.ThrowsAny<ArgumentException>(() => VisualTheme.FromThemeJson(document.ToJsonString()));
    }

    [Fact]
    public void MalformedOrUnboundedThemeDocumentsAreRejectedBeforeProjection() {
        string json = VisualTheme.Graphite().ToThemeJson();
        Assert.Throws<ArgumentException>(() => VisualTheme.FromThemeJson(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal)));
        var document = JsonNode.Parse(json)!;
        document["typography"]!["titleSize"] = 0;
        Assert.ThrowsAny<ArgumentException>(() => VisualTheme.FromThemeJson(document.ToJsonString()));
        document = JsonNode.Parse(json)!;
        document["geometry"]!.AsObject().Remove("markerRadius");
        Assert.Throws<ArgumentException>(() => VisualTheme.FromThemeJson(document.ToJsonString()));
        document = JsonNode.Parse(json)!;
        document["light"]!["roles"]!["positive"] = "rgba(1,2,3,0.4)";
        Assert.Throws<ArgumentException>(() => VisualTheme.FromThemeJson(document.ToJsonString()));
        Assert.Throws<ArgumentException>(() => VisualTheme.FromThemeJson(new string(' ', 1024 * 1024 + 1)));
        Assert.Throws<ArgumentException>(() => VisualTheme.FromThemeJson("{\"ignored\":" + new string('[', 33) + "0" + new string(']', 33) + "}"));
        var colors = Enumerable.Repeat(ChartColor.FromHex("#0000FF"), 257).ToArray();
        Assert.Throws<ArgumentException>(() => new VisualTheme(new VisualDesignTokens { Palette = colors }, new VisualDesignTokens()).ToThemeJson());
    }

    private static VisualTokenColor[] StatusPairs(VisualStatusTokens status) =>
        new[] { status.Critical, status.High, status.Medium, status.Low, status.Info, status.Pass, status.Neutral, status.Maintenance };
}
