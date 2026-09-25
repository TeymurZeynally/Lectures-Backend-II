namespace Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Contract;

public record Feeder(int Id, string CatName, int FoodGrams, int CapacityGrams, DateTimeOffset UpdatedAt);
