using ExCSS;

namespace Core.Helper;

public class CssParserHelper
{
    public static async Task<string> Parse(string css)
    {
        StylesheetParser parser = new();
        Stylesheet? stylesheet = await parser.ParseAsync(css);
        string? cssString = stylesheet.ToCss();
        return cssString ?? "";
    }
}