using System;
using System.Text.RegularExpressions;
using MLEM.Font;

namespace MLEM.Formatting.Codes {
    /// <inheritdoc />
    public class FontCode : Code {

        private readonly Func<GenericFont, GenericFont> font;

        /// <inheritdoc />
        public FontCode(Match match, Regex regex, Func<GenericFont, GenericFont> font) : base(match, regex) {
            this.font = font;
        }

        /// <inheritdoc />
        public override GenericFont GetFont(GenericFont defaultPick) {
            return this.font?.Invoke(defaultPick);
        }

    }
}
