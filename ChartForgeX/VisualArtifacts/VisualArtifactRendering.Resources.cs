using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactRendering {
    private static int? SvgCharacterCeiling(VisualArtifact artifact) =>
        (artifact.ExportPolicy as IVisualArtifactResourcePolicy)?.MaximumSvgCharacters;

    private static T ExportWithResourceTranslation<T>(VisualArtifact artifact, Func<T> export) {
        try { return export(); }
        catch (VisualResourceLimitException exception) {
            if (artifact.ExportPolicy is IVisualArtifactResourcePolicy policy) {
                var translated = policy.TranslateResourceFailure(exception);
                if (!ReferenceEquals(translated, exception)) throw translated;
            }
            throw;
        }
    }
}
