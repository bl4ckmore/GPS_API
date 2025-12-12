using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Core.Entities
{
    public class CartItem : BaseEntity
    {
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }
        public int qty { get; set; } = 1;

        // Add this back to fix the build error
        public decimal unitPrice { get; set; }

        [ForeignKey("CartId")]
        public Cart? Cart { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}