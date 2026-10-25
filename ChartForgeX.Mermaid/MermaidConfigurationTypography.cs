using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

/// <summary>The existing finite XML-safe native font setting parser shared by source diagram families.</summary>
internal static partial class MermaidConfigurationTypography {
    internal static TextStyleOverride? Read(MermaidSourceConfiguration configuration, string prefix, Action<MermaidConfigurationSetting,string> error) {
        var style = new TextStyleOverride();
        var family = configuration.Last(prefix + "FontFamily");
        if (family != null) {
            if (family.IsString && TryFamily(family.Value, out var familyValue)) style.FontFamily = familyValue;
            else Invalid(family, "must be a non-empty XML-safe font stack without CSS declarations");
        }
        var size = configuration.Last(prefix + "FontSize");
        if (size != null) {
            if (TrySize(size.Value, out var number)) style.FontSize = number;
            else Invalid(size, "must be a finite logical-pixel size greater than zero and at most 512 (a number or px string)");
        }
        var weight = configuration.Last(prefix + "FontWeight");
        if (weight != null) {
            if (TryWeight(weight.Value, out var value)) style.FontWeight = value;
            else Invalid(weight, "must be normal, bold, bolder, lighter or an integer from 1 through 1000");
        }
        return style.HasOverrides ? style : null;
        void Invalid(MermaidConfigurationSetting setting,string reason) => error(setting,"Mermaid configuration '"+setting.Path+"' "+reason+".");
    }
}
