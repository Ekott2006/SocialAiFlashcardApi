using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Core.Helper;

public class HtmlParserHelper
{
    public static async Task<string?> Parse(string html)
    {
        HtmlParser htmlParser = new();
        IHtmlDocument document = await htmlParser.ParseDocumentAsync(html);
        return document.Body?.ToHtml();
    }
}