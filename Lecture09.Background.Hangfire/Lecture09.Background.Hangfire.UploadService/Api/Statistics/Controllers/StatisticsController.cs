using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Lecture09.Background.Hangfire.UploadService.Api.Statistics.Controllers;

[ApiController]
[Route("statistics")]
public class StatisticsController : ControllerBase
{
    private readonly IRecurringJobManager _recurringJobs;

    public StatisticsController(IRecurringJobManager recurringJobs)
    {
        _recurringJobs = recurringJobs;
    }

    // Запуск периодической задачи вне расписания: Trigger ставит в очередь разовое выполнение
    // по сохранённому в базе вызову метода. Расписание при этом не меняется
    [HttpPost("recalculate")]
    public IActionResult Recalculate()
    {
        _recurringJobs.Trigger("recalculate-statistics");
        return Accepted();
    }
}
