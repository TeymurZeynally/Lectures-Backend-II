using AutoMapper;
using global::Grpc.Core;
using Lecture01.Sync.Grpc.CatsService.Api.Cats.Services;
using Lecture01.Sync.Grpc.CatsService.DataAccess.Entities;
using Lecture01.Sync.Grpc.CatsService.Grpc;

namespace Lecture01.Sync.Grpc.CatsService.Api.Cats.Grpc;

public class CatsGrpcServiceHandler : CatsGrpcService.CatsGrpcServiceBase
{
    private readonly ICatsService _catsService;
    private readonly IMapper _mapper;

    public CatsGrpcServiceHandler(ICatsService catsService, IMapper mapper)
    {
        _catsService = catsService;
        _mapper = mapper;
    }

    public override async Task<CatMessage> CreateCat(CreateCatRequest request, ServerCallContext context)
    {
        var created = await _catsService.CreateAsync(_mapper.Map<Cat>(request));
        return _mapper.Map<CatMessage>(created);
    }

    public override async Task<CatMessage> GetCat(GetCatRequest request, ServerCallContext context)
    {
        var cat = await _catsService.GetByIdAsync(Guid.Parse(request.Id));
        if (cat is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Cat {request.Id} not found"));
        }

        return _mapper.Map<CatMessage>(cat);
    }

    public override async Task<ListCatsResponse> ListCats(ListCatsRequest request, ServerCallContext context)
    {
        var cats = await _catsService.GetAllAsync();
        var response = new ListCatsResponse();
        response.Cats.AddRange(_mapper.Map<List<CatMessage>>(cats));
        return response;
    }

    public override async Task<CatMessage> UpdateCat(UpdateCatRequest request, ServerCallContext context)
    {
        var updated = await _catsService.UpdateAsync(Guid.Parse(request.Id), _mapper.Map<Cat>(request));
        if (updated is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Cat {request.Id} not found"));
        }

        return _mapper.Map<CatMessage>(updated);
    }

    public override async Task<DeleteCatResponse> DeleteCat(DeleteCatRequest request, ServerCallContext context)
    {
        var deleted = await _catsService.DeleteAsync(Guid.Parse(request.Id));
        return new DeleteCatResponse { Success = deleted };
    }
}
