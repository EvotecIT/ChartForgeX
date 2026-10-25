using System;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Rendering;

/// <summary>Private immutable finite operation shared by retained root scopes and explicit terminal capture.</summary>
internal sealed class VisualCapturedEffect {
    private VisualCapturedEffect(VisualLayerBlur? blur = null, VisualDropShadow? shadow = null,
        VisualOutline? outline = null, VisualBloom? bloom = null, VisualMask? mask = null, VisualInnerShadow? innerShadow = null) {
        Blur = blur; DropShadow = shadow; Outline = outline; Bloom = bloom; Mask = mask; InnerShadow = innerShadow;
    }
    internal VisualInnerShadow? InnerShadow { get; }
    internal VisualLayerBlur? Blur { get; }
    internal VisualDropShadow? DropShadow { get; }
    internal VisualOutline? Outline { get; }
    internal VisualBloom? Bloom { get; }
    internal VisualMask? Mask { get; }
    internal bool OrderedDither => Blur?.OrderedDither ?? DropShadow?.OrderedDither ??
        Outline?.OrderedDither ?? Bloom?.OrderedDither ?? InnerShadow?.OrderedDither ?? Mask!.OrderedDither;

    internal static VisualCapturedEffect From(VisualInnerShadow effect) => new(innerShadow: effect);
    internal static VisualCapturedEffect From(VisualLayerBlur effect) => new(blur: effect);
    internal static VisualCapturedEffect From(VisualDropShadow effect) => new(shadow: effect);
    internal static VisualCapturedEffect From(VisualOutline effect) => new(outline: effect);
    internal static VisualCapturedEffect From(VisualBloom effect) => new(bloom: effect);
    internal static VisualCapturedEffect From(VisualMask effect) => new(mask: effect);

    internal bool IsUnconditionalIdentity => InnerShadow != null ? InnerShadow.Color.A == 0 || InnerShadow.Opacity == 0
        : Blur != null ? Blur.SigmaX == 0 && Blur.SigmaY == 0
        : DropShadow != null ? DropShadow.Color.A == 0 || DropShadow.Opacity == 0
        : Outline != null ? Outline.Radius == 0 || Outline.Color.A == 0
        : Bloom != null && (Bloom.Threshold16 == 65535 || Bloom.Strength16 == 0);

    /// <summary>Resolves the single existing effect plan without copying source pixels or retaining mutable DTOs.</summary>
    internal RasterEffectPlan? Plan(RasterEffectRegion source, VisualSize size, VisualRenderOptions options) {
        if (InnerShadow is { } inner) {
            if (inner.Color.A == 0 || inner.Opacity == 0) return null;
            return RasterEffectPlan.InnerShadow(source, inner.HorizontalQ8(options.Scale), inner.VerticalQ8(options.Scale),
                checked(inner.OffsetX * options.Scale), checked(inner.OffsetY * options.Scale),
                inner.Color, inner.DeviceSpread(options.Scale), inner.Opacity);
        }
        if (Mask is { } mask) {
            if (mask.PixelsPerUnit != options.Scale)
                throw new ArgumentException("Mask and native capture must use the same device density.", nameof(options));
            return RasterEffectPlan.Mask(source, mask.Image,
                RasterEffectGrid.Coordinate(mask.Bounds.X, options.Scale, nameof(mask)),
                RasterEffectGrid.Coordinate(mask.Bounds.Y, options.Scale, nameof(mask)), mask.Mode);
        }
        if (Blur is { } blur) {
            var x = blur.HorizontalQ8(options.Scale); var y = blur.VerticalQ8(options.Scale);
            return x == 0 && y == 0 ? null : RasterEffectPlan.Blur(source, x, y);
        }
        if (Outline is { } outline) {
            var radius = outline.DeviceRadius(options.Scale);
            return radius == 0 || outline.Color.A == 0 ? null : RasterEffectPlan.Outline(source, outline.Color, radius);
        }
        if (Bloom is { } bloom) {
            var x = bloom.HorizontalQ8(options.Scale); var y = bloom.VerticalQ8(options.Scale);
            return bloom.Threshold16 == 65535 || bloom.Strength16 == 0 ? null
                : RasterEffectPlan.Bloom(source, x, y, bloom.Threshold16, bloom.Strength16);
        }
        var shadow = DropShadow ?? throw new InvalidOperationException("Missing finite capture operation.");
        var sx = shadow.HorizontalQ8(options.Scale); var sy = shadow.VerticalQ8(options.Scale);
        var offsetX = checked(shadow.OffsetX * options.Scale); var offsetY = checked(shadow.OffsetY * options.Scale);
        var spread = checked(shadow.Spread * options.Scale);
        var relative = shadow.ResolveRegion(new ChartRect(0, 0, size.Width, size.Height));
        if (shadow.Color.A == 0 || shadow.Opacity == 0) return null;
        var plan = shadow.Opacity == 1 && relative == null
            ? RasterEffectPlan.LayerShadow(source, sx, sy, offsetX, offsetY, shadow.Color, spread)
            : RasterEffectPlan.LayerShadow(source, sx, sy, offsetX, offsetY, shadow.Color, spread, shadow.Opacity,
                relative.HasValue ? RasterEffectGrid.Cover(relative.Value, options.Scale) : (RasterEffectRegion?)null);
        return plan.ShadowVisibleRegion.HasValue ? plan : null;
    }

    internal CapturedVisualLayer Capture(PreparedVisual source, VisualRenderOptions options, CancellationToken cancellation) {
        if (InnerShadow != null) return source.CaptureLayer(InnerShadow, options, cancellation);
        if (Blur != null) return source.CaptureLayer(Blur, options, cancellation);
        if (DropShadow != null) return source.CaptureLayer(DropShadow, options, cancellation);
        if (Outline != null) return source.CaptureLayer(Outline, options, cancellation);
        if (Bloom != null) return source.CaptureLayer(Bloom, options, cancellation);
        return source.CaptureLayer(Mask!, options, cancellation);
    }

    internal static VisualCapturedEffect From(VisualArtifactInterchangeCapturedEffect effect) {
        if (effect.InnerShadow != null) return From(effect.InnerShadow);
        if (effect.Blur != null) return From(effect.Blur);
        if (effect.DropShadow != null) return From(effect.DropShadow);
        if (effect.Outline != null) return From(effect.Outline);
        if (effect.Bloom != null) return From(effect.Bloom);
        var mask = effect.Mask ?? throw new ArgumentException("Missing finite effect payload.", nameof(effect));
        return From(new VisualMask(new RgbaImage(mask.ImageWidth, mask.ImageHeight, mask.Pixels),
            new ChartPoint(mask.OriginX, mask.OriginY), mask.PixelsPerUnit, mask.Mode, mask.OrderedDither));
    }

    /// <summary>Builds a temporary borrowed wire view for complete-envelope admission; the caller copies only after validation.</summary>
    internal VisualArtifactInterchangeCapturedEffect ToPortableBorrowed() {
        var result = new VisualArtifactInterchangeCapturedEffect {
            Blur = Blur, DropShadow = DropShadow, Outline = Outline, Bloom = Bloom, InnerShadow = InnerShadow
        };
        if (Mask is { } mask) {
            var image = mask.Image;
            result.Mask = new VisualArtifactInterchangeMask {
                ImageWidth = image.Width, ImageHeight = image.Height,
                OriginX = mask.Bounds.X, OriginY = mask.Bounds.Y,
                PixelsPerUnit = mask.PixelsPerUnit, Mode = mask.Mode,
                OrderedDither = mask.OrderedDither, Pixels = image.Pixels
            };
        }
        return result;
    }
}
