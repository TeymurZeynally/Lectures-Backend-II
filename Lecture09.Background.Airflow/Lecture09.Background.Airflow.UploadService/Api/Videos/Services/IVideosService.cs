using Lecture09.Background.Airflow.UploadService.Api.Videos.Contract;

namespace Lecture09.Background.Airflow.UploadService.Api.Videos.Services;

public interface IVideosService
{
    Task<UploadVideoResponse> UploadAsync(string? title);
}
