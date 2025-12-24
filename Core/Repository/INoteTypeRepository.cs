using Core.Dto.Common;
using Core.Dto.NoteType;
using Core.Model;

namespace Core.Repository;

public interface INoteTypeRepository
{
    public Task<NoteType?> Get(string creatorId, int id);

    public Task<PaginationResult<NoteType>> Get(string creatorId, PaginationRequest<int> request,
        bool isDeleted);

    public Task<int> Delete(int id, string creatorId);
    public Task<NoteType?> Create(string creatorId, NoteTypeRequest request);
    public Task<int> Update(int id, string creatorId, NoteTypeRequest request);
}