using AutoMapper;
using Lecture01.Sync.Grpc.CatsService.DataAccess.Entities;
using Lecture01.Sync.Grpc.CatsService.Grpc;

namespace Lecture01.Sync.Grpc.CatsService.Api.Cats.Mapping;

public class CatMappingProfile : Profile
{
    public CatMappingProfile()
    {
        CreateMap<Cat, CatMessage>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()));

        CreateMap<CreateCatRequest, Cat>();

        CreateMap<UpdateCatRequest, Cat>()
            .ForMember(d => d.Id, o => o.Ignore());
    }
}
