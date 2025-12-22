using Bogus;
using Core.Model;

namespace Core.Data.ModelFaker;

public sealed class DeckFaker : Faker<Deck>
{
    public DeckFaker(string creatorId, bool isDeleted = false)
    {
        RuleFor(x => x.Name, f => f.Lorem.Sentence());
        RuleFor(x => x.CreatorId, _ => creatorId);
        RuleFor(x => x.Description, f => f.Lorem.Sentences());
        RuleFor(x => x.IsPublic, f => f.Random.Bool());
        RuleFor(x => x.IsDeleted, _ => isDeleted);
        RuleFor(x => x.IsUserOption, f => f.Random.Bool());
        RuleFor(x => x.Statistic, _ => new DeckStatisticFaker());
        RuleFor(x => x.Option, _ => new DeckOptionFaker());
    }
}