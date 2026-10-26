using System;
using System.Globalization;
using System.IO;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static string TurbulenceId(string prefix, int index) => prefix + "-procedural-turbulence-" + index.ToString(CultureInfo.InvariantCulture);
    private static void WriteProceduralSource(SvgMarkupWriter writer, VisualProceduralSourcePlan plan,
        string prefix, int index, VisualSvgOptions? options, CancellationToken cancellation) {
        var source = plan.Source; var bounds = source.Bounds; var effect = source.Effect;
        if (options?.EffectProfile == VisualSvgEffectProfile.NativeFilters) {
            writer.StartElement("defs").EndStartElement();
            writer.StartElement("filter").Attribute("id", TurbulenceId(prefix, index))
                .Attribute("filterUnits", "userSpaceOnUse").Attribute("primitiveUnits", "userSpaceOnUse")
                .Attribute("x", bounds.X).Attribute("y", bounds.Y).Attribute("width", bounds.Width).Attribute("height", bounds.Height)
                .Attribute("color-interpolation-filters", effect.ColorSpace == VisualFilterColorSpace.Srgb ? "sRGB" : "linearRGB").EndStartElement();
            var octaves = plan.Plan.TurbulenceData!.EffectiveOctaves;
            // Emit effective finite kernel inputs. Original raw seed/frequency/count stay on the immutable source/identity.
            var frequencyX = octaves == 0 ? 0 : effect.BaseFrequencyX;
            var frequencyY = octaves == 0 ? 0 : effect.BaseFrequencyY;
            writer.StartElement("feTurbulence").Attribute("baseFrequency", EffectNumber(frequencyX) + " " + EffectNumber(frequencyY))
                .Attribute("seed", RasterTurbulenceLattice.NormalizeSeed(effect.Seed)).Attribute("numOctaves", octaves)
                .Attribute("type", effect.Type == VisualTurbulenceType.FractalNoise ? "fractalNoise" : "turbulence")
                .Attribute("stitchTiles", effect.StitchTiles ? "stitch" : "noStitch").EndEmptyElement();
            writer.EndElement(); writer.EndElement();
            // This one surface IS the procedural source; there is no cloned/hidden semantic SourceGraphic child.
            writer.StartElement("rect").Attribute("x", bounds.X).Attribute("y", bounds.Y)
                .Attribute("width", bounds.Width).Attribute("height", bounds.Height)
                .Attribute("fill", "none").Attribute("filter", "url(#" + TurbulenceId(prefix, index) + ")");
            Semantics(writer, source, prefix, index);
            writer.EndEmptyElement();
        } else {
            var image = plan.Capture(cancellation);
            cancellation.ThrowIfCancellationRequested();
            var encoded = PngWriter.WriteRgba(image);
            cancellation.ThrowIfCancellationRequested();
            var sample = plan.SamplingBounds;
            // Clip through the one finite source extent. The common scene preserves frame/title semantics independently.
            writer.StartElement("svg").Attribute("x", bounds.X).Attribute("y", bounds.Y)
                .Attribute("width", bounds.Width).Attribute("height", bounds.Height)
                .Attribute("viewBox", EffectNumber(bounds.X) + " " + EffectNumber(bounds.Y) + " " + EffectNumber(bounds.Width) + " " + EffectNumber(bounds.Height))
                .Attribute("overflow", "hidden").EndStartElement();
            writer.StartElement("image").Attribute("x", sample.X).Attribute("y", sample.Y)
                .Attribute("width", sample.Width).Attribute("height", sample.Height).Attribute("preserveAspectRatio", "none")
                .Attribute("href", "data:image/png;base64," + Convert.ToBase64String(encoded));
            Semantics(writer, source, prefix, index); writer.EndEmptyElement(); writer.EndElement();
        }
    }

    private static void TurbulenceIdentity(BinaryWriter writer, VisualSceneProceduralSource source) {
        Rectangle(writer, source.Bounds);
        var effect = source.Effect;
        writer.Write(effect.BaseFrequencyX); writer.Write(effect.BaseFrequencyY); writer.Write(effect.Seed);
        writer.Write(effect.NumberOfOctaves); writer.Write((int)effect.Type); writer.Write(effect.StitchTiles); writer.Write((int)effect.ColorSpace);
    }
}
