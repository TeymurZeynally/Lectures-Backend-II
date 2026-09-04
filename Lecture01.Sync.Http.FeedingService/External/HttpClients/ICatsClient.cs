using Lecture01.Sync.Http.FeedingService.External.HttpClients.Models;

namespace Lecture01.Sync.Http.FeedingService.External.HttpClients;

public interface ICatsClient
{
    Task<CatDto?> GetCatAsync(Guid catId);
}
