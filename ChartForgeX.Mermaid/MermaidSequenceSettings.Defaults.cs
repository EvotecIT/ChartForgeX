using System;
using ChartForgeX.Core;
using ChartForgeX.Typography;
namespace ChartForgeX.Mermaid;
internal sealed partial class MermaidSequenceSettings {
    // Retained12.2 config.schema/defaultConfig values. These are effective source defaults, never fake authored Settings/spans.
    private void ApplySourceDefaults(MermaidSourceConfiguration configuration) {
        void Default(string key,double value){if(!_geometry.ContainsKey(key))_geometry.Add(key,value);}
        Default("activationWidth",10);Default("diagramMarginX",50);Default("diagramMarginY",10);Default("actorMargin",50);Default("width",150);Default("height",65);
        Default("boxMargin",10);Default("boxTextMargin",5);Default("noteMargin",10);Default("messageMargin",35);Default("wrapPadding",10);Default("bottomMarginAdj",1);Default("labelBoxWidth",50);Default("labelBoxHeight",20);
        _wrapLabels??=false;_mirrorParticipants??=true;_hideUnusedParticipants??=false;_rightAngles??=false;_messageAlignment??=TextAlignment.Center;_noteAlignment??=TextAlignment.Center;
        _participant=Role(_participant,14,"\"Open Sans\", sans-serif");_message=Role(_message,16,"\"trebuchet ms\", verdana, arial, sans-serif");_note=Role(_note,14,"\"trebuchet ms\", verdana, arial, sans-serif");
        TextStyleOverride Role(TextStyleOverride? authored,double size,string family){var role=authored?.Clone()??new TextStyleOverride();role.FontSize??=configuration.FontSize??size;role.FontFamily??=configuration.FontFamily??family;role.FontWeight??="400";return role;}
    }
}
