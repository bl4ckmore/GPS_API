using System;
using System.Collections.Generic;

namespace ECommerceApp.Core.Entities
{
    public class Category : BaseEntity
    {
        public new Guid id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
