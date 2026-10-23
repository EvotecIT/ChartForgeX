using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RailroadInterchangeTests {
    [Theory]
    [MemberData(nameof(MermaidRailroadParserTests.CommonSources), MemberType = typeof(MermaidRailroadParserTests))]
    public void FourFrontendArtifactsSerializeImportEditAndRenderThroughTheNativeRuleOwner(string dialect, string source) {
        var parsed = new MermaidParser().ParseRailroad(source); Assert.Empty(parsed.Diagnostics);
        var artifact = parsed.Document!.ToVisualArtifact(new MermaidRailroadRenderOptions {
            Presentation = new MermaidPresentationOptions { Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), ThemeMode = VisualThemeMode.Dark }
        });
        artifact.Metadata["host-note"] = "<grammar> is inert data"; artifact.Accessibility.Name = "Portable grammar";
        artifact.Accessibility.Description = "Two symbolic rules"; artifact.Accessibility.Language = "en";
        var original = Assert.IsType<RailroadDiagramBlock>(artifact.Model);
        original.Options.Accessibility.Name = artifact.Accessibility.Name;
        original.Options.Accessibility.Description = artifact.Accessibility.Description;
        original.Options.Accessibility.Language = artifact.Accessibility.Language;
        var json = artifact.ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal(VisualArtifactInterchangeEnvelope.CurrentVersion, envelope.Version);
        Assert.Equal(VisualArtifactInterchangeFamily.Railroad, envelope.Family); Assert.NotNull(envelope.Railroad);
        Assert.Equal(VisualArtifactSourceLanguage.Mermaid, envelope.SourceLanguage);
        Assert.Equal(artifact.Metadata["mermaid.railroadDialect"], envelope.Extensions["mermaid.railroadDialect"]);
        Assert.Equal("<grammar> is inert data", envelope.Extensions["host-note"]); Assert.Equal(json, envelope.ToJson());
        Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json()).ToJson());
        var imported = envelope.ToRailroadDiagramBlock();
        Assert.Equal(original.RuleComparison, imported.RuleComparison);
        Assert.Equal(original.Rules.Select(rule => rule.Id), imported.Rules.Select(rule => rule.Id));
        for (var index = 0; index < original.Rules.Count; index++) Equal(original.Rules[index].Expression, imported.Rules[index].Expression);
        Assert.Equal("Portable grammar", imported.Options.Accessibility.Name);
        Assert.Equal("Two symbolic rules", imported.Options.Accessibility.Description); Assert.Equal("en", imported.Options.Accessibility.Language);
        Assert.Equal(original.Options.Theme.ToJson(), imported.Options.Theme.ToJson());
        Assert.Equal(original.RenderSvg("portable-railroad"), imported.RenderSvg("portable-railroad"));
        Assert.Equal(original.ToPng(), imported.ToPng());
        var before = imported.RenderSvg("portable-railroad");
        imported.AddRule("additional", RailroadExpression.Terminal("added"), "Additional rule").WithNaturalSize();
        var edited = imported.RenderSvg("portable-railroad"); Assert.NotEqual(before, edited); Assert.Contains("Additional rule", edited);
        Assert.Equal(2, original.Rules.Count); Assert.Equal(2, envelope.Railroad!.Rules.Count);
        var image = RasterImageDecoder.Decode(imported.ToPng());
        Assert.Equal(imported.Options.Size.Width, image.Width); Assert.Equal(imported.Options.Size.Height, image.Height);
        Save(dialect, imported);
    }

    [Fact]
    public void NativeRulesLabelsAndEveryExpressionKindRetainTheirOwnSemanticFieldsWithoutExpansion() {
        var expression = RailroadExpression.Sequence(RailroadExpression.Empty(), RailroadExpression.Terminal("", false),
            RailroadExpression.Reference("recursive"), RailroadExpression.Choice(new[] { RailroadExpression.Terminal("first"), RailroadExpression.Terminal("second") }, ordered: true),
            RailroadExpression.Optional(RailroadExpression.AnyCharacter()), RailroadExpression.Repeat(RailroadExpression.Reference("recursive"), 2, int.MaxValue),
            RailroadExpression.Lookahead(RailroadExpression.CharacterRange(0x1f600, 0x1f64f)),
            RailroadExpression.Lookahead(RailroadExpression.Terminal("blocked"), positive: false),
            RailroadExpression.Except(RailroadExpression.AnyCharacter(), RailroadExpression.Special("host-defined condition")));
        var block = RailroadDiagramBlock.Create().WithTitle("Native grammar").WithSubtitle("Semantic portable form").WithPadding(23, 17, 29, 19)
            .WithTransparentBackground().WithCard(false).WithPngOutputScale(2);
        block.RuleComparison = RailroadRuleComparison.OrdinalIgnoreCase; block.AddRule("recursive", expression, "Displayed heading");
        block.AddRule("labelled", RailroadExpression.Terminal("<&\"😀"), "A separate label");
        var artifact = VisualArtifact.Create("native-grammar", VisualArtifactKind.VisualBlock, block);
        var envelope = artifact.ToInterchangeEnvelope(); block.AddRule("later", RailroadExpression.Empty());
        Assert.Equal(2, envelope.Railroad!.Rules.Count);
        var imported = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToRailroadDiagramBlock();
        Assert.Equal("Native grammar", imported.Title); Assert.Equal("Semantic portable form", imported.Subtitle);
        Assert.Equal(new[] { "Displayed heading", "A separate label" }, imported.Rules.Select(rule => rule.Label));
        Assert.Equal(block.RuleComparison, imported.RuleComparison); Equal(expression, imported.Rules[0].Expression);
        Assert.Equal(2, imported.Options.PngOutputScale); Assert.True(imported.Options.TransparentBackground); Assert.False(imported.Options.ShowCard);
        Assert.Equal((23d, 17d, 29d, 19d), (imported.Options.Padding.Left, imported.Options.Padding.Top, imported.Options.Padding.Right, imported.Options.Padding.Bottom));
    }

    [Fact]
    public void IndexedWireRetainsNativeDepthWithoutRaisingTheExistingCommonJsonDepthLimit() {
        var expression = RailroadExpression.Terminal("x");
        for (var depth = 1; depth < 128; depth++) expression = RailroadExpression.Optional(expression);
        var envelope = Native(expression); var json = envelope.ToJson();
        Equal(expression, VisualArtifactInterchangeEnvelope.FromJson(json).Railroad!.Rules[0].Expression);
        envelope.Railroad!.Rules[0] = new RailroadRule("root", "Root", RailroadExpression.Optional(expression));
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }

    [Theory]
    [InlineData("\"railroadArguments\":[0]", "\"railroadArguments\":[1]")]
    [InlineData("\"railroadArguments\":[0]", "\"railroadArguments\":[2]")]
    [InlineData("\"railroadArguments\":[0]", "\"railroadArguments\":[-1]")]
    [InlineData("\"railroadArguments\":[0]", "\"railroadArguments\":[0.5]")]
    [InlineData("\"railroadArguments\":[0]", "\"railroadArguments\":[0,0]")]
    [InlineData("\"kind\":\"Terminal\"", "\"kind\":\"Choice\"")]
    [InlineData("\"kind\":\"Terminal\"", "\"kind\":\"ExecuteCallback\"")]
    [InlineData("\"maximum\":5", "\"maximum\":-1")]
    [InlineData("\"expression\":1", "\"expression\":0")]
    [InlineData("\"ruleComparison\":\"Ordinal\"", "\"ruleComparison\":\"42\"")]
    public void MalformedIndicesArityKindsBoundsAndUnreachableRecordsAreRejectedBeforeNativeRendering(string before, string after) {
        var json = Native(RailroadExpression.Repeat(RailroadExpression.Terminal("x"), 0, 5)).ToJson(); Assert.Contains(before, json);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(before, after, StringComparison.Ordinal)));
    }

    [Fact]
    public void ReusedExpressionGraphsCannotExpandPastTheNativeOccurrenceBudgetDuringImport() {
        var leaf = "{\"kind\":\"Terminal\",\"value\":\"x\",\"railroadArguments\":[]}";
        var records = new List<string> { leaf };
        for (var index = 1; index < 17; index++) records.Add("{\"kind\":\"Sequence\",\"railroadArguments\":[" + (index - 1) + "," + (index - 1) + "]}");
        var json = "{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"id\":\"grammar\",\"kind\":\"VisualBlock\",\"sourceLanguage\":\"Native\",\"family\":\"Railroad\",\"railroad\":{\"ruleComparison\":\"Ordinal\",\"railroadRules\":[{\"id\":\"root\",\"label\":\"Root\",\"expression\":16}],\"railroadExpressions\":[" + string.Join(",", records) + "]}}";
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json));
    }

    [Fact]
    public void NativeRuleAndOccurrenceLimitsAreSharedWithTypedSerialization() {
        var envelope = Native(RailroadExpression.Empty());
        for (var index = 1; index < 512; index++) envelope.Railroad!.Rules.Add(new RailroadRule("rule-" + index, "Rule", RailroadExpression.Empty()));
        Assert.Equal(512, VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).Railroad!.Rules.Count);
        envelope.Railroad!.Rules.Add(new RailroadRule("overflow", "Too many", RailroadExpression.Empty()));
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope = Native(RailroadExpression.Sequence(Enumerable.Repeat(RailroadExpression.Terminal("x"), 32767)));
        Assert.Equal(32768, envelope.Railroad!.Rules[0].Expression.Children.Count + 1);
        Assert.Equal(envelope.ToJson(), VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToJson());
        envelope.Railroad.Rules.Add(new RailroadRule("overflow", "One more", RailroadExpression.Empty()));
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }

    [Fact]
    public void WrongFamilyDuplicateIdsAndOutOfBoundsNativePresentationFailAtImport() {
        var generic = new VisualArtifactInterchangeEnvelope { Id = "generic", Kind = VisualArtifactKind.VisualBlock };
        var genericJson = generic.ToJson(); Assert.DoesNotContain("\"railroad\"", genericJson);
        Assert.Equal(genericJson, VisualArtifactInterchangeEnvelope.FromJson(genericJson).ToJson());
        Assert.Throws<NotSupportedException>(() => generic.ToRailroadDiagramBlock());
        var envelope = Native(RailroadExpression.Empty()); envelope.Family = VisualArtifactInterchangeFamily.None;
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToRailroadDiagramBlock());
        envelope.Family = VisualArtifactInterchangeFamily.Railroad; envelope.Railroad!.RuleComparison = RailroadRuleComparison.OrdinalIgnoreCase;
        envelope.Railroad.Rules.Add(new RailroadRule("ROOT", "Duplicate", RailroadExpression.Empty()));
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson()); envelope.Railroad.Rules.RemoveAt(1);
        envelope.Width = 520.5; Assert.ThrowsAny<ArgumentException>(() => envelope.ToRailroadDiagramBlock()); envelope.Width = 520;
        envelope.Railroad.PngOutputScale = 5; Assert.ThrowsAny<ArgumentException>(() => envelope.ToRailroadDiagramBlock());
    }

    private static VisualArtifactInterchangeEnvelope Native(RailroadExpression expression) =>
        VisualArtifact.Create("grammar", VisualArtifactKind.VisualBlock, RailroadDiagramBlock.Create().AddRule("root", expression, "Root")).ToInterchangeEnvelope();
    private static void Equal(RailroadExpression expected, RailroadExpression actual) {
        Assert.Equal((expected.Kind, expected.Value, expected.CaseSensitive, expected.OrderedChoice, expected.MinimumOccurrences, expected.MaximumOccurrences,
            expected.FirstCodePoint, expected.LastCodePoint), (actual.Kind, actual.Value, actual.CaseSensitive, actual.OrderedChoice,
            actual.MinimumOccurrences, actual.MaximumOccurrences, actual.FirstCodePoint, actual.LastCodePoint));
        Assert.Equal(expected.Children.Count, actual.Children.Count);
        for (var index = 0; index < expected.Children.Count; index++) Equal(expected.Children[index], actual.Children[index]);
    }
    private static void Save(string dialect, RailroadDiagramBlock block) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "railroad-interchange-" + dialect + ".svg"), block.RenderSvg("portable-grammar"));
        File.WriteAllBytes(Path.Combine(directory, "railroad-interchange-" + dialect + ".png"), block.ToPng());
    }
}
