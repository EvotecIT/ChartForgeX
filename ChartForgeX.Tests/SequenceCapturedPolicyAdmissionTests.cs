using System;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceCapturedPolicyAdmissionTests {
    [Theory]
    [InlineData("navigation")]
    [InlineData("lifeline")]
    [InlineData("surface")]
    public void RepresentedNonLayoutPolicyRestoresItsPairedThemeWithoutChangingStaticPaint(string policy) {
        var source = SequenceArtifact.Create("captured-non-layout").WithSize(1000,820)
            .AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Save");
        source.ThemeMode = VisualThemeMode.Dark;
        source.Theme = VisualTheme.Graphite().Configure(builder => builder.Dark.Background = ChartColor.FromHex("#112233"));
        if (policy == "navigation") source.Participants[0].NavigationLinks = new[] { new VisualNavigationLink("Guide","#guide") };
        else if (policy == "lifeline") source.Paint.LifelineStroke = new ChartStrokeStyle(width:.5,dash:Array.Empty<double>());
        else source.Participants[0].SurfaceShadow = new VisualDropShadow(ChartColor.Black,4,4,0,orderedDither:false,opacity:.06);
        var first = SequencePreparedCompiler.PrepareDefault(source);
        SequenceMessageFixture.Capture("captured-non-layout-"+policy,first);
        var raw = source.ToVisualArtifact().ToInterchangeEnvelope();
        Assert.Null(raw.Sequence!.Layout);Assert.Null(raw.Sequence.Typography);
        Assert.NotNull(raw.Sequence.Theme);Assert.Equal(VisualThemeMode.Dark,raw.Sequence.ThemeMode);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(raw.ToJson()).ToSequenceArtifact();
        Assert.Equal(source.ToSvg(),restored.ToSvg());Assert.Equal(source.ToPng(),restored.ToPng());
        Assert.Equal(first.ToSvg(),source.ToSvg());
    }

    [Fact]
    public void BareThemeWithoutAnyRepresentedPolicyStillRejects() {
        var source = SequenceArtifact.Create("bare-theme").AddParticipant("A").AddParticipant("B").AddMessage("A","B","Save");
        var envelope = source.ToVisualArtifact().ToInterchangeEnvelope();
        envelope.Sequence!.Theme = VisualTheme.Graphite();
        envelope.Sequence.ThemeMode = VisualThemeMode.Light;
        Assert.Throws<ArgumentException>(() => envelope.Validate());
    }
}
