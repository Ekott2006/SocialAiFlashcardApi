using Bogus;
using Core.Model;

namespace Core.Data.ModelFaker;

public sealed class NoteTypeFaker : Faker<NoteType>
{
    public NoteTypeFaker(string? creatorId, bool isDeleted, string? name = null)
    {
        RuleFor(x => x.Name, f => name ?? f.Commerce.ProductName());
        RuleFor(x => x.CreatorId, creatorId);
        RuleFor(x => x.Templates, _ => []);
        RuleFor(x => x.CssStyle, f => f.Lorem.Sentence());
        RuleFor(x => x.CreatedAt, f => f.Date.Past());
        RuleFor(x => x.CreatedAt, f => f.Date.Recent());
        RuleFor(x => x.IsDeleted, isDeleted);
    }
}