namespace Lecture09.Background.Temporal.UploadService.Api.Videos.Contract;

public class UploadVideoResponse
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowUrl { get; set; } = string.Empty;
}
