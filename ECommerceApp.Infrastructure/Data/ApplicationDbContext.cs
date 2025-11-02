using ECommerceApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // pick up IEntityTypeConfiguration<T> in this assembly (e.g., ProductConfiguration)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Product
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.id);
            // if your Product has Price (PascalCase), keep this next line;
            // if it's lower-case 'price', change to x.price
            e.Property(x => x.Price).HasColumnType("numeric(18,2)");
            e.Property(x => x.CategoryId).HasColumnType("uuid").IsRequired(false);
            e.HasIndex(x => x.CategoryId);
        });

        // Category (key is 'id', not 'Id')
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Slug).HasMaxLength(200);
        });

        // Cart
        modelBuilder.Entity<Cart>(e =>
        {
            e.HasKey(x => x.id);
            e.HasIndex(x => new { x.UserId, x.IsDeleted });
        });

        // CartItem — your entity uses lower-case 'qty' and 'unitPrice'
        modelBuilder.Entity<CartItem>(e =>
        {
            e.HasKey(x => x.id);

            e.Property(x => x.qty).HasDefaultValue(1);
            e.Property(x => x.unitPrice).HasColumnType("numeric(18,2)");

            e.HasIndex(x => x.CartId);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => new { x.CartId, x.IsDeleted });

            e.HasOne(x => x.Cart)
             .WithMany()                 // use .WithMany(c => c.Items) if Cart has Items
             .HasForeignKey(x => x.CartId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Order
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.id);
            e.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
            e.Property(x => x.Shipping).HasColumnType("numeric(18,2)");
            e.Property(x => x.Discount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Tax).HasColumnType("numeric(18,2)");
            e.Property(x => x.Total).HasColumnType("numeric(18,2)");
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
        });

        // OrderItem — lower-case 'qty' and 'unitPrice'
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.id);
            e.Property(x => x.unitPrice).HasColumnType("numeric(18,2)");
            e.Property(x => x.qty).HasDefaultValue(1);

            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => new { x.OrderId, x.ProductId });
        });
    }
}
