namespace ChartForgeX.Core;

/// <summary>Controls geometry at X positions absent from a member of one stacked-area group and value axis.</summary>
public enum ChartStackedAreaMissingDataPolicy {
    /// <summary>Retains each series' authored coordinates and breaks. This is the existing default.</summary>
    Independent,
    /// <summary>Draws only positions observed by every member; any absent position breaks the entire stack.</summary>
    Gap,
    /// <summary>Aligns the union of observed positions, giving absent members zero geometric contribution.</summary>
    /// <remarks>Synthetic vertices carry no authored observation, label, marker or source identity.</remarks>
    Zero
}
