using Lecture09.Background.Airflow.UploadService.Api.Videos.Contract;
using Lecture09.Background.Airflow.UploadService.External.HttpClients;

namespace Lecture09.Background.Airflow.UploadService.Api.Videos.Services;

public class VideosService : IVideosService
{
    private static readonly string[] Titles =
    [
        "Путешествие на Байкал",
        "Обзор нового ноутбука",
        "Рецепт домашнего хлеба",
        "Сборка компьютера с нуля",
        "Уроки гитары для начинающих",
        "Разбор шахматной партии",
        "Ремонт велосипеда своими руками",
        "Таймлапс заката над морем",
        "Поход в горы: день первый",
        "Обзор настольной игры",
        "Студент объясняет пропущенный дедлайн 10 часов",
        "Утренняя пробежка в парке"
    ];

    private readonly IAirflowClient _airflowClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VideosService> _logger;

    public VideosService(IAirflowClient airflowClient, IConfiguration configuration, ILogger<VideosService> logger)
    {
        _airflowClient = airflowClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<UploadVideoResponse> UploadAsync(string? title)
    {
        var videoId = Guid.NewGuid().ToString()[..8];
        title = string.IsNullOrWhiteSpace(title) ? Titles[Random.Shared.Next(Titles.Length)] : title;

        var dagRun = await _airflowClient.TriggerDagRunAsync(
            "video_processing",
            $"video-{videoId}",
            new { video_id = videoId, title });

        _logger.LogInformation(
            "Video {VideoId} \"{Title}\" uploaded, DAG run {DagRunId} created ({State})",
            videoId, title, dagRun.DagRunId, dagRun.State);

        return new UploadVideoResponse
        {
            VideoId = videoId,
            Title = title,
            DagRunId = dagRun.DagRunId,
            DagRunUrl = $"{_configuration["Airflow:Url"]}/dags/video_processing/runs/{dagRun.DagRunId}"
        };
    }
}
