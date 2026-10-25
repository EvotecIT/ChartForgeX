using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Composition;

public sealed partial class ImageComposition {
    /// <summary>Draws one immutable captured native layer, placing its selected source frame at x/y and retaining effect overflow.</summary>
    /// <param name="layer">A completed explicit capture; its private pixels are borrowed without modification.</param>
    /// <param name="x">Destination position of the original child's left edge in composition logical units.</param>
    /// <param name="y">Destination position of the original child's top edge in composition logical units.</param>
    /// <returns>The current composition.</returns>
    /// <remarks>Only the destination composition frame clips the effect. Density conversion uses the existing native affine image sampler.
    /// Capture is complete before this operation mutates any pixels; no scene parsing, font resolution, filter or theme inference occurs here.</remarks>
    public ImageComposition DrawLayer(CapturedVisualLayer layer, double x, double y) {
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        ValidateFinite(x, nameof(x)); ValidateFinite(y, nameof(y));
        var bounds = layer.Bounds; var image = layer.Image;
        // Preserve fractional logical support at captured/output densities; the int-coordinate fitting route would round it away.
        _canvas.DrawImageTransformed(image.Width, image.Height, image.Pixels,
            bounds.Width / image.Width, 0, 0, bounds.Height / image.Height, x + bounds.X, y + bounds.Y);
        return this;
    }
}
