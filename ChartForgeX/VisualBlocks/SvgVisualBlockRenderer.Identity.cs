using System.Globalization;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static string BuildProvisionalId(IVisualBlock block, string idScope) {
        var options = block.Options;
        return SvgRenderedIdentity.CreateProvisionalId(
            "cfx-visual",
            idScope,
            block.GetType().FullName ?? block.GetType().Name,
            block.AccessibleName,
            options.Size.Width.ToString(CultureInfo.InvariantCulture),
            options.Size.Height.ToString(CultureInfo.InvariantCulture));
    }

    private static string BindVisualIdentity(string svg, string provisionalId, string idScope) {
        return SvgRenderedIdentity.Bind(svg, provisionalId, "cfx-visual", idScope);
    }
}
