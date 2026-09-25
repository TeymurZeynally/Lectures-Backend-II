using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Contract;

namespace Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Services;

public class FeedersService : IFeedersService
{
    private readonly Dictionary<int, Feeder> _feeders;
    private readonly ConcurrentDictionary<Guid, Channel<Feeder>> _subscribers = new();
    private readonly Lock _lock = new();

    public FeedersService()
    {
        _feeders = new[] { "Барсик", "Шлёпа", "Блинк", "Бендер", "Кот Окак", "Кот в сапогах" }
            .Select((name, index) => new Feeder(index + 1, name, 200, 200, DateTimeOffset.UtcNow))
            .ToDictionary(f => f.Id);
    }

    public List<Feeder> GetAll()
    {
        lock (_lock)
        {
            return _feeders.Values.ToList();
        }
    }

    public Feeder? Eat(int id, int grams)
    {
        return Update(id, f => f with { FoodGrams = Math.Max(0, f.FoodGrams - grams) });
    }

    public Feeder? Refill(int id)
    {
        return Update(id, f => f with { FoodGrams = f.CapacityGrams });
    }

    public async IAsyncEnumerable<Feeder> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<Feeder>();
        _subscribers[id] = channel;

        try
        {
            foreach (var feeder in GetAll())
            {
                yield return feeder;
            }

            await foreach (var feeder in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return feeder;
            }
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
        }
    }

    private Feeder? Update(int id, Func<Feeder, Feeder> change)
    {
        Feeder updated;
        lock (_lock)
        {
            if (!_feeders.TryGetValue(id, out var existing))
            {
                return null;
            }

            updated = change(existing) with { UpdatedAt = DateTimeOffset.UtcNow };
            _feeders[id] = updated;
        }

        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(updated);
        }

        return updated;
    }
}
