using AutoMapper;
using Lecture01.Sync.Http.CatsService.Api.Cats.Contract;
using Lecture01.Sync.Http.CatsService.DataAccess.Entities;

namespace Lecture01.Sync.Http.CatsService.Api.Cats.Mapping;

public class CatMappingProfile : Profile
{
    public CatMappingProfile()
    {
        CreateMap<Cat, CatResponse>();
        CreateMap<CatRequest, Cat>();
    }
}
