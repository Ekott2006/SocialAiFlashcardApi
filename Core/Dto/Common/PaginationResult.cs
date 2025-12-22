namespace Core.Dto.Common;

public record PaginationResult<T>(
    IReadOnlyList<T> Data,
    int TotalCount,
    int PageSize,
    bool HasPrevious,
    bool HasNext
);