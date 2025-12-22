using System.ComponentModel.DataAnnotations;
using Core.Model;
using Core.Model.Helper;

namespace Core.Dto.Deck;

public class DeckOptionRequest
{
    [Required] [Range(0, 500)] public int NewCardsPerDay { get; set; }
    [Required] [Range(0, 9999)] public int ReviewLimitPerDay { get; set; }
    [Required] public DeckOptionSortOrder SortOrder { get; set; }
    [Required] public bool InterdayLearningMix { get; set; }

    // Request -> Entity
    public static implicit operator DeckOption(DeckOptionRequest request) => new()
    {
        NewCardsPerDay = request.NewCardsPerDay,
        ReviewLimitPerDay = request.ReviewLimitPerDay,
        SortOrder = request.SortOrder,
        InterdayLearningMix = request.InterdayLearningMix,
    };

    // Entity -> Request (The "Reverse")
    public static implicit operator DeckOptionRequest(DeckOption model) => new()
    {
        NewCardsPerDay = model.NewCardsPerDay,
        ReviewLimitPerDay = model.ReviewLimitPerDay,
        SortOrder = model.SortOrder,
        InterdayLearningMix = model.InterdayLearningMix,
    };
}