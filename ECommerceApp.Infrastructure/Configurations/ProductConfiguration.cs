using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerceApp.Core.Entities;

namespace ECommerceApp.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Requires Microsoft.EntityFrameworkCore.Relational
        builder.ToTable("Products");

        // KEY — assumes Product : BaseEntity with Guid Id
        builder.HasKey(p => p.id);

        // Properties
        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(p => p.Description)
               .HasMaxLength(500);

        builder.Property(p => p.LongDescription)
               .HasMaxLength(5000);

        builder.Property(p => p.SKU)
               .IsRequired()
               .HasMaxLength(50);

        // Decimal precision (provider-agnostic)
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.CompareAtPrice).HasPrecision(18, 2);
        builder.Property(p => p.Weight).HasPrecision(10, 3);

        // Indexes
        builder.HasIndex(p => p.SKU).IsUnique();
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsFeatured);

        // Category relationship
        builder.HasOne(p => p.Category)
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // Images relationship
        // Use SHADOW FK so ProductImage doesn't need a ProductId property or a back navigation.
        builder.HasMany(p => p.Images)
               .WithOne()                          // no navigation on ProductImage
               .HasForeignKey("ProductId")         // shadow FK column will be created
               .OnDelete(DeleteBehavior.Cascade);
    }
}
