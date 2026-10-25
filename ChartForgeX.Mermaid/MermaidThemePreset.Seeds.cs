using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Precise=ChartForgeX.Rendering.ChartColorMath.PreciseColor;
namespace ChartForgeX.Mermaid;
internal sealed partial class MermaidThemePreset {
    private static Dictionary<string,Precise> Seed(string name) {
        var colors=new Dictionary<string,Precise>(StringComparer.Ordinal);
        void Set(string key,string value){if(!SvgRasterColor.TryParsePrecise(value,out var color))throw new InvalidOperationException("Invalid maintained preset literal.");colors[key]=color;}
        switch(name) {
            case "base":
                Set("background","#f4f4f4");
                Set("primaryColor","#fff4dd");
                Set("noteBkgColor","#fff5ad");
                Set("noteTextColor","#333");
                break;
            case "dark":
                Set("background","#333");
                Set("primaryColor","#1f2020");
                Set("mainBkg","#1f2020");
                Set("mainContrastColor","lightgrey");
                Set("border1","#ccc");
                Set("textColor","#ccc");
                Set("noteBkgColor","#fff5ad");
                Set("sequenceNumberColor","black");
                break;
            case "default":
                Set("background","white");
                Set("primaryColor","#ECECFF");
                Set("secondaryColor","#ffffde");
                Set("mainBkg","#ECECFF");
                Set("secondBkg","#ffffde");
                Set("lineColor","#333333");
                Set("border1","#9370DB");
                Set("border2","#aaaa33");
                Set("textColor","#333");
                Set("actorTextColor","black");
                Set("noteBkgColor","#fff5ad");
                Set("activationBorderColor","#666");
                Set("activationBkgColor","#f4f4f4");
                Set("sequenceNumberColor","white");
                break;
            case "forest":
                Set("background","white");
                Set("primaryColor","#cde498");
                Set("secondaryColor","#cdffb2");
                Set("mainBkg","#cde498");
                Set("secondBkg","#cdffb2");
                Set("lineColor","green");
                Set("border1","#13540c");
                Set("border2","#6eaa49");
                Set("actorTextColor","black");
                Set("signalColor","#333");
                Set("signalTextColor","#333");
                Set("labelBoxBorderColor","#326932");
                Set("noteBkgColor","#fff5ad");
                Set("activationBorderColor","#666");
                Set("activationBkgColor","#f4f4f4");
                Set("sequenceNumberColor","white");
                break;
            case "neo-dark":
                Set("background","#333");
                Set("primaryColor","#1f2020");
                Set("mainBkg","#2a2020");
                Set("mainContrastColor","lightgrey");
                Set("border1","#ccc");
                Set("textColor","#ccc");
                Set("noteBkgColor","#fff5ad");
                Set("noteTextColor","#333");
                break;
            case "neo":
                Set("background","#ffffff");
                Set("primaryColor","#cccccc");
                Set("mainBkg","#ffffff");
                Set("noteBkgColor","#fff5ad");
                Set("noteTextColor","#333");
                Set("tertiaryColor","#ffffff");
                break;
            case "neutral":
                Set("primaryColor","#eee");
                Set("contrast","#707070");
                Set("background","#ffffff");
                Set("mainBkg","#eee");
                Set("lineColor","#666");
                Set("border1","#999");
                Set("text","#333");
                Set("activationBorderColor","#666");
                Set("activationBkgColor","#f4f4f4");
                Set("sequenceNumberColor","white");
                break;
            case "redux-color":
                Set("background","#ffffff");
                Set("primaryColor","#cccccc");
                Set("mainBkg","#ffffff");
                Set("noteBkgColor","#fff5ad");
                Set("noteTextColor","#28253D");
                Set("tertiaryColor","#ffffff");
                Set("actorBorder","#28253D");
                Set("noteBorderColor","#FACC15");
                break;
            case "redux-dark-color":
                Set("background","#333");
                Set("primaryColor","#1f2020");
                Set("mainBkg","#111113");
                Set("mainContrastColor","lightgrey");
                Set("border1","#ccc");
                Set("textColor","#ccc");
                Set("noteBorderColor","#FACC15");
                break;
            case "redux-dark":
                Set("background","#333");
                Set("primaryColor","#1f2020");
                Set("mainBkg","#111113");
                Set("mainContrastColor","lightgrey");
                Set("border1","#ccc");
                Set("textColor","#ccc");
                Set("noteBorderColor","#FACC15");
                break;
            case "redux":
                Set("background","#ffffff");
                Set("primaryColor","#cccccc");
                Set("mainBkg","#ffffff");
                Set("noteBkgColor","#fff5ad");
                Set("noteTextColor","#28253D");
                Set("tertiaryColor","#ffffff");
                Set("noteBorderColor","#FACC15");
                Set("actorBorder","#28253D");
                break;
            default:throw new ArgumentOutOfRangeException(nameof(name));
        }
        // The retained dark redux constructors initialize these with ?? before updateColors fallbacks.
        if(name=="redux-dark"||name=="redux-dark-color"){Set("noteBkgColor","#FEF9C3");Set("noteTextColor","#28253D");}
        Precise Get(string key)=>colors[key];
        void Derive(string key,Precise value){if(!colors.ContainsKey(key))colors.Add(key,value);}
        bool legacy=name is "default" or "forest" or "neutral" or "dark";
        bool dark=name=="dark"||name.EndsWith("-dark",StringComparison.Ordinal)||name.EndsWith("-dark-color",StringComparison.Ordinal);
        // These constructors create their base derived values before user overrides. Keep them as initialized values.
        if(name=="default"||name=="neutral") {
            Derive("secondaryColor",name=="default"?Literal("#ffffde"):Get("contrast").AdjustHsl(lightness:55));
            Derive("tertiaryColor",Get("primaryColor").AdjustHsl(hue:-160));
        } else if(name=="forest")Derive("tertiaryColor",Get("primaryColor").AdjustHsl(lightness:10));
        else if(dark){Derive("secondaryColor",Get("primaryColor").AdjustHsl(lightness:16));Derive("tertiaryColor",Get("primaryColor").AdjustHsl(hue:-160));}
        if(legacy||dark) {
            Derive("primaryBorderColor",dark?Get("background").Invert():Border(Get("primaryColor"),false));
            Derive("secondaryBorderColor",Border(Get("secondaryColor"),false));Derive("tertiaryBorderColor",Border(Get("tertiaryColor"),false));
            Derive("primaryTextColor",Get("primaryColor").Invert());Derive("secondaryTextColor",Get("secondaryColor").Invert());Derive("tertiaryTextColor",(name=="forest"?Get("primaryColor"):Get("tertiaryColor")).Invert());
            Derive("lineColor",Get("background").Invert());Derive("textColor",Get("background").Invert());
        } else if(name!="base")Derive("primaryBorderColor",Border(name.StartsWith("redux",StringComparison.Ordinal)?Literal("#28253D"):Get("primaryColor"),false));
        if(name=="forest")colors["lineColor"]=Get("background").Invert();
        // The dark legacy constructor marks its line color calculated after its initial inversion.
        if(name=="dark")colors.Remove("lineColor");
        return colors;
    }
    private static Precise Literal(string value)=>SvgRasterColor.TryParsePrecise(value,out var color)?color:throw new InvalidOperationException("Invalid maintained preset literal.");
    private static Precise Border(Precise color,bool dark)=>color.AdjustHsl(saturation:-40,lightness:dark?10:-10);
}
