using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NineYi.Ai.CodeReview.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CodeReviewDbContext>
{
    public CodeReviewDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CodeReviewDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=CodeReview.db";

        optionsBuilder.UseSqlite(connectionString);

        return new CodeReviewDbContext(optionsBuilder.Options);
    }
}
