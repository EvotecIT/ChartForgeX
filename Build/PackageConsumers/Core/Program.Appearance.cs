using System;
using ChartForgeX;
using ChartForgeX.Effects;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void NativeAppearanceContracts() {
        PackageAssertions.Owner(typeof(VisualOutline), "ChartForgeX");
        PackageAssertions.Owner(typeof(VisualBloom), "ChartForgeX");
        var model = SequenceArtifact.Create("package-appearance").WithSize(320, 320)
            .AddParticipant("review", "Review").AddParticipant("archive", "Archive")
            .AddMessage("review", "archive", "Save");
        var prepared = model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 320), 20)));
        var outline = prepared.CaptureLayer(new VisualOutline(ChartForgeX.Primitives.ChartColor.Black, 1));
        var noBright = prepared.CaptureLayer(new VisualBloom(2, 1, 1));
        var original = prepared.CaptureLayer(new VisualLayerBlur(0));
        PackageAssertions.Require(outline.Source == prepared && outline.Bounds.Width > prepared.Size.Width,
            "Core outline lost its native source or outward support.");
        PackageAssertions.Require(System.Linq.Enumerable.SequenceEqual(original.ToImage().Pixels, noBright.ToImage().Pixels),
            "Threshold one changed captured source pixels.");
        PackageAssertions.Owner(typeof(VisualInnerShadow), "ChartForgeX");
        var innerPolicy = new VisualInnerShadow(ChartForgeX.Primitives.ChartColor.Black, 2, 2, 1,
            spread: 1, orderedDither: false, opacity: .5);
        var inner = prepared.CaptureLayer(innerPolicy, new VisualRenderOptions(supersampling: 1));
        var retainedInner = prepared.WithInnerShadow(innerPolicy);
        var innerJson = inner.ToArtifact().ToInterchangeJson();
        var innerEnvelope = VisualArtifactInterchangeEnvelope.FromJson(innerJson);
        var replayedInner = innerEnvelope.ApplyCapturedLayer(prepared);
        PackageAssertions.Require(System.Linq.Enumerable.SequenceEqual(inner.ToImage().Pixels, replayedInner.ToImage().Pixels),
            "Core inset capture changed pixels through typed ordered replay.");
        PackageAssertions.Require(inner.Bounds.Width == prepared.Size.Width && inner.Bounds.Height == prepared.Size.Height,
            "Core inset capture changed the original frame.");
        PackageAssertions.Require(retainedInner.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters))
            .IndexOf("operator=\"atop\"", StringComparison.Ordinal) >= 0, "Native inset filter lost SourceAtop.");
        PackageAssertions.Png(inner.Visual.ToPng());
        PackageAssertions.Png(outline.Visual.ToPng());
        var glowPolicy = new VisualDropShadow(ChartForgeX.Primitives.ChartColor.FromRgb(37, 123, 219),
            0, 0, 3, spread: 1, orderedDither: false, opacity: .6);
        var glow = prepared.CaptureLayer(glowPolicy, new VisualRenderOptions(supersampling: 1));
        var retainedGlow = prepared.WithDropShadow(glowPolicy);
        var savedGlow = VisualArtifactInterchangeEnvelope.FromJson(glow.ToArtifact().ToInterchangeJson());
        var replayedGlow = savedGlow.ApplyCapturedLayer(prepared);
        PackageAssertions.Require(glow.Source == prepared && glow.Bounds.X < 0
            && glow.Bounds.Width > prepared.Size.Width, "Zero-offset alpha glow lost outward source support.");
        PackageAssertions.Require(System.Linq.Enumerable.SequenceEqual(glow.ToImage().Pixels, replayedGlow.ToImage().Pixels),
            "The typed shadow recipe did not replay its native pixels.");
        PackageAssertions.Require(retainedGlow.Size.Equals(prepared.Size), "Fixed-root glow silently expanded its source viewport.");
        PackageAssertions.Png(glow.Visual.ToPng());
    }
}
