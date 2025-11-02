// ECommerceApp.Application.DTOs/CartItemDto.cs (Example structure)

using System;

namespace ECommerceApp.Application.DTOs
{
    public class CartItemDto
    {
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}