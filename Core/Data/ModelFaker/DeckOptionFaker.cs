using Bogus;
using Core.Model;
using Core.Model.Helper;

namespace Core.Data.ModelFaker;

public sealed class DeckOptionFaker : Faker<DeckOption>
{
    public DeckOptionFaker()
    {
        RuleFor(o => o.NewCardsPerDay, f => f.Random.Number(0, 500));
        RuleFor(o => o.ReviewLimitPerDay, f => f.Random.Number(0, 9999));
        RuleFor(o => o.SortOrder, f => f.PickRandom<DeckOptionSortOrder>());
        RuleFor(o => o.InterdayLearningMix, f => f.Random.Bool());
    }
}