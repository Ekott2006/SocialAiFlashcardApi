using Core.Dto.Common;
using Core.Model.Interface;
using Microsoft.EntityFrameworkCore;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Core.Repository;

public class Repository
{
    protected static async Task<PaginationResult<TEntity>> PaginateAsync<TEntity, TCursor>(
        IQueryable<TEntity> query,
        PaginationRequest<TCursor> request) 
        where TEntity : class, IPagination<TCursor> 
        where TCursor : struct 
    {
        // 1. Reference Lookup (The Cursor)
        TEntity? reference = null;
        if (request.CursorId != null)
        {
            reference = await query.FirstOrDefaultAsync(x => x.Id.Equals(request.CursorId));
        }

        // 2. Keyset Setup 
        KeysetPaginationContext<TEntity> keysetContext = query.KeysetPaginate(
            builder => builder.Descending(d => d.UpdatedAt).Descending(d => d.Id),
            KeysetPaginationDirection.Forward,
            reference
        );

        // 3. Execution with "Fetch N+1" Optimization
        List<TEntity> items = await keysetContext.Query
            .Take(request.PageSize + 1) 
            .ToListAsync();

        // 4. Determine Pagination Metadata
        bool hasNext = items.Count > request.PageSize;
    
        if (hasNext)
        {
            items.RemoveAt(items.Count - 1); // Remove the extra item
        }

        keysetContext.EnsureCorrectOrder(items);
    
        // We still use HasPreviousAsync because determining it via 'Take' 
        // is significantly more complex for the first/last page logic.
        bool hasPrevious = await keysetContext.HasPreviousAsync(items);

        return new PaginationResult<TEntity>(
            items, 
            items.Count, 
            hasPrevious, 
            hasNext
        );
    }
}