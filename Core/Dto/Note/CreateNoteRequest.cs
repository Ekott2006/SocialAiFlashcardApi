namespace Core.Dto.Note;

public class CreateNoteRequest : NoteRequest
{
    public int DeckId { get; set; }
    public int NoteTypeId { get; set; }
}