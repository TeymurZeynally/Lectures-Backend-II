namespace Lecture09.Background.Airflow.UploadService.Api.Videos.Contract;

public class UploadVideoResponse
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DagRunId { get; set; } = string.Empty;
    public string DagRunUrl { get; set; } = string.Empty;
}
