using System.ComponentModel.DataAnnotations;

namespace Core.Dto.Common;

public class PaginationRequest<T> where T : struct
{
    public T? CursorId { get; set; }
    [Range(1, 100)] public int PageSize { get; set; }
}