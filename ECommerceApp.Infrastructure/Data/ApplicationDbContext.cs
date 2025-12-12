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
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>(); // <-- ADD THIS LINE

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
        modelBuilder.Entity<CartItem>(entity =>
        {
            // 1. Force Relationship (Cart has many Items)
            entity.HasOne(ci => ci.Cart)
                  .WithMany(c => c.Items)
                  .HasForeignKey(ci => ci.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 2. Force Relationship (Item has one Product)
            entity.HasOne(ci => ci.Product)
                  .WithMany()
                  .HasForeignKey(ci => ci.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 3. FORCE COLUMN NAMES (Fixes "Cartid does not exist" error)
            entity.Property(ci => ci.CartId).HasColumnName("CartId");
            entity.Property(ci => ci.ProductId).HasColumnName("ProductId");

            // 4. Force lowercase 'qty'
            entity.Property(ci => ci.qty).HasColumnName("qty");

            // ❌ REMOVED: entity.Ignore(ci => ci.unitPrice); 
            // We deleted unitPrice from the class, so we must delete this line too.
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

            e.Property(x => x.OrderId).HasColumnName("OrderId"); 

            e.Property(x => x.ProductId).HasColumnName("ProductId"); 

            e.Property(x => x.unitPrice).HasColumnType("numeric(18,2)");
            e.Property(x => x.qty).HasDefaultValue(1);

            e.HasIndex(x => x.OrderId);
        });
    }
}
