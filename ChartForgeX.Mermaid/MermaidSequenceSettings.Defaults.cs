using System;
using ChartForgeX.Core;
using ChartForgeX.Typography;
namespace ChartForgeX.Mermaid;
internal sealed partial class MermaidSequenceSettings {
    // Retained geometry defaults are effective values, never fake authored Settings/spans.
    // Typography and outer padding inherit the native host; authored source roles still win.
    private void ApplySourceDefaults(MermaidSourceConfiguration configuration) {
        void Default(string key,double value){if(!_geometry.ContainsKey(key))_geometry.Add(key,value);}
        Default("activationWidth",10);Default("actorMargin",50);Default("width",150);Default("height",65);
        Default("boxMargin",10);Default("boxTextMargin",5);Default("noteMargin",10);Default("messageMargin",35);Default("wrapPadding",10);Default("bottomMarginAdj",1);Default("labelBoxWidth",50);Default("labelBoxHeight",20);
        _wrapLabels??=true;_mirrorParticipants??=true;_hideUnusedParticipants??=false;_rightAngles??=false;_messageAlignment??=TextAlignment.Center;_noteAlignment??=TextAlignment.Center;
        _participant=Role(_participant);_message=Role(_message);_note=Role(_note);
        TextStyleOverride? Role(TextStyleOverride? authored) {
            if (authored == null && configuration.FontFamily == null && !configuration.FontSize.HasValue) return null;
            var role = authored?.Clone() ?? new TextStyleOverride();
            role.FontSize ??= configuration.FontSize;
            role.FontFamily ??= configuration.FontFamily;
            return role;
        }
    }
}
