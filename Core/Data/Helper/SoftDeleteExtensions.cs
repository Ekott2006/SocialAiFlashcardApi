using Core.Model.Helper;
using Core.Model.Interface;
using Microsoft.EntityFrameworkCore;

namespace Core.Data.Helper;

public static class SoftDeleteExtensions
{
    public static Task<int> SetSoftDeleteAsync<T>(this IQueryable<T> query, bool isDeleted) 
        where T : class, ISoftDelete
    {
        return query.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, isDeleted));
    }
}