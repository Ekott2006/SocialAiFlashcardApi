using Core.Data;
using Core.Dto.Card;
using Core.Dto.Common;
using Core.Model;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Core.Repository;

public class CardRepository(DataContext context) : Repository
{
    public async Task<PaginationResult<Card>> Get(string creatorId, int deckId, PaginationRequest<int> request)
    {
        IQueryable<Card> query = context.Cards.AsNoTracking()
            .Where(x => x.DeckId == deckId && x.CreatorId == creatorId);

        return await PaginateAsync(query, request);
    }


    // TODO: Add Default Value
    public async Task<Card?> Create(string creatorId, CreateCardRequest request)
    {
        bool doesNoteExists = await context.Notes.AnyAsync(x => x.CreatorId == creatorId && x.Id == request.NoteId);
        if (!doesNoteExists) return null;
        Card card = new()
        {
            DeckId = request.DeckId,
            NoteId = request.NoteId,
            CreatorId = creatorId,
            Front = request.Front,
            Back = request.Back,
            IsSuspended = false,

            // TODO: Add the SRS Info
        };
        await context.Cards.AddAsync(card);
        await context.SaveChangesAsync();
        return card;
    }

    // Service Update and User Update
    public async Task Update(int id, string creatorId, UpdateUserCardRequest request)
    {
        IQueryable<Card> query = context.Cards.Where(x => x.Id == id && x.CreatorId == creatorId);
        if (request.TypeRequest == UpdateUserCardTypeRequest.Suspend)
        {
            await query.ExecuteUpdateAsync(x => x
                .SetProperty(d => d.IsSuspended, true));
        }
        else
        {
            // query.ExecuteUpdateAsync(x => x.SetProperty(x => x.DueDate, ).SetProperty(x => x.Ease, ).SetProperty(x => x.Interval))
        }
    }
}