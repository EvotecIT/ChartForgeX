using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Optional internal resource budget/translation on the existing artifact export policy.</summary>
internal interface IVisualArtifactResourcePolicy {
    int? MaximumSvgCharacters { get; }
    Exception TranslateResourceFailure(VisualResourceLimitException exception);
}
