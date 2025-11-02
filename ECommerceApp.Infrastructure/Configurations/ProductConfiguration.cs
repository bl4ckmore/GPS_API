using ECommerceApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceApp.Infrastructure.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> b)
        {
            b.ToTable("products");
            b.HasKey(p => p.id);

            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.SKU).HasMaxLength(64);
            b.Property(p => p.Description).HasMaxLength(2000);
            b.Property(p => p.LongDescription).HasMaxLength(8000);

            // money/decimal
            b.Property(p => p.Price).HasColumnType("numeric(12,2)").IsRequired();
            b.Property(p => p.CompareAtPrice).HasColumnType("numeric(12,2)");
            b.Property(p => p.SalePrice).HasColumnType("numeric(12,2)");
            b.Property(p => p.Weight).HasColumnType("numeric(10,3)");

            // arrays / jsonb (Postgres)
            b.Property(p => p.Images).HasColumnType("text[]");
            b.Property(p => p.Features).HasColumnType("text[]");
            b.Property(p => p.Parameters).HasColumnType("jsonb");

            // optional FK to Category (uuid)
            b.Property(p => p.CategoryId).HasColumnType("uuid").IsRequired(false);

            b.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);

            // helpful indexes
            b.HasIndex(p => new { p.SKU, p.IsDeleted }).HasDatabaseName("ix_products_sku_isdeleted");
            b.HasIndex(p => new { p.Name, p.IsDeleted }).HasDatabaseName("ix_products_name_isdeleted");
            b.HasIndex(p => new { p.IsActive, p.IsFeatured });
        }
    }
}
