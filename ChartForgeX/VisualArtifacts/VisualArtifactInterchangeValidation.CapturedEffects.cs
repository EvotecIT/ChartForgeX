using System;
using System.Collections.Generic;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    /// <summary>Checks one bounded ordered presentation and every raw mask snapshot before replay or copying.</summary>
    internal static void CapturedEffects(VisualArtifactInterchangePresentation presentation,
        VisualArtifactInterchangeEnvelope envelope) {
        var effects = presentation.CapturedEffects;
        if (effects.Count == 0) return;
        if (!envelope.Width.HasValue || !envelope.Height.HasValue)
            throw new ArgumentException("Captured effects require the complete positive source viewport.", nameof(envelope));
        if (effects.Count > VisualSceneRasterRenderer.MaximumIsolatedDepth)
            throw new NotSupportedException("Captured effects exceed the existing native capture depth.");
        var borrowed = new HashSet<byte[]>();
        long borrowedBytes = 0, snapshotBytes = 0, encodedCharacters = 0;
        int? maskDensity = null;
        int? captureDensity = null;
        var terminal = false;
        foreach (var effect in effects) {
            if (effect == null) throw new ArgumentException("A captured effect cannot be null.", nameof(envelope));
            var payloads = (effect.Blur != null ? 1 : 0) + (effect.DropShadow != null ? 1 : 0) +
                (effect.Outline != null ? 1 : 0) + (effect.Bloom != null ? 1 : 0) + (effect.Mask != null ? 1 : 0) +
                (effect.InnerShadow != null ? 1 : 0);
            if (payloads != 1) throw new ArgumentException("A captured step requires exactly one active effect payload.", nameof(envelope));
            if (terminal && (effect.Mask == null || effect.CaptureFrame != null))
                throw new NotSupportedException("Only the existing captured-layer mask operation may follow a terminal capture.");
            if (effect.CaptureFrame is { } frame) {
                terminal = true;
                var options = frame.RasterOptions ?? throw new ArgumentException("Capture raster options cannot be null.", nameof(envelope));
                captureDensity = options.Scale;
                var bounds = frame.Bounds;
                var x = RasterEffectGrid.Coordinate(bounds.X, options.Scale, nameof(frame.Bounds));
                var y = RasterEffectGrid.Coordinate(bounds.Y, options.Scale, nameof(frame.Bounds));
                var width = RasterEffectGrid.Coordinate(bounds.Width, options.Scale, nameof(frame.Bounds));
                var height = RasterEffectGrid.Coordinate(bounds.Height, options.Scale, nameof(frame.Bounds));
                _ = new RasterEffectRegion(x, y, width, height);
                _ = VisualSceneRasterRenderer.CalculateAllocation(new VisualSize(envelope.Width.Value, envelope.Height.Value),
                    options.Scale, options.Supersampling, options.PixelBudget);
            } else if (!terminal && effect.Blur != null) {
                throw new NotSupportedException("Blur requires an explicit terminal CaptureLayer frame; no root WithBlur policy is represented.");
            }
            if (effect.Mask is not { } mask) continue;
            _ = new VisualRenderOptions(scale: mask.PixelsPerUnit);
            if (maskDensity.HasValue && mask.PixelsPerUnit != maskDensity.Value)
                throw new ArgumentException("Ordered masks must share one native device density.", nameof(envelope));
            maskDensity = mask.PixelsPerUnit;
            var image = new RgbaImage(mask.ImageWidth, mask.ImageHeight, mask.Pixels);
            var required = checked((long)mask.ImageWidth * mask.ImageHeight * 4);
            if (mask.Pixels.LongLength != required)
                throw new ArgumentException("Portable masks require the exact declared RGBA byte length.", nameof(envelope));
            _ = VisualMask.ValidateInput(image, new ChartPoint(mask.OriginX, mask.OriginY), mask.PixelsPerUnit, mask.Mode);
            if (borrowed.Add(mask.Pixels)) borrowedBytes = checked(borrowedBytes + required);
            snapshotBytes = checked(snapshotBytes + required);
            encodedCharacters = checked(encodedCharacters + Base64Length(required));
        }
        if (maskDensity.HasValue && captureDensity.HasValue && maskDensity.Value != captureDensity.Value)
            throw new ArgumentException("Captured masks and terminal output must use one native density.", nameof(envelope));
        if (encodedCharacters > MaximumJsonCharacters)
            throw new ArgumentOutOfRangeException(nameof(envelope), "Captured effect samples exceed the common JSON character budget before encoding.");
        RasterEffectPreflight.ValidateCapture(null, borrowedBytes, snapshotBytes, checked(snapshotBytes * 3), out _, out _);
    }
}
