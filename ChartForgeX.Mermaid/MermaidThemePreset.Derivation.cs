using System;
using System.Collections.Generic;
using Precise=ChartForgeX.Rendering.ChartColorMath.PreciseColor;
namespace ChartForgeX.Mermaid;
internal sealed partial class MermaidThemePreset {
    private static void DeriveRoles(string name,Dictionary<string,Precise> c,bool darkMode) {
        Precise Get(string key)=>c[key];void Set(string key,Precise value)=>c[key]=value;
        void Fallback(string key,Precise value){if(!c.ContainsKey(key))c.Add(key,value);}
        void Copy(string target,string source)=>Set(target,Get(source));void Default(string target,string source)=>Fallback(target,Get(source));
        if(name is "base" or "neo" or "neo-dark" or "redux" or "redux-dark" or "redux-color" or "redux-dark-color") {
            var redux=name.StartsWith("redux",StringComparison.Ordinal);var dark=name=="neo-dark"||name=="redux-dark"||name=="redux-dark-color";
            Fallback("primaryTextColor",Literal(darkMode?"#eee":redux?(dark?"#FFFFFF":"#28253D"):"#333"));
            Fallback("secondaryColor",Get("primaryColor").AdjustHsl(hue:-120));Fallback("tertiaryColor",Get("primaryColor").AdjustHsl(hue:180,lightness:5));
            Fallback("primaryBorderColor",Border(Get("primaryColor"),darkMode));Fallback("secondaryBorderColor",Border(Get("secondaryColor"),darkMode));Fallback("tertiaryBorderColor",Border(Get("tertiaryColor"),darkMode));
            Fallback("noteBkgColor",Literal("#fff5ad"));Fallback("noteTextColor",Literal("#333"));Fallback("noteBorderColor",Border(Get("noteBkgColor"),darkMode));
            Fallback("secondaryTextColor",Get("secondaryColor").Invert());Fallback("tertiaryTextColor",Get("tertiaryColor").Invert());Fallback("lineColor",Get("background").Invert());Default("textColor","primaryTextColor");Default("mainBkg","primaryColor");
            // These explicit update assignments precede the common defaults in retained redux-dark-color.
            if(name=="redux-dark-color"){Set("actorBorder",Literal("#FFFFFF"));Set("signalColor",Literal("#FFFFFF"));Set("labelBoxBorderColor",Literal("#BDBCCC"));}
            Default("actorBorder","primaryBorderColor");Default("actorBkg","mainBkg");Default("actorTextColor","primaryTextColor");Default("actorLineColor","actorBorder");Default("labelBoxBkgColor","actorBkg");Default("signalColor","textColor");Default("signalTextColor","textColor");Default("labelBoxBorderColor","actorBorder");Default("labelTextColor","actorTextColor");Default("loopTextColor","actorTextColor");
            Fallback("activationBorderColor",Get("secondaryColor").AdjustHsl(lightness:-10));Default("activationBkgColor","secondaryColor");Fallback("sequenceNumberColor",Get("lineColor").Invert());Default("rectBkgColor","tertiaryColor");return;
        }
        if(name=="dark") {
            Set("secondBkg",Get("mainBkg").AdjustHsl(lightness:16));Copy("lineColor","mainContrastColor");Copy("actorBorder","border1");Copy("actorBkg","mainBkg");Copy("actorTextColor","mainContrastColor");Copy("actorLineColor","actorBorder");Copy("signalColor","mainContrastColor");Copy("signalTextColor","mainContrastColor");Copy("labelBoxBkgColor","actorBkg");Copy("labelBoxBorderColor","actorBorder");Copy("labelTextColor","mainContrastColor");Copy("loopTextColor","mainContrastColor");Copy("noteBorderColor","secondaryBorderColor");Copy("noteBkgColor","secondBkg");Copy("noteTextColor","secondaryTextColor");Copy("activationBorderColor","border1");Copy("activationBkgColor","secondBkg");
        } else if(name=="neutral") {
            Set("secondBkg",Get("contrast").AdjustHsl(lightness:55));Copy("border2","contrast");Set("actorBorder",Get("border1").AdjustHsl(lightness:23));Copy("actorBkg","mainBkg");Copy("actorTextColor","text");Copy("actorLineColor","actorBorder");Copy("signalColor","text");Copy("signalTextColor","text");Copy("labelBoxBkgColor","actorBkg");Copy("labelBoxBorderColor","actorBorder");Copy("labelTextColor","text");Copy("loopTextColor","text");Set("noteBorderColor",Literal("#999"));Set("noteBkgColor",Literal("#666"));Set("noteTextColor",Literal("#fff"));
        } else {
            if(name=="forest")Set("actorBorder",Get("mainBkg").AdjustHsl(lightness:-20));else Copy("actorBorder","border1");
            Copy("actorBkg","mainBkg");Copy("labelBoxBkgColor","actorBkg");Copy("labelTextColor","actorTextColor");Copy("loopTextColor","actorTextColor");Copy("noteBorderColor","border2");Copy("noteTextColor","actorTextColor");Copy("actorLineColor","actorBorder");
            if(name=="default"){Copy("signalColor","textColor");Copy("signalTextColor","textColor");Copy("labelBoxBorderColor","actorBorder");}
        }
        Default("rectBkgColor","tertiaryColor");
    }
}
