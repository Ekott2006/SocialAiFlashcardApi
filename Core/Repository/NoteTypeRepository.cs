using System.ComponentModel.DataAnnotations;
using Core.Data;
using Core.Dto.Common;
using Core.Model;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Core.Repository;

public class NoteTypeRepository(DataContext context)
{
    // TODO: Constrict the Response Type from the Select
    public async Task<PaginationResult<NoteType>> Get(string creatorId, PaginationRequest request,
        bool isDeleted)
    {
        IQueryable<NoteType> query = isDeleted
                ? context.NoteTypes.AsNoTracking().IgnoreQueryFilters()
                    .Where(x => x.IsDeleted && x.CreatorId == creatorId)
                : context.NoteTypes.AsNoTracking()
                    .Where(x => x.CreatorId == creatorId || x.CreatorId == null)
            ;

        NoteType? reference = request.CursorId != null
            ? await query.FirstOrDefaultAsync(x => x.Id == request.CursorId)
            : null;

        KeysetPaginationContext<NoteType> keysetContext = query
            .KeysetPaginate(x =>
                    x.Descending(d => d.UpdatedDate).Descending(d => d.Id),
                KeysetPaginationDirection.Forward,
                reference
            );

        List<NoteType> decks = await keysetContext.Query
            .Take(request.PageSize)
            .ToListAsync();
        keysetContext.EnsureCorrectOrder(decks);
        bool hasPrevious = await keysetContext.HasPreviousAsync(decks);
        bool hasNext = await keysetContext.HasNextAsync(decks);
        int count = await query.CountAsync();

        return new PaginationResult<NoteType>(decks, count, decks.Count, hasPrevious, hasNext);
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
            Fields = request.Fields,
            Templates = request.Templates,
            CssStyle = request.CssStyle,
        };
        await context.NoteTypes.AddAsync(deck);
        await context.SaveChangesAsync();
        return deck;
    }


    public async Task<int?> Update(int id, string creatorId, NoteTypeRequest request)
    {
        if (await DoesNoteNameExist(request.Name)) return null;
        return await context.NoteTypes.Where(x => x.Id == id && x.CreatorId == creatorId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(d => d.Name, request.Name)
                .SetProperty(d => d.Fields, request.Fields)
                .SetProperty(d => d.Templates, request.Templates)
                .SetProperty(d => d.CssStyle, request.CssStyle)
            );
    }

    public async Task<int> Delete(int id, string creatorId)
    {
        return await context.NoteTypes.Where(x => x.CreatorId == creatorId && x.Id == id).ExecuteDeleteAsync();
    }


    public async Task<int> Restore(int id, string creatorId)
    {
        return await context.NoteTypes.IgnoreQueryFilters().Where(x => x.Id == id && x.CreatorId == creatorId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(d => d.IsDeleted, false)
            );
    }
}

public class NoteTypeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [StringLength(5000)] public string CssStyle { get; set; } = string.Empty;
    [Required] [MinLength(1)] public string[] Fields { get; set; }
    [Required] [MinLength(1)] public NoteTypeTemplates[] Templates { get; set; }
}

public class NoteTypeTemplatesRequest
{
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
}