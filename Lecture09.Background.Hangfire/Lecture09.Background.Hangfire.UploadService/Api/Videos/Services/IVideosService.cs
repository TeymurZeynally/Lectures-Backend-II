using Lecture09.Background.Hangfire.UploadService.Api.Videos.Contract;

namespace Lecture09.Background.Hangfire.UploadService.Api.Videos.Services;

public interface IVideosService
{
    UploadVideoResponse Upload(string? title);
}
