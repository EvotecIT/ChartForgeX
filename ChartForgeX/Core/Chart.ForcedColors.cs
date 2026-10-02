namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Keeps state marks in their colours in forced-colours mode (see <see cref="ChartOptions.PinStateColorsInForcedColors"/>).
    /// </summary>
    /// <param name="pin">True to pin the state colours; false (the default) lets forced colours apply.</param>
    /// <returns>The current chart.</returns>
    public Chart WithStateColorsPinnedInForcedColors(bool pin = true) {
        Options.PinStateColorsInForcedColors = pin;
        return this;
    }
}
