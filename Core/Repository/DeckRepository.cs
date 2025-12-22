using Core.Data;
using Core.Dto.Common;
using Core.Dto.Deck;
using Core.Model;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Core.Repository;

public class DeckRepository(DataContext context)
{
    // TODO: Constrict the Type from the Select
    public async Task<PaginationResult<Deck>> Get(string creatorId, PaginationRequest request,
        bool isDeleted)
    {
        IQueryable<Deck> query = isDeleted
                ? context.Decks.AsNoTracking().IgnoreQueryFilters()
                    .Where(x => x.IsDeleted && x.CreatorId == creatorId)
                : context.Decks.AsNoTracking()
                    .Where(x => x.CreatorId == creatorId)
            ;

        Deck? reference = request.CursorId != null
            ? await query.FirstOrDefaultAsync(x => x.Id == request.CursorId)
            : null;

        KeysetPaginationContext<Deck> keysetContext = query
            .KeysetPaginate(x =>
                    x.Descending(d => d.UpdatedDate).Descending(d => d.Id),
                KeysetPaginationDirection.Forward,
                reference
            );

        List<Deck> decks = await keysetContext.Query
            .Take(request.PageSize)
            .ToListAsync();
        keysetContext.EnsureCorrectOrder(decks);
        bool hasPrevious = await keysetContext.HasPreviousAsync(decks);
        bool hasNext = await keysetContext.HasNextAsync(decks);
        int count = await query.CountAsync();

        return new PaginationResult<Deck>(decks, count, decks.Count, hasPrevious, hasNext);
    }

    public async Task<Deck?> Get(string creatorId, int id)
    {
        return await context.Decks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.CreatorId == creatorId);
    }

    private async Task<DeckOptionRequest?> GetUserOption(string creatorId)
    {
        var user = await context.Users
            .AsNoTracking()
            .Select(x => new { x.Id, x.DeckOption })
            .FirstOrDefaultAsync(x => x.Id == creatorId);
        return user != null ? (DeckOptionRequest)user.DeckOption : null;
    }

    public async Task<Deck?> Create(string creatorId, DeckRequest request)
    {
        DeckOptionRequest? option = request.IsUserOption ? await GetUserOption(creatorId) : request.OptionRequest;
        if (option == null) return null;

        Deck deck = new()
        {
            CreatorId = creatorId,
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            IsUserOption = request.IsUserOption,
            Option = option,
            Statistic = new DeckStatistic() { Due = 0, New = 0 }
        };
        await context.Decks.AddAsync(deck);
        await context.SaveChangesAsync();
        return deck;
    }

    public async Task<int?> Update(int id, string creatorId, DeckRequest request)
    {
        DeckOptionRequest? option = request.IsUserOption ? await GetUserOption(creatorId) : request.OptionRequest;
        if (option == null) return null;

        return await context.Decks.Where(x => x.Id == id && x.CreatorId == creatorId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(d => d.Name, request.Name)
                .SetProperty(d => d.Description, request.Description)
                .SetProperty(d => d.IsPublic, request.IsPublic)
                .SetProperty(d => d.IsUserOption, request.IsUserOption)
                .SetProperty(d => d.Option.NewCardsPerDay, option.NewCardsPerDay)
                .SetProperty(d => d.Option.ReviewLimitPerDay, option.ReviewLimitPerDay)
                .SetProperty(d => d.Option.SortOrder, option.SortOrder)
                .SetProperty(d => d.Option.InterdayLearningMix, option.InterdayLearningMix)
            );
    }

    public async Task<int> Delete(int id, string creatorId)
    {
        return await context.Decks.Where(x => x.CreatorId == creatorId && x.Id == id).ExecuteDeleteAsync();
    }


    public async Task<int> Restore(int id, string creatorId)
    {
        return await context.Decks.IgnoreQueryFilters().Where(x => x.Id == id && x.CreatorId == creatorId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(d => d.IsDeleted, false)
            );
    }
}