using System;
using System.Globalization;
using System.Xml;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

/// <summary>Captures finite source values in the existing configuration/native topology owners.</summary>
internal sealed class MermaidUseCaseSettings {
    internal double? Padding { get; private set; }
    private double? _wrappingWidth, _minimumWidth;
    private TextStyleOverride? _actor, _useCase;
    internal bool HasSettings => Padding.HasValue || _wrappingWidth.HasValue || _minimumWidth.HasValue || _actor != null || _useCase != null;
    internal static bool IsScalarPath(string path) => path is "usecase.wrappingWidth" or "usecase.minNodeWidth" or "usecase.diagramPadding"
        or "usecase.actorFontFamily" or "usecase.actorFontSize" or "usecase.actorFontWeight"
        or "usecase.usecaseFontFamily" or "usecase.usecaseFontSize" or "usecase.usecaseFontWeight";
    internal bool Handles(string path) {
        if (IsScalarPath(path)) return true;
        if (!path.StartsWith("usecase.", StringComparison.Ordinal)) return false;
        var dot = path.IndexOf('.', 8); return dot >= 0 && IsScalarPath(path.Substring(0, dot));
    }
    internal static MermaidUseCaseSettings Read(MermaidSourceConfiguration source, Action<MermaidConfigurationSetting, string> error) {
        var result = new MermaidUseCaseSettings();
        result.Padding = Number("diagramPadding", 0, 4096);
        result._wrappingWidth = Number("wrappingWidth", 1, 4096);
        result._minimumWidth = Number("minNodeWidth", 1, 4096);
        result._actor = Font("actor"); result._useCase = Font("usecase");
        return result;
        double? Number(string key, double minimum, double maximum) {
            var setting = source.Last("usecase." + key); if (setting == null) return null;
            if (setting.TryGetNumber(out var value) && value >= minimum && value <= maximum) return value;
            Invalid(setting, "must be a finite number from " + minimum + " through " + maximum); return null;
        }
        TextStyleOverride? Font(string role) {
            var style = new TextStyleOverride();
            var size = source.Last("usecase." + role + "FontSize");
            if (size != null) {
                if (size.TryGetNumber(out var value) && value >= 1 && value <= 256) style.FontSize = value;
                else Invalid(size, "must be a finite numeric font size from 1 through 256");
            }
            var family = source.Last("usecase." + role + "FontFamily");
            if (family != null) {
                var valid = family.IsString && !string.IsNullOrWhiteSpace(family.Value) && family.Value!.Length <= 4096
                    && family.Value.IndexOfAny(new[] { ';', '<', '>', '(', ')', '{', '}', '\\', '\r', '\n' }) < 0;
                if (valid) try { XmlConvert.VerifyXmlChars(family.Value!); } catch (XmlException) { valid = false; }
                if (valid) style.FontFamily = family.Value; else Invalid(family, "must be an XML-safe font stack without CSS declarations or expressions");
            }
            var weight = source.Last("usecase." + role + "FontWeight");
            if (weight != null) {
                var value = weight.Value?.Trim();
                if (weight.IsString && (value is "normal" or "bold" or "bolder" or "lighter"
                    || int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric) && numeric >= 1 && numeric <= 1000)) style.FontWeight = value;
                else Invalid(weight, "must be a CSS font-weight keyword or numeric weight string from 1 through 1000");
            }
            return style.HasOverrides ? style : null;
        }
        void Invalid(MermaidConfigurationSetting setting, string reason) => error(setting, "Mermaid configuration '" + setting.Path + "' " + reason + ".");
    }
    internal void Apply(TopologyChart chart, MermaidFlowchartRenderOptions options) {
        if (_wrappingWidth.HasValue || _minimumWidth.HasValue) {
            chart.DefaultRenderOptions ??= new TopologyRenderOptions();
            chart.DefaultRenderOptions.AllowMultilineNodeLabels = true;
            chart.DefaultRenderOptions.WrapNodeLabels = true;
            chart.DefaultRenderOptions.MaxNodeLabelLines = 32;
            chart.DefaultRenderOptions.MaxNodeSubtitleLines = 32;
        }
        foreach (var node in chart.Nodes) {
            var style = node.Shape == TopologyNodeShape.Actor ? _actor : _useCase;
            if (style != null) {
                var copy = style.Clone();
                if (options.Presentation?.FontFamily != null) copy.FontFamily = options.Presentation.FontFamily;
                if (options.Presentation?.FontSize != null) copy.FontSize = options.Presentation.FontSize;
                node.TextStyle = copy;
            }
            var inset = node.ContentPadding ?? 12;
            if (_minimumWidth.HasValue) node.Width = Math.Max(node.Width, _minimumWidth.Value + inset * 2);
            if (_wrappingWidth.HasValue) node.MaximumTextWidth = Math.Max(_wrappingWidth.Value, _minimumWidth ?? 0);
        }
    }
}
