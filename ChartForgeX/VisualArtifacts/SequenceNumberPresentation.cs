namespace ChartForgeX.VisualArtifacts;

/// <summary>Selects the finite native presentation of existing visible sequence occurrence numbers.</summary>
public enum SequenceNumberPresentation {
    /// <summary>Keeps the ordinary inline number prefix or explicitly styled text run.</summary>
    Inline = 0,
    /// <summary>Uses a separately measured native circle and number beside the message endpoint selected by its terminal notation.</summary>
    Badge = 1
}
