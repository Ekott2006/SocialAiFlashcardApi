using Core.Helper;
using Core.Model.Helper;
using Core.Model.Interface;
using Microsoft.EntityFrameworkCore;

namespace Core.Model;

[Index(nameof(Name), nameof(Id), IsUnique = true)]
public class NoteType:  HelperModelEntity, IPagination<int>
{
    public int Id { get; set; }
    public string? CreatorId { get; set; }
    public User? Creator { get; set; }
    public string Name { get; set; }
    // TODO: Check if it needs caching
    public List<string> Fields => TemplateHelper.GetAllFields(this.Templates.Select(x => $"{x.Back}{x.Front}"));
    public List<NoteTypeTemplates> Templates { get; set; }
    public string CssStyle { get; set; }

    public bool IsDeleted { get; set; }
}