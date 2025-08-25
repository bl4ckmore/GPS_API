using AutoMapper;
using ECommerceApp.Core.Entities;
using ECommerceApp.Application.DTOs;

namespace ECommerceApp.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<ProductAttribute, ProductAttributeDto>();

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Attributes, opt => opt.MapFrom(s => s.Attributes))
            .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images))
            .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Category));
    }
}
