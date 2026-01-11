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
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<Survey> Surveys { get; set; }
    public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
    public DbSet<SurveyResponse> SurveyResponses { get; set; }
    public DbSet<SurveyAnswer> SurveyAnswers { get; set; }
    public DbSet<SurveyType> SurveyTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // === Fix for PostgreSQL Case Sensitivity ===

        // 1. Survey (იყენებს BaseEntity-ს, სადაც არის პატარა 'id')
        modelBuilder.Entity<Survey>(e =>
        {
            // აქ ვიყენებთ x.id-ს (პატარა ასოთი), რადგან BaseEntity-ში ასეა
            e.Property(x => x.id).HasColumnName("id");
        });

        // 2. SurveyResponse (ამ კლასში გვაქვს დიდი 'Id')
        modelBuilder.Entity<SurveyResponse>(e =>
        {
            e.HasKey(x => x.Id);
            // C#-ის 'Id'-ს ვაბამთ ბაზის 'id' სვეტს
            e.Property(x => x.Id).HasColumnName("id");

            e.Property(x => x.SurveyId).HasColumnName("survey_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Data).HasColumnName("data").HasColumnType("jsonb");
        });

        // ===========================================

        // Product
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.id);
            e.Property(x => x.Price).HasColumnType("numeric(18,2)");
            e.Property(x => x.CategoryId).HasColumnType("uuid").IsRequired(false);
            e.HasIndex(x => x.CategoryId);
        });

        // Category
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

        // CartItem
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasOne(ci => ci.Cart)
                  .WithMany(c => c.Items)
                  .HasForeignKey(ci => ci.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Product)
                  .WithMany()
                  .HasForeignKey(ci => ci.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(ci => ci.CartId).HasColumnName("CartId");
            entity.Property(ci => ci.ProductId).HasColumnName("ProductId");
            entity.Property(ci => ci.qty).HasColumnName("qty");
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

        // OrderItem
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