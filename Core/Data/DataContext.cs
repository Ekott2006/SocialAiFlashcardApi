using System.Linq.Expressions;
using Core.Model;
using Core.Model.Helper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Core.Data;

public class DataContext(DbContextOptions<DataContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
    public DbSet<Deck> Decks { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<NoteType> NoteTypes { get; set; }
    
    
    public override int SaveChanges()
    {
        HandleSoftDelete();
        HandleDateTimeHelper();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleSoftDelete();
        HandleDateTimeHelper();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        

        // Apply global query filter for soft deletable entities
        foreach (IMutableEntityType entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                    .HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static LambdaExpression GetSoftDeleteFilter(Type entityType)
    {
        ParameterExpression parameter = Expression.Parameter(entityType, "e");
        MemberExpression property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        BinaryExpression condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }

    private void HandleSoftDelete()
    {
        foreach (EntityEntry<ISoftDeletable> entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
        }
    }

    private void HandleDateTimeHelper()
    {
        IEnumerable<EntityEntry> entries = ChangeTracker
            .Entries()
            .Where(e => e is { Entity: DateTimeModel, State: EntityState.Added or EntityState.Modified });

        foreach (EntityEntry entityEntry in entries)
        {
            ((DateTimeModel)entityEntry.Entity).UpdatedDate = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((DateTimeModel)entityEntry.Entity).CreatedDate = DateTime.UtcNow;
            }
        }
    }
}