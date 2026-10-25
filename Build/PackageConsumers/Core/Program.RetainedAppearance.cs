using System;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void RetainedAppearanceContracts() {
        PackageAssertions.Owner(typeof(VisualArtifactInterchangeCapturedEffect), "ChartForgeX");
        var model = SequenceArtifact.Create("package-retained-appearance").WithSize(320, 320)
            .AddParticipant("review", "Review").AddParticipant("archive", "Archive")
            .AddMessage("review", "archive", "Save");
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 320), 20));
        var source = model.Prepare(context);
        var retained = source.WithDropShadow(new VisualDropShadow(ChartColor.Black, 2, 3, 1, opacity: .15))
            .WithOutline(new VisualOutline(ChartColor.FromHex("#0369A1"), 1))
            .WithBloom(new VisualBloom(1, .7, .3));
        var saved = retained.ToArtifact().ToInterchangeJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var facts = VisualArtifactInterchangeEnvelope.FromJson(saved);
        facts.Presentation!.CapturedEffects.Clear();
        var editable = facts.ToSequenceArtifact();
        var replayed = envelope.ApplyCapturedEffects(editable.Prepare(context));
        PackageAssertions.Require(retained.ToPng().SequenceEqual(replayed.ToPng()), "Ordered retained effect replay changed native pixels.");
        editable.Messages[0].Text = "Approved";
        var edited = envelope.ApplyCapturedEffects(editable.Prepare(context));
        PackageAssertions.Require(edited.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)).Contains("Approved"),
            "Edited typed source was replaced with stale captured text.");
        var capture = retained.CaptureLayer(new VisualLayerBlur(1), new VisualRenderOptions(supersampling: 1));
        var terminal = VisualArtifactInterchangeEnvelope.FromJson(capture.ToArtifact().ToInterchangeJson());
        var restored = terminal.ApplyCapturedLayer(source);
        PackageAssertions.Require(terminal.Width == 320 && terminal.Height == 320 && capture.Bounds.Width > 320,
            "Source viewport was silently rebased to outward captured pixels.");
        PackageAssertions.Require(restored.Bounds.Equals(capture.Bounds) && restored.ToImage().Pixels.SequenceEqual(capture.ToImage().Pixels),
            "Terminal replay lost its actual output frame or pixels.");
        PackageAssertions.Png(restored.Visual.ToPng());
    }
}
