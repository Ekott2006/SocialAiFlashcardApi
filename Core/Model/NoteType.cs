using Core.Model.Helper;
using Microsoft.EntityFrameworkCore;

namespace Core.Model;

[Index(nameof(Name), nameof(Id), IsUnique = true)]
public class NoteType: DateTimeModel, ISoftDeletable
{
    public int Id { get; set; }
    public string? CreatorId { get; set; }
    public User? Creator { get; set; }
    public string Name { get; set; }
    public string[] Fields { get; set; }
    public NoteTypeTemplates[] Templates { get; set; }
    public string CssStyle { get; set; }

    public bool IsDeleted { get; set; }
}
[Owned]
public class NoteTypeTemplates
{
    public string Front { get; set; }
    public string Back { get; set; }
}