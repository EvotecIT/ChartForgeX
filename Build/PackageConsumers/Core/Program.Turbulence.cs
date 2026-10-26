using System;
using ChartForgeX;
using ChartForgeX.SvgRaster;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void TurbulenceContracts() {
        PackageAssertions.Owner(typeof(VisualTurbulence), "ChartForgeX");
        PackageAssertions.Owner(typeof(VisualFilterColorSpace), "ChartForgeX");
        var effect = new VisualTurbulence(.071, .113, 3, -7.9, VisualTurbulenceType.FractalNoise, stitchTiles: true);
        var first = effect.Generate(new VisualSize(32, 24)); var next = effect.Generate(new VisualSize(32, 24));
        PackageAssertions.Require(!ReferenceEquals(first.Pixels, next.Pixels) && first.Pixels.SequenceEqual(next.Pixels),
            "Procedural generation lost deterministic detached pixel ownership.");
        var prepared = effect.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(32, 24), 0),
            frame: new VisualFrame(transparentBackground: true)));
        var normalized = new VisualTurbulence(.071, .113, 3, 8, VisualTurbulenceType.FractalNoise, stitchTiles: true)
            .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(32, 24), 0),
                frame: new VisualFrame(transparentBackground: true)));
        PackageAssertions.Require(prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels
            .SequenceEqual(normalized.ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels),
            "Native procedural preparation lost the retained raw-seed normalization policy.");
        PackageAssertions.Require(prepared.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)).Contains("feTurbulence"),
            "Native procedural SVG profile lost its finite source.");
        PackageAssertions.Png(prepared.ToPng());
        var saved=prepared.ToArtifact("package-noise").ToInterchangeJson();
        var envelope=VisualArtifactInterchangeEnvelope.FromJson(saved);
        var restored=envelope.PrepareTurbulenceSource(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(32,24),0),
            frame:new VisualFrame(transparentBackground:true)));
        var presentation=envelope.Presentation ?? throw new InvalidOperationException("Missing typed presentation.");
        var policy=presentation.TurbulenceSource ?? throw new InvalidOperationException("Missing typed source.");
        PackageAssertions.Require(policy.Seed==effect.Seed && envelope.Version==2,
            "Portable procedural source lost its raw seed or typed version boundary.");
        PackageAssertions.Require(prepared.ToPng().SequenceEqual(restored.ToPng()),"Portable procedural source changed the same-context native output.");
        presentation.TurbulenceSource=new VisualTurbulence(policy.BaseFrequencyX,policy.BaseFrequencyY,policy.NumberOfOctaves,14,
            policy.Type,policy.StitchTiles,policy.ColorSpace);
        var edited=envelope.PrepareTurbulenceSource(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(32,24),0),
            frame:new VisualFrame(transparentBackground:true)));
        PackageAssertions.Png(edited.ToPng());
        PackageAssertions.Require(edited.ToArtifact().ToInterchangeEnvelope().Presentation!.TurbulenceSource!.Seed==14,
            "Ordinary portable source edit was not retained by the current native producer.");
        const string affineSource = "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48'><defs>" +
            "<filter id='n' x='0' y='0' width='24' height='16' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>" +
            "<feTurbulence baseFrequency='.071 .113' numOctaves='3' seed='13' type='fractalNoise' stitchTiles='stitch'/></filter></defs>" +
            "<rect width='24' height='16' fill='white' transform='matrix(1,.25,.5,1,8,8)' filter='url(#n)'/></svg>";
        var affine = SvgRasterizer.Rasterize(affineSource, strict: true, filters: new SvgRasterFilterOptions());
        PackageAssertions.Require(affine.Diagnostics.Count == 0 && affine.Image.Pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha > 0),
            "Finite affine procedural source did not reach native pixels through the public importer.");
        PackageAssertions.Png(affine.Image.ToPng());

    }
}
