using Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Contract;

namespace Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Services;

public interface IFeedersService
{
    List<Feeder> GetAll();
    Feeder? Eat(int id, int grams);
    Feeder? Refill(int id);
    IAsyncEnumerable<Feeder> SubscribeAsync(CancellationToken cancellationToken);
}
