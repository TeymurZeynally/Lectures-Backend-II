namespace Lecture01.Sync.Http.FeedingService.Api.Feeding.Contract;

public class MealPlan
{
    public Guid PlanId { get; set; }
    public Guid CatId { get; set; }
    public int DailyGrams { get; set; }
    public List<string> Schedule { get; set; } = new();
}
