using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidFlowchartAssets {
    internal static void Apply(TopologyChart chart, MermaidFlowchartDocument source, MermaidFlowchartRenderOptions options) {
        var catalog = SourceCatalog(options.IconCatalog ?? TopologyIconCatalog.Default());
        var images = new Dictionary<string, TopologyIconArtwork>(StringComparer.Ordinal);
        var used = false;
        for (var index = 0; index < source.Nodes.Count; index++) {
            var node = source.Nodes[index]; if (!HasAsset(node)) continue;
            var target = chart.Nodes[index]; var imageKey = Value(node, "img");
            RetainFacts(target.Metadata, node);
            if (!Dimension(node, "w", out var width, out var error) || !Dimension(node, "h", out var height, out error))
                throw new ArgumentException(error, nameof(source));
            if (imageKey != null) {
                if (options.ImageBindings == null || !options.ImageBindings.TryGetValue(imageKey, out var image)) continue;
                if (!images.TryGetValue(imageKey, out var artwork)) {
                    artwork = TopologyIconArtwork.Portrait(image).WithPreserveAspectRatio("none");
                    var length = artwork.ImageHref!.Length;
                    if (length > VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters)
                        throw new MermaidResourceLimitException(nameof(VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters),
                            VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters, length, Span(node, "img"));
                    images.Add(imageKey, artwork);
                }
                Image(target, node, image, artwork, width, height, source.Configuration.Graph?.WrappingWidth ?? 120, chart.Theme.Typography.DataLabelSize);
                used = true; continue;
            }
            var identity = Value(node, "icon")!; var icon = catalog.Resolve(identity);
            if (icon == null || !string.Equals(icon.QualifiedId, identity, StringComparison.Ordinal)) continue;
            target.IconId = identity; target.IconShape = icon.Shape; target.Kind = icon.NodeKind; target.Symbol = icon.Symbol;
            target.Artwork = icon.Artwork?.Clone(); if (string.IsNullOrWhiteSpace(target.Color)) target.Color = icon.Color;
            target.DisplayMode = TopologyNodeDisplayMode.Icon; target.PreserveDisplayModeSize = true; target.FitLabelToContent = false;
            var size = Math.Max(width ?? 48, height ?? 48); target.Width = size; target.Height = size; target.GlyphScale = size / 26;
            var form = Value(node, "form");
            var authoredFill = MermaidNativeStyles.Paint(node.Styles, "fill");
            var authoredStroke = MermaidNativeStyles.Paint(node.Styles, "stroke");
            target.BorderColor = authoredFill ?? chart.Theme.Resolve(chart.ThemeMode).Surface.ToHexRgba();
            if (form == "circle") {
                target.Shape = TopologyNodeShape.Circle;
                var glyph = TopologyGlyphBounds.Measure(target, new TopologyRenderOptions { IconCatalog = catalog }, chart.Theme.AxisStrokeWidth, chart.Theme.BarRadius).Geometry;
                target.Width = target.Height = Math.Max(glyph.Width, glyph.Height) * Math.Sqrt(2) + 40;
            } else if (form is "square" or "rounded") {
                target.Shape = form == "square" ? TopologyNodeShape.Rectangle : TopologyNodeShape.Rounded;
                target.Width = target.Height = size + (source.Configuration.Graph?.ContentPadding ?? 15);
                target.CornerRadius = form == "square" ? .1 : 5;
            } else {
                target.Shape = TopologyNodeShape.Rectangle;
                // The source unframed form has no default contour. The native glyph remains its existing separate paint owner.
                target.BorderColor = authoredStroke ?? "none";
                if (authoredFill == null && authoredStroke == null) target.SurfaceOpacity = 0;
                else if (authoredFill == null) target.BackgroundColor = "none";
            }
            target.MaximumTextWidth = Math.Max(size, source.Configuration.Graph?.WrappingWidth ?? 120);
            Caption(target, node, chart.Theme.Typography.DataLabelSize);
            if (target.ShowLabel) (chart.DefaultRenderOptions ??= new TopologyRenderOptions()).IncludeIconLabels = true;
            target.Metadata["mermaid.asset.kind"] = "icon"; used = true;
        }
        if (used) (chart.DefaultRenderOptions ??= new TopologyRenderOptions()).IconCatalog = catalog;
        foreach (var group in source.Subgraphs) {
            var target = chart.Groups.Find(item => item.Id == group.Id); if (target == null) continue;
            foreach (var pair in group.Properties) target.Metadata["mermaid.property." + pair.Key] = pair.Value;
            foreach (var pair in group.PropertyTokens) target.Metadata["mermaid.property." + pair.Key + ".raw"] = pair.Value;
        }
    }

    private static void Image(TopologyNode target, MermaidFlowchartNode source, RgbaImage pixels, TopologyIconArtwork artwork,
        double? width, double? height, double wrappingWidth, double captionSize) {
        var caption = HasCaption(source); var rawWidth = Math.Max(caption ? wrappingWidth : 0, width ?? pixels.Width);
        var constrained = Value(source, "constraint") == "on";
        var aspect = pixels.Width / (double)pixels.Height;
        target.Width = constrained && height.HasValue ? height.Value * aspect : rawWidth;
        target.Height = constrained ? target.Width / aspect : height ?? pixels.Height;
        target.DisplayMode = TopologyNodeDisplayMode.Artwork; target.Shape = TopologyNodeShape.Rectangle; target.CornerRadius = 0;
        target.PreserveDisplayModeSize = true; target.FitLabelToContent = false; target.Artwork = artwork.Clone();
        target.IconId = null; target.IconShape = null; target.Symbol = null; target.Kind = TopologyNodeKind.Generic;
        target.MaximumTextWidth = Math.Max(target.Width, wrappingWidth);
        Caption(target, source, captionSize); target.Metadata["mermaid.asset.kind"] = "image";
    }

    private static void Caption(TopologyNode target, MermaidFlowchartNode source, double captionSize) {
        target.ShowLabel = HasCaption(source); target.CaptionSurface = TopologyCaptionSurface.Plain;
        if (target.ShowLabel) {
            target.CaptionPlacement = Value(source, "pos") == "t" ? TopologyCaptionPlacement.Top : TopologyCaptionPlacement.Bottom;
            target.CaptionGap = 8;
            target.CaptionTextStyle = new ChartForgeX.Typography.TextStyleOverride { FontSize = target.TextStyle?.FontSize ?? captionSize, FontWeight = target.TextStyle?.FontWeight ?? "400" };
        }
    }

    private static TopologyIconCatalog SourceCatalog(TopologyIconCatalog source) {
        var catalog = source.Clone();
        foreach (var pack in catalog.Packs) foreach (var icon in pack.Icons) {
            var artwork = icon.Artwork; if (artwork == null) continue;
            artwork.SvgPath = null; artwork.PreviewPath = null;
            if (artwork.ImageHref is string href && !href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) artwork.ImageHref = null;
            if (artwork.ImageHref == null && !artwork.HasSvgBody) icon.Artwork = null;
        }
        return catalog;
    }

    private static void RetainFacts(Dictionary<string, string> metadata, MermaidFlowchartNode node) {
        foreach (var pair in node.Properties) {
            if (!IsMetadataKey(pair.Key)) continue;
            var prefix = "mermaid.property." + pair.Key;
            if (node.PropertyTokens.TryGetValue(pair.Key, out var raw)) metadata[prefix + ".raw"] = raw;
            var span = Span(node, pair.Key);
            metadata[prefix + ".line"] = span.Line.ToString(CultureInfo.InvariantCulture);
            metadata[prefix + ".column"] = span.Column.ToString(CultureInfo.InvariantCulture);
            metadata[prefix + ".length"] = span.Length.ToString(CultureInfo.InvariantCulture);
        }
    }
}
