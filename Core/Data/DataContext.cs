using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Core.Data.Helper;
using Core.Model;
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
    

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplySoftDeleteFilters();
        
        builder.Entity<Note>().Property(b => b.Data).HasJsonConversion();
        builder.Entity<NoteType>().Property(b => b.Templates).HasJsonConversion();
       
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.AddInterceptors(
            new TimestampInterceptor(), 
            new SoftDeleteInterceptor()
        );
    }
}