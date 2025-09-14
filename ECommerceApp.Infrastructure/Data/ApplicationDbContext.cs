using ECommerceApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Products (arrays/jsonb are supported by Npgsql)
        b.Entity<Product>(e =>
        {
            e.Property(p => p.Features).HasColumnType("text[]");
            e.Property(p => p.Images).HasColumnType("text[]");
            e.Property(p => p.Parameters).HasColumnType("jsonb");
        });

        // Users
        b.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(128);
        });

        // Login audit
        b.Entity<UserLogin>(e =>
        {
            e.ToTable("user_logins");
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.Property(x => x.Provider).HasMaxLength(64);
            e.Property(x => x.Username).HasMaxLength(128);
            e.Property(x => x.IpAddress).HasMaxLength(64);
        });
    }
}
