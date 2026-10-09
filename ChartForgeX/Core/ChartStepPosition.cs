namespace ChartForgeX.Core;

/// <summary>Positions a step transition within each pair of connected observations.</summary>
public enum ChartStepPosition {
    /// <summary>Changes to the next value at the preceding observation's x coordinate.</summary>
    Start,
    /// <summary>Changes value halfway between the two mapped x coordinates.</summary>
    Middle,
    /// <summary>Holds the preceding value until the next observation's x coordinate.</summary>
    End
}
