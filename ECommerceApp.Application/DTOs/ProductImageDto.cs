﻿namespace ECommerceApp.Application.DTOs;

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
