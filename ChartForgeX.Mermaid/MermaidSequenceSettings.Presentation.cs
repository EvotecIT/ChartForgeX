using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidSequenceSettings {
    private static readonly string[] Geometry = { "actorMargin", "messageMargin", "noteMargin", "diagramMarginX", "diagramMarginY", "width", "height", "boxMargin", "boxTextMargin", "activationWidth", "wrapPadding", "bottomMarginAdj", "labelBoxWidth", "labelBoxHeight" };
    private readonly Dictionary<string,double> _geometry = new(StringComparer.Ordinal);
    private TextStyleOverride? _participant, _message, _note;
    private bool? _wrapLabels, _mirrorParticipants, _hideUnusedParticipants, _rightAngles, _forceMenus;
    private TextAlignment? _messageAlignment, _noteAlignment;
    private static bool IsPresentationPath(string path) {
        if (!path.StartsWith("sequence.",StringComparison.Ordinal)) return false;
        var name=path.Substring("sequence.".Length);
        if (name=="wrap"||name=="mirrorActors"||name=="hideUnusedParticipants"||name=="messageAlign"||name=="noteAlign"||name=="rightAngles"||name=="forceMenus"||Array.IndexOf(Geometry,name)>=0) return true;
        foreach(var role in new[]{"actor","message","note"})
            if(name==role+"FontFamily"||name==role+"FontSize"||name==role+"FontWeight")return true;
        return false;
    }
    private static bool HandlesPresentation(string path) {
        if(IsPresentationPath(path))return true;
        if(!path.StartsWith("sequence.",StringComparison.Ordinal))return false;
        var dot=path.IndexOf('.',"sequence.".Length);
        return dot>=0&&IsPresentationPath(path.Substring(0,dot));
    }
    private void ReadPresentation(MermaidSourceConfiguration configuration,Action<MermaidConfigurationSetting,string> error) {
        foreach(var name in Geometry) {
            var setting=configuration.Last("sequence."+name);if(setting==null)continue;
            bool positive=name=="width"||name=="height"||name=="activationWidth";
            if(setting.TryGetNumber(out var value)&&!double.IsNaN(value)&&!double.IsInfinity(value)&&(positive?value>0:value>=0))_geometry[name]=value;
            else error(setting,"Mermaid configuration '"+setting.Path+"' must be a finite "+(positive?"positive":"nonnegative")+" logical-unit number.");
        }
        var wrap=configuration.Last("sequence.wrap");
        if(wrap!=null) {
            if(!wrap.IsString&&bool.TryParse(wrap.Value,out var enabled))_wrapLabels=enabled;
            else error(wrap,"Mermaid configuration 'sequence.wrap' must be a boolean.");
        }
        ReadAlignment("messageAlign", value => _messageAlignment = value);
        ReadAlignment("noteAlign", value => _noteAlignment = value);
        void ReadAlignment(string name, Action<TextAlignment> set) {
            var setting=configuration.Last("sequence."+name);if(setting==null)return;
            if(setting.IsString && setting.Value is "left" or "center" or "right") set(setting.Value == "left" ? TextAlignment.Left : setting.Value == "center" ? TextAlignment.Center : TextAlignment.Right);
            else error(setting,"Mermaid configuration '"+setting.Path+"' must be left, center or right.");
        }
        ReadView("forceMenus", value => _forceMenus = value);
        configuration.SequenceForceMenus=_forceMenus;
        ReadView("rightAngles", value => _rightAngles = value);
        ReadView("mirrorActors", value => _mirrorParticipants = value);
        ReadView("hideUnusedParticipants", value => _hideUnusedParticipants = value);
        void ReadView(string name, Action<bool> set) {
            var setting=configuration.Last("sequence."+name);if(setting==null)return;
            if(!setting.IsString&&bool.TryParse(setting.Value,out var value))set(value);
            else error(setting,"Mermaid configuration '"+setting.Path+"' must be a boolean.");
        }
        _participant=MermaidConfigurationTypography.Read(configuration,"sequence.actor",error);
        _message=MermaidConfigurationTypography.Read(configuration,"sequence.message",error);
        _note=MermaidConfigurationTypography.Read(configuration,"sequence.note",error);
        ApplySourceDefaults(configuration);
    }
    private double? GeometryValue(string name)=>_geometry.TryGetValue(name,out var value)?value:(double?)null;
    private void ApplyPresentation(SequenceArtifact sequence,MermaidPresentationOptions? host,bool neo) {
        ChartPadding? padding=null;
        if(GeometryValue("diagramMarginX").HasValue||GeometryValue("diagramMarginY").HasValue)
            padding=new ChartPadding(GeometryValue("diagramMarginX")??sequence.Padding,GeometryValue("diagramMarginY")??sequence.Padding,
                GeometryValue("diagramMarginX")??sequence.Padding,GeometryValue("diagramMarginY")??sequence.Padding);
        var sourceTabHeight=(GeometryValue("labelBoxHeight")??0)+(neo?15:0);
        var layout=new SequenceLayoutPolicy(padding,GeometryValue("width"),GeometryValue("height"),GeometryValue("actorMargin"),
            GeometryValue("messageMargin"),GeometryValue("noteMargin"),GeometryValue("boxMargin"),GeometryValue("boxTextMargin"),
            GeometryValue("activationWidth"),GeometryValue("wrapPadding"),_wrapLabels,_mirrorParticipants,_hideUnusedParticipants,_messageAlignment,_noteAlignment,selfMessageRouting:_rightAngles.HasValue?(_rightAngles.Value?SequenceSelfMessageRouting.Orthogonal:SequenceSelfMessageRouting.Curved):null,
            footerBottomMargin:GeometryValue("bottomMarginAdj"),fragmentTabMinimumWidth:Math.Max(50,GeometryValue("labelBoxWidth")??50),fragmentTabMinimumHeight:sourceTabHeight==0?20:sourceTabHeight,participantBands:neo?new SequenceParticipantBands():null);
        if(layout.HasOverrides)sequence.Layout=layout;
        TextStyleOverride? Merge(TextStyleOverride? source) {
            if(source==null)return null;
            var style=source.Clone();
            if(host?.FontFamily!=null)style.FontFamily=host.FontFamily;
            if(host?.FontSize!=null)style.FontSize=host.FontSize;
            return style;
        }
        var typography=new SequenceTypography(Merge(_participant),Merge(_message),Merge(_note),Merge(_message));
        if(typography.HasOverrides)sequence.Typography=typography;
    }
}
