namespace ChartForgeX.Core;

/// <summary>Identifies an authored range-bar endpoint independently of numeric or visual ordering.</summary>
public enum ChartRangeBarEndpoint {
    /// <summary>The authored interval Start, which may be greater than End.</summary>
    Start,
    /// <summary>The authored interval End, which may be less than Start.</summary>
    End
}
