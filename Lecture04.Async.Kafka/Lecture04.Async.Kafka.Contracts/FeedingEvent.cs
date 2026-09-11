namespace Lecture04.Async.Kafka.Contracts;

public sealed class FeedingEvent(long sequenceNumber, int catId, DateTimeOffset fedAt)
{
    public long SequenceNumber { get; } = sequenceNumber;
    public int CatId { get; } = catId;
    public DateTimeOffset FedAt { get; } = fedAt;
}
