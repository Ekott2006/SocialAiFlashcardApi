using Microsoft.EntityFrameworkCore;

namespace Core.Model.Helper.Deck;

[Owned]
public record DeckOption
{
    public int NewCardsPerDay { get; set; }
    public int ReviewLimitPerDay { get; set; }
    public DeckOptionSortOrder SortOrder { get; set; }
    public bool InterdayLearningMix { get; set; }
}