using Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Test.Helper;

public class DatabaseSetupHelper
{
    private readonly SqliteConnection _connection;
    protected DataContext Context { get; set; }
    protected readonly List<string> _sqlLog = [];
    
    protected DatabaseSetupHelper()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        DbContextOptions<DataContext> options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(_connection)
            .LogTo(log => _sqlLog.Add(log), Microsoft.Extensions.Logging.LogLevel.Information)
            .Options;
        Context = new DataContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose() {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

}