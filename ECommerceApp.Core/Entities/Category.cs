using ECommerceApp.Core.Entities;


namespace ECommerceApp.Core.Entities; 


public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategoriesi { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>(); 
}