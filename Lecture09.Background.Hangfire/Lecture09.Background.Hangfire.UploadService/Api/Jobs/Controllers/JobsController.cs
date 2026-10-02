using Hangfire;
using Lecture09.Background.Hangfire.UploadService.Api.Jobs.Contract;
using Microsoft.AspNetCore.Mvc;

namespace Lecture09.Background.Hangfire.UploadService.Api.Jobs.Controllers;

[ApiController]
[Route("jobs")]
public class JobsController : ControllerBase
{
    // JobStorage - хранилище Hangfire (здесь Postgres). Через него читает данные и Dashboard
    private readonly JobStorage _storage;

    public JobsController(JobStorage storage)
    {
        _storage = storage;
    }

    // Monitoring API - те же цифры, что на главной странице Dashboard: сколько задач в каждом состоянии
    // и сколько Hangfire-серверов (Worker'ов) сейчас живы
    [HttpGet("summary")]
    public ActionResult<JobsSummaryResponse> GetSummary()
    {
        var statistics = _storage.GetMonitoringApi().GetStatistics();

        return Ok(new JobsSummaryResponse
        {
            Enqueued = statistics.Enqueued,
            Scheduled = statistics.Scheduled,
            Awaiting = statistics.Awaiting ?? 0,
            Processing = statistics.Processing,
            Succeeded = statistics.Succeeded,
            Failed = statistics.Failed,
            Servers = statistics.Servers
        });
    }
}
