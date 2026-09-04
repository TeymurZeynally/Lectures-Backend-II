using Lecture01.Sync.Http.FeedingService.Api.Feeding.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lecture01.Sync.Http.FeedingService.Api.Feeding.Controllers;

[ApiController]
[Route("meal-plans")]
public class FeedingController : ControllerBase
{
    private readonly IFeedingService _feedingService;

    public FeedingController(IFeedingService feedingService)
    {
        _feedingService = feedingService;
    }

    public record CreateMealPlanRequest(Guid CatId);

    [HttpPost]
    public async Task<IActionResult> Create(CreateMealPlanRequest request)
    {
        var plan = await _feedingService.CreateMealPlanAsync(request.CatId);
        return plan is null ? NotFound() : Ok(plan);
    }
}
