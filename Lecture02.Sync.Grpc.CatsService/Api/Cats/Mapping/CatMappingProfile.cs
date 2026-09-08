using AutoMapper;
using Lecture02.Sync.Grpc.CatsService.DataAccess.Entities;
using Lecture02.Sync.Grpc.CatsService.Grpc;

namespace Lecture02.Sync.Grpc.CatsService.Api.Cats.Mapping;

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
