using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Precise=ChartForgeX.Rendering.ChartColorMath.PreciseColor;
namespace ChartForgeX.Mermaid;
/// <summary>A finite native materialization of common and Sequence preset roles; no runtime expression interpreter.</summary>
internal sealed partial class MermaidThemePreset {
    private readonly Dictionary<string,ChartColor> _colors;
    private readonly ChartColor[]? _backgrounds,_borders;
    internal readonly string? Name;
    private MermaidThemePreset(string? name,Dictionary<string,ChartColor> colors,ChartColor[]? backgrounds,ChartColor[]? borders){Name=name;_colors=colors;_backgrounds=backgrounds;_borders=borders;}
    internal static bool IsKnown(string? name)=>name is "base" or "default" or "dark" or "forest" or "neutral" or "neo" or "neo-dark" or "redux" or "redux-dark" or "redux-color" or "redux-dark-color";
    internal static MermaidThemePreset Create(string? name,bool hostTheme,Dictionary<string,Precise> authored,bool darkMode,ChartColor[]? backgrounds,ChartColor[]? borders) {
        var selected=IsKnown(name)&&!hostTheme?name:null;
        var values=selected!=null?Seed(selected):new Dictionary<string,Precise>(StringComparer.Ordinal);
        // The default constructor alone eagerly updates its roles before receiving overrides.
        if(selected=="default")DeriveRoles(selected,values,false);
        foreach(var pair in authored)values[pair.Key]=pair.Value;
        if(selected!=null)DeriveRoles(selected,values,darkMode);
        // Upstream calculate replays authored literals after its derived-role assignments.
        foreach(var pair in authored)values[pair.Key]=pair.Value;
        var colors=new Dictionary<string,ChartColor>(StringComparer.Ordinal);foreach(var pair in values)colors[pair.Key]=pair.Value.ToColor();
        var palette=name=="redux-color"||name=="redux-dark-color";
        return new MermaidThemePreset(name,colors,palette?Clone(backgrounds??DefaultBackgrounds(selected)):null,palette?Clone(borders??DefaultBorders(selected)):null);
    }
    internal bool TryGet(string name,out ChartColor value)=>_colors.TryGetValue(name,out value);
    internal ChartColor? AtBackground(int sourceIndex)=>At(_backgrounds,sourceIndex);
    internal ChartColor? AtBorder(int sourceIndex)=>At(_borders,sourceIndex);
    internal ChartColor? AtActivationBackground(int sourceIndex)=>_backgrounds==null?null:_backgrounds.Length>0?At(_backgrounds,sourceIndex):TryGet("mainBkg",out var value)?value:(ChartColor?)null;
    private static ChartColor? At(ChartColor[]? values,int index)=>values!=null&&values.Length>0?values[index%values.Length]:(ChartColor?)null;
    private static ChartColor[]? Clone(ChartColor[]? values)=>values==null?null:(ChartColor[])values.Clone();
    private static ChartColor[]? DefaultBackgrounds(string? name)=>name=="redux-color"?Palette("#FDF4FF #F0FDFA #FFF7ED #ECFEFF #F0FDF4 #F5F3FF #FEF2F2 #FEFCE8 #EEF2FF #F7FEE7 #F0F9FF #FFF1F2"):name=="redux-dark-color"?Array.Empty<ChartColor>():null;
    private static ChartColor[]? DefaultBorders(string? name)=>name=="redux-color"||name=="redux-dark-color"?Palette("#E879F9 #2DD4BF #FB923C #22D3EE #4ADE80 #A78BFA #F87171 #FACC15 #818CF8 #A3E635 #38BDF8 #FB7185"):null;
    private static ChartColor[] Palette(string value){var parts=value.Split(' ');var result=new ChartColor[parts.Length];for(var i=0;i<parts.Length;i++)result[i]=ChartColor.FromHex(parts[i]);return result;}
    internal VisualTheme Apply(VisualTheme source) {
        if(_colors.Count==0)return source;
        return source.Configure(builder=>{Apply(builder.Light);Apply(builder.Dark);});
    }
    private void Apply(VisualDesignTokens tokens) {
        if(TryGet("background",out var background))tokens.Background=background;
        if(TryGet("primaryColor",out var primary)){tokens.Surface=primary;tokens.Neutral=primary;}
        if(TryGet("secondaryColor",out var secondary)){tokens.ElevatedSurface=secondary;tokens.SecondaryAccent=secondary;}
        if(TryGet("tertiaryColor",out var tertiary))tokens.Neutral2=tertiary;
        if(TryGet("textColor",out var text)){tokens.Foreground=text;tokens.MutedForeground=text;}
        if(TryGet("primaryTextColor",out var primaryText))tokens.Foreground=primaryText;
        if(TryGet("primaryBorderColor",out var border))tokens.Border=border;
        if(TryGet("lineColor",out var line)){tokens.Axis=line;tokens.Grid=line;tokens.Status.Neutral=new VisualTokenColor(line,line);}
    }
}
