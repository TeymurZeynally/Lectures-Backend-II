using Lecture02.Sync.Grpc.CatsService.Grpc;

namespace Lecture02.Sync.Grpc.FeedingService.External.GrpcClients;

public interface ICatsClient
{
    Task<CatMessage?> GetCatAsync(Guid catId);
}
