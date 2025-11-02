using ECommerceApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECommerceApp.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db, IConfiguration cfg)
        {
            var fleetCategory = await EnsureCategoryAsync(db, "fleet");

            const string sku = "R12L-4G";
            var exists = await db.Products.AnyAsync(p => p.SKU == sku);
            if (!exists)
            {
                db.Products.Add(new Product
                {
                    Name = "R12L – 4G Fleet and Asset GPS Tracker",
                    Description = "Global 4G CAT1 vehicle/asset tracker with GPS/BDS dual positioning, IP65, remote fuel cut-off, alarms.",
                    LongDescription = "Enterprise-grade tracker for fleet and assets.",
                    SKU = sku,
                    Price = 129.00m,
                    CompareAtPrice = 149.00m,
                    SalePrice = 109.00m,
                    Stock = 25,
                    Weight = 0.036m,
                    IsActive = true,
                    IsFeatured = true,

                    Category = fleetCategory,

                    Type = "fleet",
                    Features = new[]
                    {
                        "Vibration Alarm", "Remote Fuel Cut-off", "Power Failure Alarm",
                        "Over Speed Alarm", "ACC Detection", "Track Playback"
                    },
                    Parameters = new Dictionary<string, string>
                    {
                        ["Positioning Mode"] = "BDS/GPS/GNSS",
                        ["TTFF (Open Sky)"] = "Hot <2s, Cold <38s",
                        ["Communication"] = "4G CAT1",
                        ["IP Rating"] = "IP65"
                    },
                    ImageUrl = "/assets/images/default-fleet-tracker.jpg",
                    Images = new[] { "/assets/images/default-fleet-tracker.jpg" },
                    Rating = 4.6,
                    ReviewCount = 34,
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
        }

        private static async Task<Category> EnsureCategoryAsync(ApplicationDbContext db, string name)
        {
            var slug = name.Trim().ToLowerInvariant();
            var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            if (existing is not null) return existing;

            var cat = new Category { Name = name, Slug = slug };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            return cat;
        }
    }
}
