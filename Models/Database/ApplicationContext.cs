using Microsoft.EntityFrameworkCore;

namespace Models.Database;

public class ApplicationContext : DbContext
{
    public DbSet<Label> Labels { get; set; }
    public string DbPath { get; }
    public ApplicationContext()
    {
        DbPath = AppContext.BaseDirectory + "data/app.db";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}

public class Label
{
    public Int64 Id { get; set; }
    public Int64 IndexId { get; set; }
    public Int64 LabelId { get; set; }
    public string LabelName { get; set; } // Needed for API.GitHub.UpdateRepositoryLabel() and API.GitHub.DeleteRepositoryLabel()
    public Int64 RepositoryId { get; set; }
}
