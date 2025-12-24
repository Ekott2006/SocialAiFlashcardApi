using Core.Data;
using Core.Data.Helper;
using Core.Dto.Common;
using Core.Dto.Note;
using Core.Model;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Core.Repository;

public class NoteRepository(DataContext context) : Repository
{
    public async Task<PaginationResult<Note>> Get(string creatorId, int deckId, PaginationRequest<int> request,
        bool isDeleted)
    {
        IQueryable<Note> query = context.Notes.AsNoTracking()
            .Where(x => x.CreatorId == creatorId && x.DeckId == deckId);
        if (isDeleted) query = query.IgnoreQueryFilters().Where(x => x.IsDeleted);

        return await PaginateAsync(query, request);
    }

    public async Task<Note?> GetAdvanced(string creatorId, int id)
    {
        return await context.Notes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatorId == creatorId);
    }

    private async Task<bool> DoesDeckAndNoteTypeExists(string creatorId, int deckId, int noteTypeId)
    {
        bool doesDeckExists = await context.Decks.AnyAsync(x => x.CreatorId == creatorId && x.Id == deckId);
        bool doesNoteTypeExists = await context.Notes.AnyAsync(x =>
            x.Id == noteTypeId && x.CreatorId == creatorId);
        return doesNoteTypeExists && doesDeckExists;
    }

    public async Task<Note?> Create(string creatorId, CreateNoteRequest request)
    {
        if (!(await DoesDeckAndNoteTypeExists(creatorId, request.DeckId, request.NoteTypeId))) return null;

        Note note = new()
        {
            DeckId = request.DeckId,
            NoteTypeId = request.NoteTypeId,
            CreatorId = creatorId,
            Data = request.Data,
            Tags = request.Tags,
        };
        await context.Notes.AddAsync(note);
        await context.SaveChangesAsync();
        return note;
    }

    public async Task<int> Update(int id, string creatorId, UpdateNoteRequest request)
    {
        return await context.Notes.Where(x => x.Id == id && x.CreatorId == creatorId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(d => d.Data, request.Data)
                .SetProperty(d => d.Tags, request.Tags)
            );
    }


    public async Task<int> Delete(int id, string creatorId)
    {
        return await context.Notes
            .Where(x => x.CreatorId == creatorId && x.Id == id)
            .SetSoftDeleteAsync(true);
    }

    public async Task<int> Restore(int id, string creatorId)
    {
        return await context.Notes
            .Where(x => x.Id == id && x.CreatorId == creatorId)
            .SetSoftDeleteAsync(false);
    }
}