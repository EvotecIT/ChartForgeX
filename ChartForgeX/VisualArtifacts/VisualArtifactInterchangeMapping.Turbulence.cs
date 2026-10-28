using System;
using ChartForgeX.Accessibility;
using ChartForgeX.Effects;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Restores one typed procedural source through its native owner and an explicit fresh caller context.</summary>
    /// <param name="envelope">Validated pure Unknown/None source facts, without a carried captured-effect chain.</param>
    /// <param name="context">Explicit same-size viewport, insets, theme, fonts and frame for fresh native preparation.</param>
    /// <returns>A detached preparation retaining source identity, extensions and accessibility with actual fresh frame facts.</returns>
    /// <remarks>The original resolved theme and regions are evidence, not an editable frame or scene command stream.
    /// Explicit context headings win; omitted headings use the envelope captions. To edit carried appearance, clear
    /// CapturedEffects on a detached source copy, rebuild the immutable TurbulenceSource selection, prepare here,
    /// then replay the original envelope through its existing ApplyCapturedEffects or ApplyCapturedLayer entrypoint.
    /// No RNG state, sampled geometry, pixels, stale value/visibility plans or external provider is reconstructed.</remarks>
    public static PreparedVisual PrepareTurbulenceSource(this VisualArtifactInterchangeEnvelope envelope, VisualRenderContext context) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        if (context == null) throw new ArgumentNullException(nameof(context));
        envelope.Validate();
        var policy = envelope.Presentation?.TurbulenceSource
            ?? throw new NotSupportedException("A typed complete-root TurbulenceSource is required.");
        RequireNoCapturedEffectPresentation(envelope);
        if (envelope.Width != context.Layout.Size.Width || envelope.Height != context.Layout.Size.Height)
            throw new ArgumentException("The explicit procedural source context must match the captured source viewport exactly.", nameof(context));
        var frame = context.Frame.WithHeadings(context.Frame.Title ?? envelope.Title, context.Frame.Subtitle ?? envelope.Subtitle);
        var actual = new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, frame, context.Font)
            .WithNativeTextCapture(context.NativeTextCapture);
        return policy.PrepareSource(actual, envelope);
    }

    /// <summary>Captures source intent and current common frame facts without imposing serialization budgets on static paint.</summary>
    internal static VisualArtifactInterchangeEnvelope FromPreparedTurbulence(VisualTurbulence policy,
        VisualRenderContext context, VisualArtifactInterchangeEnvelope? evidence) {
        var result = new VisualArtifactInterchangeEnvelope {
            Id = evidence?.Id ?? "turbulence", Kind = VisualArtifactKind.Unknown, Family = VisualArtifactInterchangeFamily.None,
            SourceLanguage = evidence?.SourceLanguage ?? VisualArtifactSourceLanguage.Native,
            Width = context.Layout.Size.Width, Height = context.Layout.Size.Height,
            Title = context.Frame.Title ?? "", Subtitle = context.Frame.Subtitle ?? "",
            AccessibleName = evidence?.AccessibleName, AccessibleDescription = evidence?.AccessibleDescription,
            Language = evidence?.Language, IsDecorative = evidence?.IsDecorative ?? false,
            Presentation = new VisualArtifactInterchangePresentation {
                TurbulenceSource = policy,
                Theme = MapTheme(context.Theme.Resolve(context.ThemeMode), context.Font.Family)
            }
        };
        var theme = result.Presentation.Theme!;
        theme.TitleSize = context.Theme.Typography.TitleSize; theme.SubtitleSize = context.Theme.Typography.SubtitleSize;
        theme.DataLabelSize = context.Theme.Typography.DataLabelSize; theme.LegendSize = context.Theme.Typography.LegendSize;
        if (evidence != null) foreach (var pair in evidence.Extensions) result.Extensions.Add(pair.Key, pair.Value);
        return result;
    }

    internal static VisualAccessibility TurbulenceAccessibility(VisualArtifactInterchangeEnvelope? evidence) => new() {
        Name = evidence?.AccessibleName, Description = evidence?.AccessibleDescription,
        Language = evidence?.Language, IsDecorative = evidence?.IsDecorative ?? false
    };
}
