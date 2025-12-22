using Core.Model.Helper;
using Microsoft.EntityFrameworkCore;

namespace Core.Model;

[Owned]
public class DeckOption
{
    public int NewCardsPerDay { get; set; }
    public int ReviewLimitPerDay { get; set; }
    public DeckOptionSortOrder SortOrder { get; set; }
    public bool InterdayLearningMix { get; set; }
}