using Grpc.Core;
using Lecture01.Sync.Grpc.CatsService.Grpc;

namespace Lecture01.Sync.Grpc.FeedingService.External.GrpcClients;

public class CatsClient : ICatsClient
{
    private readonly CatsGrpcService.CatsGrpcServiceClient _client;

    public CatsClient(CatsGrpcService.CatsGrpcServiceClient client)
    {
        _client = client;
    }

    public async Task<CatMessage?> GetCatAsync(Guid catId)
    {
        try
        {
            return await _client.GetCatAsync(new GetCatRequest { Id = catId.ToString() });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
