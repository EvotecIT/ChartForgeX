using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Finite source presentation mapped to native sequence facts; aliases resolve to literal Unicode, never assets.</summary>
internal static class MermaidZenUmlPresentation {
    private static readonly Dictionary<string,string> Symbols=new(StringComparer.Ordinal) {
        ["check"] = "✅",
        ["x"] = "❌",
        ["warning"] = "⚠️",
        ["exclamation"] = "❗",
        ["question"] = "❓",
        ["bulb"] = "💡",
        ["eyes"] = "👀",
        ["rocket"] = "🚀",
        ["fire"] = "🔥",
        ["zap"] = "⚡",
        ["boom"] = "💥",
        ["sparkles"] = "✨",
        ["tada"] = "🎉",
        ["confetti_ball"] = "🎊",
        ["lock"] = "🔒",
        ["unlock"] = "🔓",
        ["key"] = "🔑",
        ["gear"] = "⚙️",
        ["hammer"] = "🔨",
        ["wrench"] = "🔧",
        ["package"] = "📦",
        ["email"] = "📧",
        ["link"] = "🔗",
        ["clipboard"] = "📋",
        ["bookmark"] = "🔖",
        ["speech_balloon"] = "💬",
        ["thought_balloon"] = "💭",
        ["bell"] = "🔔",
        ["megaphone"] = "📣",
        ["cloud"] = "☁️",
        ["sun"] = "☀️",
        ["star"] = "⭐",
        ["globe"] = "🌐",
        ["arrow_right"] = "➡️",
        ["arrow_left"] = "⬅️",
        ["arrow_up"] = "⬆️",
        ["arrow_down"] = "⬇️",
        ["wave"] = "👋",
        ["thumbsup"] = "👍",
        ["thumbsdown"] = "👎",
        ["computer"] = "💻",
        ["iphone"] = "📱",
        ["robot"] = "🤖",
        ["bug"] = "🐛",
        ["database"] = "🗄️",
        ["server"] = "🖥️",
        ["api"] = "🔌",
        ["gateway"] = "🚪",
        ["queue"] = "📬",
        ["cache"] = "⚡",
        ["service"] = "⚙️",
        ["processor"] = "🔄",
        ["store"] = "🏪",
        ["worker"] = "👷",
        ["container"] = "📦",
        ["network"] = "🌐",
        ["chart"] = "📊",
        ["chart_with_upwards_trend"] = "📈",
        ["chart_with_downwards_trend"] = "📉",
        ["inbox_tray"] = "📥",
        ["outbox_tray"] = "📤",
        ["file_folder"] = "📁",
        ["hourglass"] = "⏳",
        ["clock"] = "🕐",
        ["stopwatch"] = "⏱️",
        ["heavy_check_mark"] = "✔️",
        ["heavy_multiplication_x"] = "✖️",
        ["red"] = "🔴",
        ["red_circle"] = "🔴",
        ["green_circle"] = "🟢",
        ["blue_circle"] = "🔵",
        ["white_circle"] = "⚪",
        ["black_circle"] = "⚫",
        ["heart"] = "❤️",
        ["shield"] = "🛡️",
        ["coffee"] = "☕",
        ["pizza"] = "🍕",
        ["car"] = "🚗",
        ["bus"] = "🚌",
        ["airplane"] = "✈️",
        ["ship"] = "🚢",
        ["floppy_disk"] = "💾",
        ["cd"] = "💿",
        ["satellite"] = "🛰️",
        ["factory"] = "🏭",
        ["hospital"] = "🏥",
        ["bank"] = "🏦",
        ["construction"] = "🚧",
        ["recycle"] = "♻️",
        ["receipt"] = "🧾",
        ["cart"] = "🛒",
        ["cylinder"] = "🪨",
        ["dollar"] = "💵",
    };
    internal static string Symbol(string source,out bool known) {
        var key=source.Length>1&&source[0]=='['&&source[source.Length-1]==']'?source.Substring(1,source.Length-2):source;
        if(Symbols.TryGetValue(key,out var symbol)){known=true;return symbol;}
        known=source.Length==0||source[0]!='[';return known?source:key;
    }
    internal static void Apply(MermaidZenUmlParticipant source,SequenceArtifactParticipant target) {
        if(source.Attributes.TryGetValue("stereotype",out var stereotype))target.Stereotype=stereotype;
        if(source.Attributes.TryGetValue("emoji",out var emoji))target.Symbol=Symbol(emoji,out _);
        if(source.Attributes.TryGetValue("color",out var color)) {
            if(!ChartColor.TryFromHex(color,out var fill))throw MermaidZenUmlParser.Syntax("ZenUML participant color must be a literal hex fill.",source.Span);
            target.Fill=fill;
        }
        if(source.Attributes.TryGetValue("width",out var width)) {
            if(!int.TryParse(width,NumberStyles.None,CultureInfo.InvariantCulture,out var value)||value<=0)throw MermaidZenUmlParser.Syntax("ZenUML participant minimum width must be a positive integer.",source.Span);
            target.MinimumWidth=value;
        }
    }
}
