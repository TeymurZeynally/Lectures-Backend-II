using Lecture02.Sync.Http.FeedingService.Api.Feeding.Contract;

namespace Lecture02.Sync.Http.FeedingService.Api.Feeding.Services;

public interface IFeedingService
{
    Task<MealPlan?> CreateMealPlanAsync(Guid catId);
}
