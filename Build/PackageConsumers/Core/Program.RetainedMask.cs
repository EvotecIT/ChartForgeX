using System;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void RetainedMaskContracts() {
        var model = SequenceArtifact.Create("package-retained-mask").WithSize(320, 500)
            .AddParticipant("A", "Review").AddParticipant("B", "Archive").AddMessage("A", "B", "Save");
        model.Participants[0].Href = "#guide";
        var source = model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 500), 20)));
        var pixels = Enumerable.Repeat((byte)255, 320 * 500 * 4).ToArray();
        for (var at = 3; at < pixels.Length; at += 4) pixels[at] = 180;
        var mask = new VisualMask(new RgbaImage(320, 500, pixels), orderedDither: false);
        var masked = source.WithMask(mask);
        var before = masked.ToPng();
        var native = masked.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var artifact = masked.ToArtifact();
        PackageAssertions.Require(native.Contains("<mask") && native.Contains("<text") && native.Contains("href=\"#guide\""),
            "Retained mask lost its vector source, link or explicit mask input.");
        var saved = artifact.ToInterchangeJson();
        var captured = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var replayed = captured.ApplyCapturedEffects(source);
        PackageAssertions.Require(artifact.SupportsExport(VisualArtifactExportFormat.Json) && masked.Regions.Count == source.Regions.Count,
            "Masked presentation lost typed JSON or source regions.");
        PackageAssertions.Require(before.SequenceEqual(replayed.ToPng()), "Portable mask replay changed the native captured presentation.");
        try { captured.ToSequenceArtifact(); throw new InvalidOperationException("Editable-only import silently discarded captured masks."); }
        catch (NotSupportedException) { }
        var sourceFacts = VisualArtifactInterchangeEnvelope.FromJson(saved);
        sourceFacts.Presentation!.CapturedEffects.Clear();
        var editable = sourceFacts.ToSequenceArtifact();
        editable.Messages[0].Text = "Approved";
        var editedSource = editable.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 500), 20)));
        var edited = captured.ApplyCapturedEffects(editedSource);
        PackageAssertions.Require(edited.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)).Contains("Approved"),
            "Ordinary source editing failed to replay the saved mask with live words.");
        var record = captured.Presentation!.CapturedEffects[0].Mask!;
        Array.Clear(record.Pixels, 0, record.Pixels.Length);
        PackageAssertions.Require(before.SequenceEqual(replayed.ToPng()), "Portable replay retained mutable DTO pixel inputs.");
        Array.Clear(pixels, 0, pixels.Length);
        model.Participants[0].Label = "Changed producer";
        PackageAssertions.Require(Convert.ToBase64String(before) == Convert.ToBase64String(masked.ToPng()),
            "Retained mask kept mutable producer data.");
        PackageAssertions.Require(source.ToArtifact().ToInterchangeEnvelope().Nodes.Count == 2,
            "Unmasked source facts stopped supporting ordinary interchange.");
    }
}
