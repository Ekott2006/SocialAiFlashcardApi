using System.ComponentModel.DataAnnotations;
using Core.Data;
using Core.Dto.Common;
using Core.Model;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Core.Repository;

public class CardRepository(DataContext context)
{
    public async Task<PaginationResult<Card>> Get(int deckId, PaginationRequest request)
    {
        IQueryable<Card> query = context.Cards.AsNoTracking().Where(x => x.DeckId == deckId);

        Card? reference = request.CursorId != null
            ? await query.FirstOrDefaultAsync(x => x.Id == request.CursorId)
            : null;

        KeysetPaginationContext<Card> keysetContext = query
            .KeysetPaginate(x =>
                    x.Descending(d => d.UpdatedDate).Descending(d => d.Id),
                KeysetPaginationDirection.Forward,
                reference
            );

        List<Card> decks = await keysetContext.Query
            .Take(request.PageSize)
            .ToListAsync();
        keysetContext.EnsureCorrectOrder(decks);
        bool hasPrevious = await keysetContext.HasPreviousAsync(decks);
        bool hasNext = await keysetContext.HasNextAsync(decks);
        int count = await query.CountAsync();

        return new PaginationResult<Card>(decks, count, decks.Count, hasPrevious, hasNext);
    }


    // TODO: Add Default Value
    public async Task Create(string creatorId, CardRequest request)
    {
        Card card = new()
        {
            DeckId = request.DeckId,
            NoteId = request.NoteId,
            CreatorId = creatorId,
            Front = request.Front,
            Back = request.Back,
            IsSuspended = false,
            

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
            query.ExecuteUpdateAsync(x => x.SetProperty(x => x.DueDate, ).SetProperty(x => x.Ease, ).SetProperty(x => x.Interval))
        }
    }
}

public class UpdateUserCardRequest
{
    public UpdateUserCardTypeRequest TypeRequest {get; set;}
    
}

public enum UpdateUserCardTypeRequest
{
    Suspend, Reset
}

public class CardRequest
{
    [Required] public int DeckId {get; set;}
    [Required] public int NoteId { get; set; }
    [Required][StringLength(1000)] public string Front { get; set; }
    [Required][StringLength(1000)] public string Back { get; set; }
}