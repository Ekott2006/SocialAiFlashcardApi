using Core.Dto.NoteType;
using Core.Helper;
using Core.Model;
using Core.Model.Helper;
using Core.Repository;

namespace Core.Services;

public class NoteTypeService(INoteTypeRepository repository)
{
    public async Task<int> Update(int id, string creatorId, NoteTypeRequest request)
    {
        return await repository.Update(id, creatorId, await Cleanup(request));
    }

    public async Task<NoteType?> Create(string creatorId, NoteTypeRequest request)
    {
        return await repository.Create(creatorId, await Cleanup(request));
    }

    private static async Task<NoteTypeRequest> Cleanup(NoteTypeRequest request)
    {
        // Parsing all the HTML 
        IEnumerable<Task<(string? frontHtml, string? backHtml)>> tasks = request.Templates.Select(async template =>
            (await HtmlParserHelper.Parse(template.Back), await HtmlParserHelper.Parse(template.Front)));
        (string? frontHtml, string? backHtml)[] results = await Task.WhenAll(tasks);
        request.Templates = results
            .Where(r => r is { frontHtml: not null, backHtml: not null })
            .Select(r => new NoteTypeTemplates { Back = r.backHtml!, Front = r.frontHtml! })
            .ToList();
        request.CssStyle = await CssParserHelper.Parse(request.CssStyle);
        return request;
    }
}