using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Application.Common;
using ECommerceApp.Application.DTOs;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;

namespace ECommerceApp.Application.Features.Products.Queries;

public record GetProductsQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ProductDto>>;

public class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IGenericRepository<Product> _repo;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IGenericRepository<Product> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        // basic pagination over IQueryable if your repo exposes it; otherwise load all and paginate
        // Here we'll use DbSet via context trick: add a method to repo to expose queryable if you like.
        // For now, do simple list then paginate (fine to compile; optimize later).

        var all = await _repo.GetAllAsync(ct);
        var total = all.Count();

        var pageItems = all
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dto = _mapper.Map<List<ProductDto>>(pageItems);

        return PagedResult<ProductDto>.Create(dto, total, request.Page, request.PageSize);
    }
}
