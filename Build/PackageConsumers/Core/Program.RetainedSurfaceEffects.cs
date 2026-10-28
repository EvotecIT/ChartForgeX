using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void RetainedSurfaceEffectsContracts() {
        var model = SequenceArtifact.Create("package-surfaces").WithSize(800, 600)
            .AddParticipant("A", "Review").AddParticipant("B", "Archive").AddMessage("A", "B", "Save");
        model.Participants[0].Href = "#guide";
        model.Participants[1].SurfaceShadow = new VisualDropShadow(ChartColor.Black, 4, 4, 0,
            opacity: .06, relativeRegion: new ChartRect(-.1, -.1, 1.3, 1.3));
        model.AddNote(SequenceArtifactNotePlacement.Over, new[] { "A", "B" }, "Retain facts");
        model.Paint.NoteShadow = new VisualDropShadow(ChartColor.Black, 1, 2, 2, opacity: .25);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 900), 24));
        var prepared = model.Prepare(context);
        var native = prepared.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters), CancellationToken.None);
        var baked = prepared.ToSvg();
        var json = prepared.ToArtifact(model.Id, VisualArtifactKind.Sequence).ToInterchangeJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToSequenceArtifact();
        PackageAssertions.Require(native.Contains("<filter") && native.Contains("<text") && !native.Contains("<image") &&
            baked.Contains("<image") && baked.Contains("href=\"#guide\""), "Retained surface profiles lost vectors, links or shadow representation.");
        PackageAssertions.Require(restored.Participants[1].SurfaceShadow!.Opacity == .06 &&
            restored.Paint.NoteShadow!.Opacity == .25, "Retained surface facts did not restore.");
        var before = prepared.ToPng(null, CancellationToken.None);
        model.Participants[1].SurfaceShadow = null;
        PackageAssertions.Require(Convert.ToBase64String(before) == Convert.ToBase64String(prepared.ToPng()),
            "Producer mutation changed retained effect pixels.");
        restored.Messages[0].Text = "Saved and edited";
        PackageAssertions.Require(restored.ToSvg().Contains("Saved and edited"), "Portable surface facts were not editable.");
    }
}
