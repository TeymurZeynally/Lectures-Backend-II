using System.Diagnostics;
using Lecture10.Tracing.Sync.ScheduleService.Tracing;

namespace Lecture10.Tracing.Sync.ScheduleService.Api.Schedule.Repositories;

public class SlotsRepository : ISlotsRepository
{
    private readonly ILogger<SlotsRepository> _logger;

    public SlotsRepository(ILogger<SlotsRepository> logger)
    {
        _logger = logger;
    }

    public async Task<Guid?> ReserveAsync(Guid doctorId, DateTime startsAt)
    {
        using var activity = AppTracing.Source.StartActivity("SlotsRepository.ReserveAsync");

        using var query = AppTracing.Source.StartActivity("UPDATE slots", ActivityKind.Client)
            ?.SetTag("db.system.name", "postgresql")
            .SetTag("db.namespace", "schedule-db")
            .SetTag("db.query.text", "UPDATE slots SET reserved = true WHERE doctor_id = $1 AND starts_at = $2 AND reserved = false RETURNING id");

        var elapsedMs = Random.Shared.Next(15, 40);
        await Task.Delay(elapsedMs);

        var rows = Random.Shared.NextDouble() < 0.05 ? 0 : 1;
        query?.SetTag("db.response.returned_rows", rows);

        _logger.LogInformation("UPDATE slots completed in {ElapsedMs} ms, rows: {Rows}", elapsedMs, rows);

        return rows == 0 ? null : Guid.NewGuid();
    }
}
