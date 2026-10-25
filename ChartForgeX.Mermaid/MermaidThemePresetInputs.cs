using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Precise=ChartForgeX.Rendering.ChartColorMath.PreciseColor;
namespace ChartForgeX.Mermaid;
/// <summary>Validated, detached source inputs. Host mode is resolved once at conversion, not during parsing.</summary>
internal sealed partial class MermaidThemePresetInputs {
    internal static readonly string[] ColorNames={"background","primaryColor","primaryTextColor","primaryBorderColor","secondaryColor","secondaryTextColor","secondaryBorderColor","tertiaryColor","tertiaryTextColor","tertiaryBorderColor","lineColor","textColor","mainBkg","mainContrastColor","contrast","border1","border2","text","actorBkg","actorBorder","actorTextColor","actorLineColor","signalColor","signalTextColor","noteBkgColor","noteBorderColor","noteTextColor","activationBkgColor","activationBorderColor","labelBoxBkgColor","labelBoxBorderColor","labelTextColor","loopTextColor","rectBkgColor","sequenceNumberColor"};
    private readonly Dictionary<string,Precise> _colors=new(StringComparer.Ordinal);
    private ChartColor[]? _backgrounds,_borders;
    private bool _darkMode;
    internal bool StyleShadowDisabled {get;private set;}
    private readonly bool _sequence,_palettesEffective;
    private MermaidThemePresetInputs(bool sequence,string? declaredName){_sequence=sequence;_palettesEffective=declaredName==null||declaredName=="redux-color"||declaredName=="redux-dark-color";}
    internal static bool IsPalettePath(string path)=>path=="themeVariables.bkgColorArray"||path=="themeVariables.borderColorArray";
    internal static bool IsScalarPath(string path)=>path=="themeVariables.darkMode"||IsPalettePath(path)||path.StartsWith("themeVariables.",StringComparison.Ordinal)&&Array.IndexOf(ColorNames,path.Substring(15))>=0;
    internal bool Handles(string path){if(IsPalettePath(path)&&!_palettesEffective)return false;if(!_sequence&&path.StartsWith("themeVariables.",StringComparison.Ordinal)&&Array.IndexOf(new[]{"background","primaryColor","primaryTextColor","primaryBorderColor","secondaryColor","tertiaryColor","lineColor","textColor","darkMode"},path.Substring(15).Split('.')[0])<0)return false;if(!path.StartsWith("themeVariables.",StringComparison.Ordinal))return false;if(IsScalarPath(path))return true;var dot=path.IndexOf('.',15);return dot>=0&&IsScalarPath(path.Substring(0,dot));}
    internal static MermaidThemePresetInputs Read(MermaidSourceConfiguration configuration,bool sequence,Action<MermaidConfigurationSetting,string> error) {
        var result=new MermaidThemePresetInputs(sequence,configuration.Theme);
        var shadow=configuration.Last("themeVariables.dropShadow");result.StyleShadowDisabled=shadow?.IsString==true&&shadow.Value=="none";
        result.ReadShadow(shadow);
        foreach(var name in ColorNames){var setting=configuration.Last("themeVariables."+name);if(setting==null)continue;
            if(setting.IsString&&SvgRasterColor.TryParsePrecise(setting.Value,out var color))result._colors[name]=color;
            else Invalid(setting,"must be a supported native color string");}
        var dark=configuration.Last("themeVariables.darkMode");if(dark!=null){if(!dark.IsString&&bool.TryParse(dark.Value,out var value))result._darkMode=value;else Invalid(dark,"must be a boolean");}
        ReadPalette("bkgColorArray",value=>result._backgrounds=value);ReadPalette("borderColorArray",value=>result._borders=value);return result;
        void ReadPalette(string name,Action<ChartColor[]> assign){var setting=configuration.Last("themeVariables."+name);if(setting==null)return;var items=setting.Palette;
            if(items==null||items.Count>256){Invalid(setting,"must be an array of at most 256 supported color strings");return;}
            var colors=new ChartColor[items.Count];for(var i=0;i<colors.Length;i++){if(!SvgRasterColor.TryParse(items[i],out colors[i])){Invalid(setting,"must contain only supported color strings");return;}}assign(colors);}
        void Invalid(MermaidConfigurationSetting setting,string requirement)=>error(setting,"Mermaid configuration '"+setting.Path+"' "+requirement+".");
    }
    internal bool HasAuthored(string name)=>_colors.ContainsKey(name);
    internal bool TryGetAuthored(string name,out ChartColor value){if(_colors.TryGetValue(name,out var color)){value=color.ToColor();return true;}value=default;return false;}
    internal MermaidThemePreset Resolve(VisualThemeMode mode,bool hostTheme,string? currentName) {
        var name=currentName??(_sequence?(mode==VisualThemeMode.Dark?"redux-dark-color":"redux-color"):null);
        return MermaidThemePreset.Create(name,hostTheme,_colors,_darkMode,_backgrounds,_borders);
    }
    internal static MermaidConfigurationSetting CapturePalette(MermaidConfigurationSetting setting,GeoJsonValue value) {
        if(!value.TryAsArray(out var values))return setting;var strings=new List<string?>();foreach(var item in values)strings.Add(item.IsString?item.AsOptionalString():null);return setting.WithPalette(strings);
    }
}
