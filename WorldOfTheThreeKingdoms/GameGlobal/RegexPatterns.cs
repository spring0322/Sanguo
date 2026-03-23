using System.Text.RegularExpressions;

namespace WorldOfTheThreeKingdoms.GameGlobal
{
    public static partial class RegexPatterns
    {
        [GeneratedRegex(@"^[\u4e00-\u9fa5]$")]
        public static partial Regex ChineseChar();

        [GeneratedRegex(@"<(.|\n)+?>")]
        public static partial Regex HtmlTag();

        [GeneratedRegex(@"\s+")]
        public static partial Regex Whitespace();

        [GeneratedRegex(@"[\u4e00-\u9fa5]+")]
        public static partial Regex ChineseString();

        [GeneratedRegex(@"^(\d+)年(1?\d)月([123]?\d)日$")]
        public static partial Regex DatePattern();

        [GeneratedRegex(@"^(\d+)/(\d+)$")]
        public static partial Regex SlashDatePattern();

        [GeneratedRegex(@"^(\d+).*$")]
        public static partial Regex NumberFirstPattern();

        [GeneratedRegex(@",(\s*[}\]])")]
        public static partial Regex TrailingComma();

        [GeneratedRegex(@":\s*""([^""]*?)""([^"",}\]]*?)""")]
        public static partial Regex UnescapedQuotes();

        [GeneratedRegex(@"(\{|\,)\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*:")]
        public static partial Regex MissingQuotes();

        [GeneratedRegex(@",\s*,")]
        public static partial Regex DuplicateCommas();
    }
}
