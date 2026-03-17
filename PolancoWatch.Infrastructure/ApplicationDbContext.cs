using Microsoft.EntityFrameworkCore;
using PolancoWatch.Domain.Entities;

namespace PolancoWatch.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }
    public DbSet<AlertHistory> AlertHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuration can go here if needed
    }
}
