using Lecture02.Sync.Grpc.FeedingService.Api.Feeding.Contract;

namespace Lecture02.Sync.Grpc.FeedingService.Api.Feeding.Services;

public interface IFeedingService
{
    Task<MealPlan?> CreateMealPlanAsync(Guid catId);
}
