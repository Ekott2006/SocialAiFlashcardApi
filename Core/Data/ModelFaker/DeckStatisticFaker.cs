using Bogus;
using Core.Model;

namespace Core.Data.ModelFaker;

public sealed class DeckStatisticFaker : Faker<DeckStatistic>
{
    public DeckStatisticFaker()
    {
        RuleFor(s => s.Due, f => f.Random.Number(0, 1000));
        RuleFor(s => s.New, f => f.Random.Number(0, 500));
    }
}