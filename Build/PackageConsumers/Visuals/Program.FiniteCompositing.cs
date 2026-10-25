using System;
using System.Linq;
using System.Threading;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void FiniteCompositeContracts() {
        var model = SequenceArtifact.Create("package-composite").WithSize(320, 360)
            .AddParticipant("A", "Review").AddParticipant("B", "Archive").AddMessage("A", "B", "Save");
        var prepared = model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 360), 20)));
        var layer = prepared.CaptureLayer(new VisualLayerBlur(0), new VisualRenderOptions(supersampling: 1));
        var copy = ImageComposition.Create(340, 380, ChartColor.White)
            .DrawLayer(layer, 10, 10, VisualCompositeOperator.Copy, orderedDither: true);
        var image = copy.ToImage();
        var source = layer.ToImage();
        for (var y = 0; y < source.Height; y++) {
            var actual = image.Pixels.Skip(((y + 10) * image.Width + 10) * 4).Take(source.Width * 4);
            PackageAssertions.Require(actual.SequenceEqual(source.Pixels.Skip(y * source.Width * 4).Take(source.Width * 4)),
                "Visuals Copy lost exact captured RGBA.");
        }
        PackageAssertions.Require(image.Pixels[3] == 0, "Copy retained backdrop outside its source extent.");
        PackageAssertions.Png(copy.ToPng());
        var before = copy.ToPng();
        try {
            copy.DrawLayer(layer, 10, 10, VisualCompositeOperator.DestinationOut, cancellationToken: new CancellationToken(true));
            throw new InvalidOperationException("Cancelled composition unexpectedly committed.");
        } catch (OperationCanceledException) { }
        PackageAssertions.Require(before.SequenceEqual(copy.ToPng()), "Cancelled composition changed the destination.");
    }
}
