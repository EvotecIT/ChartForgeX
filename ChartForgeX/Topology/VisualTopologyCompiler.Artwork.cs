using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private bool CanRenderNodeArtwork(TopologyNode node) {
        var artwork = ResolveRenderableNodeArtwork(node, _options);
        if (artwork == null) return false;
        if (artwork.ImageHref is string href) {
            if (href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) {
                if (TryCaptureArtworkImage(node, href, new ChartRect(0, 0, node.Width, node.Height), artwork.PreserveAspectRatio, out _)) return true;
            } else if (VisualSceneImageResource.IsSafeHref(href)) return true;
        }
        if (!artwork.HasSvgBody) return false;
        return TryCaptureArtworkFragment(node, artwork.SvgBody!, artwork.SvgViewBox, artwork.PreserveAspectRatio,
            new ChartRect(0, 0, node.Width, node.Height), out _);
    }

    private bool BuildArtwork(TopologyNode node, ChartRect bounds, bool selectedOutline = true, double? opacity = null, ChartColor? fallbackColor = null, Action? beforePaint = null) {
        var artwork = ResolveRenderableNodeArtwork(node, _options);
        if (artwork == null) return false;
        // Frame legend markers are compiled before the topology layout and highlight state exist.
        // Synthetic glyph callers supply their parent's opacity; only real node artwork resolves state here.
        var active = opacity.HasValue || _highlight.IsNodeHighlighted(node);
        var imageOpacity = opacity ?? (active || !_highlight.IsActive ? 1 : _highlight.DimmedOpacity);
        if (artwork.ImageHref is string href) {
            if (href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) {
                if (TryCaptureArtworkImage(node, href, bounds, artwork.PreserveAspectRatio, out var resource)) {
                    beforePaint?.Invoke();
                    _builder.Image(resource, bounds, "topology-node-artwork", opacity: imageOpacity, preserveAspectRatio: artwork.PreserveAspectRatio);
                    ArtworkSelection(node, bounds, active, selectedOutline); return true;
                }
            } else if (VisualSceneImageResource.IsSafeHref(href)) {
                using (_builder.PushImageResource(href, bounds, artwork.PreserveAspectRatio, imageOpacity, "topology-node-artwork")) {
                    var centerX = (bounds.X + bounds.Width / 2 - _offsetX) / _scale;
                    var centerY = (bounds.Y + bounds.Height / 2 - _offsetY) / _scale;
                    var color = fallbackColor ?? Highlight(NodeAccent(node), active);
                    BuildGlyph(node, centerX, centerY, color, Math.Min(bounds.Width, bounds.Height) / (26 * _scale),
                        artworkOpacity: imageOpacity, allowArtwork: false);
                }
                _builder.AddDiagnostic(new VisualDiagnostic("topology.artwork-external-raster-fallback", "Host-managed image references remain in SVG; native raster uses the canonical glyph without network or file access."));
                ArtworkSelection(node, bounds, active, selectedOutline); return true;
            }
        }
        if (artwork.HasSvgBody) {
            if (TryCaptureArtworkFragment(node, artwork.SvgBody!, artwork.SvgViewBox, artwork.PreserveAspectRatio, bounds, out var resource)) {
                beforePaint?.Invoke();
                _builder.Image(resource, bounds, "topology-node-artwork", opacity: imageOpacity);
                ArtworkSelection(node, bounds, active, selectedOutline); return true;
            }
        }
        _builder.AddDiagnostic(new VisualDiagnostic("topology.artwork-fallback", "Artwork could not be resolved into the immutable scene; the canonical node glyph is used."));
        return false;
    }

    private static (int Width, int Height) ArtworkSamplingSize(ChartRect bounds) {
        var factor = Math.Min(2, 2048d / Math.Max(bounds.Width, bounds.Height));
        return (Math.Max(1, (int)Math.Ceiling(bounds.Width * factor)), Math.Max(1, (int)Math.Ceiling(bounds.Height * factor)));
    }

    private void ArtworkSelection(TopologyNode node, ChartRect bounds, bool active, bool enabled) {
        if (!enabled || !_options.SelectedNodeIds.Contains(node.Id)) return;
        var color = Highlight(NodeAccent(node), active);
        _builder.Rect(bounds, null, color, 2.8 * _scale, _context.Theme.BarRadius * _scale, "topology-node-artwork-selection",
            paint: new VisualScenePaintBinding(stroke: NodeAccentPaint(node, color, active)));
    }
}
