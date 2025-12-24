using System.ComponentModel.DataAnnotations;

namespace Core.Dto.Card;

public class CreateCardRequest
{
    [Required] public int DeckId {get; set;}
    [Required] public int NoteId { get; set; }
    [Required][StringLength(1000)] public string Front { get; set; }
    [Required][StringLength(1000)] public string Back { get; set; }
}