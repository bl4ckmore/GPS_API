using AutoMapper;
using ECommerceApp.Core.Entities;
using ECommerceApp.Application.DTOs;

namespace ECommerceApp.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity -> DTO
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Attributes, o => o.MapFrom(s => s.Parameters))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));

        // DTO -> Entity
        CreateMap<ProductDto, Product>()
            .ForMember(d => d.Parameters, o => o.MapFrom(s => s.Attributes))
            .ForMember(d => d.Category, o => o.Ignore())             // set by CategoryId
            .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.CategoryId));
    }
}
