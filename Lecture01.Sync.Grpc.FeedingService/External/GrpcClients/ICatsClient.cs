using Lecture01.Sync.Grpc.CatsService.Grpc;

namespace Lecture01.Sync.Grpc.FeedingService.External.GrpcClients;

public interface ICatsClient
{
    Task<CatMessage?> GetCatAsync(Guid catId);
}
