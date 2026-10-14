using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    internal Chart PanelView() {
        var view = (Chart)MemberwiseClone();
        view.Options = Options.PanelView();
        return view;
    }
    /// <summary>Omits the outer chart frame and padding when the host already provides them.</summary>
    public Chart WithHostFrame(bool hostOwnsFrame = true) { Options.HostOwnsFrame = hostOwnsFrame; return this; }
    /// <summary>Sets optional line markers while keeping all data available for interaction and accessibility.</summary>
    public Chart WithLineMarkers(ChartLineMarkerMode mode) {
        if (!Enum.IsDefined(typeof(ChartLineMarkerMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        Options.LineMarkerMode = mode; return this;
    }
}
