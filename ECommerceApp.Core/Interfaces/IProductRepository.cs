using System;
using System.Linq.Expressions;
using ECommerceApp.Core.Entities;

namespace ECommerceApp.Core.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Product?> GetProductWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
        int page, int pageSize, string? searchTerm = null, Guid? categoryId = null,
        decimal? minPrice = null, decimal? maxPrice = null, CancellationToken cancellationToken = default);
}