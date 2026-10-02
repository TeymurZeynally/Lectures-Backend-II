namespace Lecture09.Background.Hangfire.UploadService.Api.Jobs.Contract;

public class JobsSummaryResponse
{
    public long Enqueued { get; set; }
    public long Scheduled { get; set; }
    public long Awaiting { get; set; }
    public long Processing { get; set; }
    public long Succeeded { get; set; }
    public long Failed { get; set; }
    public long Servers { get; set; }
}
