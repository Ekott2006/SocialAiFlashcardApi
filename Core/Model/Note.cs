using System.ComponentModel.DataAnnotations;
using Core.Model.Helper;

namespace Core.Model;

public class Note: DateTimeModel, ISoftDeletable
{
    public int Id { get; set; }
    public int DeckId { get; set; }
    public Deck Deck { get; set; }
    public int NoteTypeId { get; set; }
    public NoteType NoteType { get; set; }
    public string[] Fields { get; set; }
    public string[] Tags { get; set; }
    public bool IsDeleted { get; set; }
}
public class Card : DateTimeModel
{
    public int Id { get; set; }
    public int DeckId {get; set;}
    public Deck Deck { get; set; }
    public int NoteId { get; set; }
    public Note Note { get; set; }
    public string CreatorId { get; set; }
    public User Creator { get; set; }
    
    public string Front { get; set; }
    public string Back { get; set; }
    public bool IsSuspended { get; set; }
    
    public DateTime DueDate { get; set; }
    public int Interval { get; set; }
    public decimal Ease { get; set; }
}


public class NoteTypeRequest
{
    [Required] public int DeckId { get; set; }
    [Required] public int NoteTypeId { get; set; }
    [Required] public string[] Fields { get; set; }
    [MaxLength(5)] public string[] Tags { get; set; } = [];
}