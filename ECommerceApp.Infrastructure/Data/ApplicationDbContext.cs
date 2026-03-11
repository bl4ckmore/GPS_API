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

    // Survey Tables
    public DbSet<Survey> Surveys { get; set; }
    public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
    public DbSet<SurveyResponse> SurveyResponses { get; set; }
    public DbSet<SurveyAnswer> SurveyAnswers { get; set; }
    public DbSet<SurveyType> SurveyTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // === 1. SURVEY (BaseEntity -> id) ===
        modelBuilder.Entity<Survey>(e =>
        {
            e.Property(x => x.id).HasColumnName("id");
        });

        // === 2. SURVEY TYPE (Fixes 500 Error on List) ===
        // [!] This was missing in the previous file [!]
        modelBuilder.Entity<SurveyType>(e =>
        {
            e.Property(x => x.Id).HasColumnName("id");
        });

        // === 3. SURVEY RESPONSE ===
        modelBuilder.Entity<SurveyResponse>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SurveyId).HasColumnName("survey_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Data).HasColumnName("data").HasColumnType("jsonb");

            e.HasMany(x => x.Answers)
             .WithOne()
             .HasForeignKey(a => a.ResponseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // === 4. SURVEY ANSWER ===
        modelBuilder.Entity<SurveyAnswer>(e =>
        {
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ResponseId).HasColumnName("ResponseId");
            e.Property(x => x.QuestionId).HasColumnName("QuestionId");
            e.Property(x => x.AnswerValue).HasColumnName("AnswerValue");
        });

        // === 5. SURVEY QUESTIONS ===
        modelBuilder.Entity<SurveyQuestion>(e =>
        {
            e.Property(x => x.id).HasColumnName("id");
            e.Property(x => x.SurveyId).HasColumnName("SurveyId");
            e.Property(x => x.QuestionText).HasColumnName("QuestionText");
            e.Property(x => x.Options).HasColumnName("Options").HasColumnType("jsonb");
        });

        // ... Keep your existing Product/Order configs ...
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.id);
            e.Property(x => x.Price).HasColumnType("numeric(18,2)");
            e.Property(x => x.CategoryId).HasColumnType("uuid").IsRequired(false);
            e.HasIndex(x => x.CategoryId);
            e.Property(x => x.VideoUrl).HasMaxLength(2048).IsRequired(false);
        });

        modelBuilder.Entity<Category>(e => {
            e.HasKey(x => x.id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Slug).HasMaxLength(200);
        });

        modelBuilder.Entity<Cart>(e => {
            e.HasKey(x => x.id);
            e.HasIndex(x => new { x.UserId, x.IsDeleted });
        });

        modelBuilder.Entity<CartItem>(entity => {
            entity.HasOne(ci => ci.Cart).WithMany(c => c.Items).HasForeignKey(ci => ci.CartId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ci => ci.Product).WithMany().HasForeignKey(ci => ci.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(ci => ci.CartId).HasColumnName("CartId");
            entity.Property(ci => ci.ProductId).HasColumnName("ProductId");
            entity.Property(ci => ci.qty).HasColumnName("qty");
        });

        modelBuilder.Entity<Order>(e => {
            e.HasKey(x => x.id);
            e.Property(x => x.Subtotal).HasColumnType("numeric(18,2)");
            e.Property(x => x.Shipping).HasColumnType("numeric(18,2)");
            e.Property(x => x.Discount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Tax).HasColumnType("numeric(18,2)");
            e.Property(x => x.Total).HasColumnType("numeric(18,2)");
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
        });

        modelBuilder.Entity<OrderItem>(e => {
            e.HasKey(x => x.id);
            e.Property(x => x.OrderId).HasColumnName("OrderId");
            e.Property(x => x.ProductId).HasColumnName("ProductId");
            e.Property(x => x.unitPrice).HasColumnType("numeric(18,2)");
            e.Property(x => x.qty).HasDefaultValue(1);
            e.HasIndex(x => x.OrderId);
        });
    }
}