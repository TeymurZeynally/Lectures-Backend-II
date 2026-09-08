using AutoMapper;
using Lecture02.Sync.Http.CatsService.Api.Cats.Contract;
using Lecture02.Sync.Http.CatsService.DataAccess.Entities;

namespace Lecture02.Sync.Http.CatsService.Api.Cats.Mapping;

public class CatMappingProfile : Profile
{
    public CatMappingProfile()
    {
        CreateMap<Cat, CatResponse>();
        CreateMap<CatRequest, Cat>();
    }
}
