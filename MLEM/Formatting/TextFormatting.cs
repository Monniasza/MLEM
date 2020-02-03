using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extensions;
using MLEM.Font;
using MLEM.Misc;
using MLEM.Textures;

namespace MLEM.Formatting {
    public static class TextFormatting {

        public static readonly Dictionary<string, FormattingCode> FormattingCodes = new Dictionary<string, FormattingCode>();
        private static readonly Dictionary<IGenericFont, string> OneEmStrings = new Dictionary<IGenericFont, string>();
        private static Regex formatRegex;

        static TextFormatting() {
            SetFormatIndicators('[', ']');

            // style codes
            FormattingCodes["regular"] = new FormattingCode(TextStyle.Regular);
            FormattingCodes["italic"] = new FormattingCode(TextStyle.Italic);
            FormattingCodes["bold"] = new FormattingCode(TextStyle.Bold);
            FormattingCodes["shadow"] = new FormattingCode(TextStyle.Shadow);

            // color codes
            var colors = typeof(Color).GetProperties();
            foreach (var color in colors) {
                if (color.GetGetMethod().IsStatic)
                    FormattingCodes[color.Name.ToLowerInvariant()] = new FormattingCode((Color) color.GetValue(null));
            }

            // animations
            FormattingCodes["unanimated"] = new FormattingCode(TextAnimation.Default);
            FormattingCodes["wobbly"] = new FormattingCode(TextAnimation.Wobbly);
            FormattingCodes["typing"] = new FormattingCode(TextAnimation.Typing);
        }

        public static void SetFormatIndicators(char opener, char closer) {
            // escape the opener and closer so that any character can be used
            var op = "\\" + opener;
            var cl = "\\" + closer;
            // find any text that is surrounded by the opener and closer
            formatRegex = new Regex($"{op}[^{op}{cl}]*{cl}");
        }

        public static string GetOneEmString(IGenericFont font) {
            if (!OneEmStrings.TryGetValue(font, out var strg)) {
                strg = " ";
                while (font.MeasureString(strg + ' ').X < font.LineHeight)
                    strg += ' ';
                OneEmStrings[font] = strg;
            }
            return strg;
        }

        public static string RemoveFormatting(this string s, IGenericFont font) {
            return formatRegex.Replace(s, match => {
                var code = FromMatch(match);
                return code != null ? code.GetReplacementString(font) : string.Empty;
            });
        }

        public static Dictionary<int, FormattingCode> GetFormattingCodes(this string s, IGenericFont font) {
            var codes = new Dictionary<int, FormattingCode>();
            var codeLengths = 0;
            foreach (Match match in formatRegex.Matches(s)) {
                var code = FromMatch(match);
                if (code == null)
                    continue;
                codes[match.Index - codeLengths] = code;
                codeLengths += match.Length - code.GetReplacementString(font).Length;
            }
            return codes;
        }

        public static void DrawFormattedString(this IGenericFont regularFont, SpriteBatch batch, Vector2 pos, string text, Dictionary<int, FormattingCode> codeLocations, Color color, float scale, IGenericFont boldFont = null, IGenericFont italicFont = null, float depth = 0, TimeSpan timeIntoAnimation = default, FormatSettings formatSettings = null) {
            var settings = formatSettings ?? FormatSettings.Default;
            var currColor = color;
            var currFont = regularFont;
            var currStyle = TextStyle.Regular;
            var currAnim = TextAnimation.Default;
            var animStart = 0;

            var innerOffset = new Vector2();
            for (var i = 0; i < text.Length; i++) {
                // check if the current character's index has a formatting code
                codeLocations.TryGetValue(i, out var code);
                if (code != null) {
                    // if so, apply it
                    switch (code.CodeType) {
                        case FormattingCode.Type.Color:
                            currColor = code.Color.CopyAlpha(color);
                            break;
                        case FormattingCode.Type.Style:
                            switch (code.Style) {
                                case TextStyle.Regular:
                                    currFont = regularFont;
                                    break;
                                case TextStyle.Bold:
                                    currFont = boldFont ?? regularFont;
                                    break;
                                case TextStyle.Italic:
                                    currFont = italicFont ?? regularFont;
                                    break;
                            }
                            currStyle = code.Style;
                            break;
                        case FormattingCode.Type.Icon:
                            code.Icon.SetTime(timeIntoAnimation.TotalSeconds * code.Icon.SpeedMultiplier % code.Icon.TotalTime);
                            batch.Draw(code.Icon.CurrentRegion, new RectangleF(pos + innerOffset, new Vector2(regularFont.LineHeight * scale)), color, 0, Vector2.Zero, SpriteEffects.None, depth);
                            break;
                        case FormattingCode.Type.Animation:
                            currAnim = code.Animation;
                            animStart = i;
                            break;
                    }
                }

                var c = text[i];
                var cSt = c.ToString();
                if (c == '\n') {
                    innerOffset.X = 0;
                    innerOffset.Y += regularFont.LineHeight * scale;
                } else {
                    if (currStyle == TextStyle.Shadow)
                        currAnim(settings, currFont, batch, text, i, animStart, cSt, pos + innerOffset + settings.DropShadowOffset * scale, settings.DropShadowColor, scale, depth, timeIntoAnimation);
                    currAnim(settings, currFont, batch, text, i, animStart, cSt, pos + innerOffset, currColor, scale, depth, timeIntoAnimation);
                    innerOffset.X += regularFont.MeasureString(cSt).X * scale;
                }
            }
        }

        private static FormattingCode FromMatch(Capture match) {
            var rawCode = match.Value.Substring(1, match.Value.Length - 2).ToLowerInvariant();
            FormattingCodes.TryGetValue(rawCode, out var val);
            return val;
        }

    }
}