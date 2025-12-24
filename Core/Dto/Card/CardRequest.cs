using System.ComponentModel.DataAnnotations;

namespace Core.Dto.Card;

public class CardRequest
{
    [Required] public int DeckId { get; set; }
    [Required] public int NoteTypeId { get; set; }
    [Required] public string[] Fields { get; set; }
    [MaxLength(5)] public string[] Tags { get; set; } = [];
}