using Microsoft.EntityFrameworkCore;
using PolancoWatch.Domain.Entities;

namespace PolancoWatch.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<AlertRule> AlertRules { get; set; } = null!;
    public DbSet<AlertHistory> AlertHistories { get; set; } = null!;
    public DbSet<NotificationSettings> NotificationSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuration can go here if needed
    }
}
