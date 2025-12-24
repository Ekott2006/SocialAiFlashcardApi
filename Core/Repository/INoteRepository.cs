using Core.Dto.Common;
using Core.Dto.Note;
using Core.Model;

namespace Core.Repository;

public interface INoteRepository
{
    Task<PaginationResult<Note>> Get(string creatorId, int deckId, PaginationRequest<int> request);
    Task<Note?> GetAdvanced(string creatorId, int id);
    Task<Note?> Create(string creatorId, CreateNoteRequest request);
    Task<int> Delete(int id, string creatorId);
    Task<int> Restore(int id, string creatorId);
    Task<int> Update(int id, string creatorId, UpdateNoteRequest request);
}