using Core.Model.Helper;
using Core.Model.Interface;

namespace Core.Model;

public class Note:  HelperModelEntity, IPagination<int>
{
    public int Id { get; set; }
    public int DeckId { get; set; }
    public Deck Deck { get; set; }
    public int NoteTypeId { get; set; }
    public NoteType NoteType { get; set; }
    public string CreatorId { get; set; }
    public User Creator { get; set; }
    public Dictionary<string, string> Data { get; set; }
    public ICollection<string> Tags { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<Card> Cards { get; set; }
}