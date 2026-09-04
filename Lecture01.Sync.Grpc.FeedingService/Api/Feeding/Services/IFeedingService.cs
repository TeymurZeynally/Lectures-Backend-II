using Lecture01.Sync.Grpc.FeedingService.Api.Feeding.Contract;

namespace Lecture01.Sync.Grpc.FeedingService.Api.Feeding.Services;

public interface IFeedingService
{
    Task<MealPlan?> CreateMealPlanAsync(Guid catId);
}
