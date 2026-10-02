using Lecture09.Background.Temporal.UploadService.Api.Videos.Contract;

namespace Lecture09.Background.Temporal.UploadService.Api.Videos.Services;

public interface IVideosService
{
    Task<UploadVideoResponse> UploadAsync(string? title);

    Task<VideoStatusResponse> GetStatusAsync(string videoId);

    Task ApproveAsync(string videoId);

    Task RejectAsync(string videoId, string reason);
}
