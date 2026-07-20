using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure.Tests;

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public CentesimoDbContext Context { get; }

    public TestDatabase()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<CentesimoDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new CentesimoDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
