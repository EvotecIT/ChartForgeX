using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    internal Chart PanelView() {
        var view = (Chart)MemberwiseClone();
        view.Options = Options.PanelView();
        return view;
    }
}
