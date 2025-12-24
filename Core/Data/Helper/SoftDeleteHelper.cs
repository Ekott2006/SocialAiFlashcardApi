using System.Linq.Expressions;
using Core.Model.Helper;
using Core.Model.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Core.Data.Helper;

public static class SoftDeleteHelper
{
    public static void ApplySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        IEnumerable<IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(ISoftDelete).IsAssignableFrom(e.ClrType));

        foreach (IMutableEntityType entity in entityTypes)
        {
            ParameterExpression parameter = Expression.Parameter(entity.ClrType, "e");
            MemberExpression property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            LambdaExpression filter = Expression.Lambda(Expression.Not(property), parameter);
            
            modelBuilder.Entity(entity.ClrType).HasQueryFilter(filter);
        }
    }
}