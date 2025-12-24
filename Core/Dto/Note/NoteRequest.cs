namespace Core.Dto.Note;

public class NoteRequest
{
    public Dictionary<string, string> Data { get; set; }
    public List<string> Tags { get; set; }
}