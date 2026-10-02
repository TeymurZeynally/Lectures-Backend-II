namespace Lecture09.Background.Hangfire.UploadService.Api.Videos.Contract;

public class UploadVideoResponse
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<VideoJobResponse> Jobs { get; set; } = [];
}
