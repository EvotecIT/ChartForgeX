using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Html;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Interactivity.Html;

/// <summary>One bounded actual-HTML transport snapshot across the accepted wide/narrow emissions.</summary>
internal static class HtmlCapturedFontAssets {
    internal static IReadOnlyList<Payload> Write(HtmlMarkupWriter writer, params VisualSvgFontResources?[] plans) {
        var payloads = new List<Payload>();
        var roots = new List<(TrueTypeFont Font, string Id)>();
        var faces = new List<(VisualSvgFontResources.Face Face, string Root)>();
        long bytes = 0;
        foreach (var plan in plans) {
            if (plan == null) continue;
            foreach (var face in plan.Faces) {
                var font = face.Font; string? rootId = null;
                foreach (var existing in roots) if (existing.Font.CapturedByteIdentity == font.CapturedByteIdentity && existing.Font.SameCapturedBytes(font)) { rootId = existing.Id; break; }
                if (rootId == null) {
                    var length = font.CapturedByteLength;
                    if (length > OwnedFontResources.MaximumRootBytes || roots.Count >= OwnedFontResources.MaximumRoots
                        || bytes + length > OwnedFontResources.MaximumDistinctBytes) continue; // Automatic keeps this line's native contours.
                    rootId = font.CapturedByteIdentity; roots.Add((font, rootId)); bytes += length;
                }
                faces.Add((face, rootId));
            }
        }
        // Admission precedes every root copy. Repeated aliases/descriptors share one copied root view.
        foreach (var root in roots) {
            var encoded = Convert.ToBase64String(root.Font.CopyCapturedBytes(OwnedFontResources.MaximumRootBytes));
            string Record(string bytes) => new HtmlMarkupWriter().StartElement("template").Attribute("data-cfx-font-root", root.Id)
                .Attribute("data-cfx-font-byte-length", root.Font.CapturedByteLength.ToString(CultureInfo.InvariantCulture))
                .EndStartElement().Text(bytes).EndElement().Build();
            var full = Record(encoded); writer.RawTrusted(full); payloads.Add(new Payload(full, Record(string.Empty)));
        }
        foreach (var entry in faces) writer.StartElement("template").Attribute("data-cfx-font-face", entry.Face.Alias)
            .Attribute("data-cfx-font-resource", entry.Root).Attribute("data-cfx-font-weight", entry.Face.Weight.ToString(CultureInfo.InvariantCulture))
            .Attribute("data-cfx-font-style", entry.Face.Italic ? "italic" : "normal").EndStartElement().EndElement();
        return payloads.AsReadOnly();
    }
    internal sealed class Payload {
        internal Payload(string full, string observation) { Full = full; Observation = observation; }
        internal string Full { get; }
        internal string Observation { get; }
    }

    internal static VisualSvgTextPresentation Resolve(HtmlChartTextPresentation value) => value switch {
        HtmlChartTextPresentation.Automatic => VisualSvgTextPresentation.Automatic,
        HtmlChartTextPresentation.NativeContours => VisualSvgTextPresentation.NativeContours,
        HtmlChartTextPresentation.Browser => VisualSvgTextPresentation.Browser,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}
