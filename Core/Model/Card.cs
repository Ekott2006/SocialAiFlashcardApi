using Core.Model.Helper;
using Core.Model.Interface;

namespace Core.Model;

public class Card : HelperModelEntity, IPagination<int>
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
    
    public DateTime? DueDate { get; set; }
    public int Interval { get; set; }
    public decimal Ease { get; set; }
}