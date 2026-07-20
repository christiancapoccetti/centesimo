using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Centesimo.Infrastructure;

public sealed class CentesimoDbContextFactory : IDesignTimeDbContextFactory<CentesimoDbContext>
{
    public CentesimoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CentesimoDbContext>()
            .UseSqlite("Data Source=centesimo.design.db3")
            .Options;
        return new CentesimoDbContext(options);
    }
}
