using Core.Data;
using Core.Data.Helper;
using Core.Dto.Common;
using Core.Dto.NoteType;
using Core.Model;
using Core.Services;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Core.Repository;

public class NoteTypeRepository(DataContext context) : Repository
{
    // TODO: Constrict the Response Type from the Select
    public async Task<PaginationResult<NoteType>> Get(string creatorId, PaginationRequest<int> request,
        bool isDeleted)
    {
        IQueryable<NoteType> query = context.NoteTypes.AsNoTracking()
            .Where(x => x.CreatorId == creatorId || x.CreatorId == null);
        if (isDeleted) query = query.IgnoreQueryFilters().Where(x => x.IsDeleted);

        return await PaginateAsync(query, request);
    }

    public async Task<NoteType?> Get(string creatorId, int id)
    {
        return await context.NoteTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && (x.CreatorId == creatorId || x.CreatorId == null));
    }


    private async Task<bool> DoesNoteNameExist(string name) =>
        await context.NoteTypes.AnyAsync(x => x.Name == name && x.CreatorId == null);

    public async Task<NoteType?> Create(string creatorId, NoteTypeRequest request)
    {
        if (await DoesNoteNameExist(request.Name)) return null;

        NoteType deck = new()
        {
            CreatorId = creatorId,
            Name = request.Name,
            Templates = request.Templates,
            CssStyle = request.CssStyle,
        };
        await context.NoteTypes.AddAsync(deck);
        await context.SaveChangesAsync();
        return deck;
    }


    public async Task<int> Update(int id, string creatorId, NoteTypeRequest request)
    {
        if (await DoesNoteNameExist(request.Name)) return 0;
        return await context.NoteTypes.Where(x => x.Id == id && x.CreatorId == creatorId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(b => b.Name, request.Name)
                .SetProperty(b => b.Templates, request.Templates)
                .SetProperty(b => b.CssStyle, request.CssStyle)
            );
    }

    public async Task<int> Delete(int id, string creatorId)
    {
        return await context.NoteTypes
            .Where(x => x.CreatorId == creatorId && x.Id == id)
            .SetSoftDeleteAsync(true);
    }

    public async Task<int> Restore(int id, string creatorId)
    {
        return await context.NoteTypes
            .Where(x => x.Id == id && x.CreatorId == creatorId)
            .SetSoftDeleteAsync(false);
    }
}