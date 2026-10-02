namespace Lecture09.Background.Temporal.UploadService.Api.Videos.Contract;

public class VideoStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public bool WaitingForApproval { get; set; }
    public bool Finished { get; set; }
}
