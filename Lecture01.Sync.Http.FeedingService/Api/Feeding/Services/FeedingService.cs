using Lecture01.Sync.Http.FeedingService.Api.Feeding.Contract;
using Lecture01.Sync.Http.FeedingService.External.HttpClients;

namespace Lecture01.Sync.Http.FeedingService.Api.Feeding.Services;

public class FeedingService : IFeedingService
{
    private readonly ICatsClient _catsClient;

    public FeedingService(ICatsClient catsClient)
    {
        _catsClient = catsClient;
    }

    public async Task<MealPlan?> CreateMealPlanAsync(Guid catId)
    {
        var cat = await _catsClient.GetCatAsync(catId);
        if (cat is null)
        {
            return null;
        }

        var dailyGrams = (int)(cat.WeightKg * 30 + (cat.AgeMonths < 12 ? 20 : 0));

        return new MealPlan
        {
            PlanId = Guid.NewGuid(),
            CatId = catId,
            DailyGrams = dailyGrams,
            Schedule = new List<string> { "08:00", "14:00", "20:00" }
        };
    }
}
