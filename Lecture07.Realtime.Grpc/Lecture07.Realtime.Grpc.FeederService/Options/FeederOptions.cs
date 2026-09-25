namespace Lecture07.Realtime.Grpc.FeederService.Options;

public sealed class FeederOptions
{
    public string FeederId { get; init; } = "feeder-1";

    public string CatName { get; init; } = "Барсик";

    public int CapacityGrams { get; init; } = 200;
}
