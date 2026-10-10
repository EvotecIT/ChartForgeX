using System;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Projects supported packet declarations without changing their retained source settings.</summary>
internal sealed class MermaidPacketSettings {
    internal int? BitsPerRow { get; private set; }
    internal bool? ShowBits { get; private set; }
    internal PacketBitOrder? BitOrder { get; private set; }

    internal static bool IsPath(string path) => path == "packet.bitsPerRow" || path == "packet.showBits" || path == "packet.bitOrder";
    internal static bool IsHandledPath(string path) => IsPath(path) || path.StartsWith("packet.bitsPerRow.", StringComparison.Ordinal)
        || path.StartsWith("packet.showBits.", StringComparison.Ordinal) || path.StartsWith("packet.bitOrder.", StringComparison.Ordinal);

    internal static MermaidPacketSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidPacketSettings();
        var rows = configuration.Last("packet.bitsPerRow");
        if (rows != null) {
            if (!rows.IsString && double.TryParse(rows.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
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
        return settings;
    }
}
