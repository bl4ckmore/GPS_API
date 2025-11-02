// ECommerceApp.Application.DTOs/AddItemRequestDto.cs

using System;

namespace ECommerceApp.Application.DTOs
{
    // This DTO models the input from the Angular service: { productId, quantity }
    public class AddItemRequestDto
    {
        // NOTE: The front-end sends a 'number', but the backend uses Guid. 
        // We assume the actual ID in the database is a GUID, and the front-end should be sending a string representation of it.
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}