using System;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Projects supported packet declarations without changing their retained source settings.</summary>
internal sealed class MermaidPacketSettings {
    internal int? BitsPerRow { get; private set; }
    internal bool? ShowBits { get; private set; }
    internal PacketBitOrder? BitOrder { get; private set; }
    internal double? RowHeight { get; private set; }
    internal double? BitWidth { get; private set; }
    internal double? PaddingX { get; private set; }
    internal double? PaddingY { get; private set; }

    internal static bool IsPath(string path) => path == "packet.bitsPerRow" || path == "packet.showBits" || path == "packet.bitOrder"
        || path == "packet.rowHeight" || path == "packet.bitWidth" || path == "packet.paddingX" || path == "packet.paddingY";
    internal static bool IsHandledPath(string path) => IsPath(path) || path.StartsWith("packet.bitsPerRow.", StringComparison.Ordinal)
        || path.StartsWith("packet.showBits.", StringComparison.Ordinal) || path.StartsWith("packet.bitOrder.", StringComparison.Ordinal)
        || path.StartsWith("packet.rowHeight.", StringComparison.Ordinal) || path.StartsWith("packet.bitWidth.", StringComparison.Ordinal)
        || path.StartsWith("packet.paddingX.", StringComparison.Ordinal) || path.StartsWith("packet.paddingY.", StringComparison.Ordinal);

    internal static MermaidPacketSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidPacketSettings();
        var rows = configuration.Last("packet.bitsPerRow");
        if (rows != null) {
            if (rows.TryGetNumber(out var value)
                && value >= 1 && value <= 512 && value == Math.Truncate(value)) settings.BitsPerRow = (int)value;
            else error(rows, "must be an integer from 1 through 512");
        }
        var numbers = configuration.Last("packet.showBits");
        if (numbers != null) {
            if (!numbers.IsString && bool.TryParse(numbers.Value, out var visible)) settings.ShowBits = visible;
            else error(numbers, "must be a boolean");
        }
        var order = configuration.Last("packet.bitOrder");
        if (order != null) {
            if (order.IsString && order.Value == "ascending") settings.BitOrder = PacketBitOrder.Ascending;
            else if (order.IsString && order.Value == "descending") settings.BitOrder = PacketBitOrder.Descending;
            else error(order, "must be 'ascending' or 'descending'");
        }
        settings.RowHeight = Dimension("packet.rowHeight", 1);
        settings.BitWidth = Dimension("packet.bitWidth", 1);
        settings.PaddingX = Dimension("packet.paddingX", 0);
        settings.PaddingY = Dimension("packet.paddingY", 0);
        return settings;

        double? Dimension(string path, double minimum) {
            var setting = configuration.Last(path);
            if (setting == null) return null;
            if (setting.TryGetNumber(out var value) && value >= minimum) return value;
            error(setting, "must be a finite number of at least " + minimum);
            return null;
        }
    }
}
