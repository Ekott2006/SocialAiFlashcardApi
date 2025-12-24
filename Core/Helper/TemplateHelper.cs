using System.Text.RegularExpressions;
using Scriban;
using static System.Text.RegularExpressions.Regex;

namespace Core.Helper;

public partial class TemplateHelper
{
    public static async Task<string> Parse(string text, Dictionary<string, string> data)
    {
        Template? template = Template.Parse(text);
        string? result = await template.RenderAsync(data);
        return result ?? "";
    }
    public static List<string> GetAllFields(IEnumerable<string> templates)
    {
        return templates
            .SelectMany(t => MyRegex().Matches(t))
            .Select(m => m.Groups[1].Value.Trim())
            .Distinct()
            .ToList();
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\{\{(.*?)\}\}")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}