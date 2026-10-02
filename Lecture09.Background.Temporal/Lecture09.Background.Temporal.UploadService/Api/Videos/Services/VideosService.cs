using Lecture09.Background.Temporal.UploadService.Api.Videos.Contract;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;

namespace Lecture09.Background.Temporal.UploadService.Api.Videos.Services;

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

    private readonly ITemporalClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VideosService> _logger;

    public VideosService(ITemporalClient client, IConfiguration configuration, ILogger<VideosService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<UploadVideoResponse> UploadAsync(string? title)
    {
        var videoId = Guid.NewGuid().ToString()[..8];
        title = string.IsNullOrWhiteSpace(title) ? Titles[Random.Shared.Next(Titles.Length)] : title;

        var handle = await _client.StartWorkflowAsync(
            "ProcessVideo",
            [videoId, title],
            new WorkflowOptions(id: $"video-{videoId}", taskQueue: "video-processing"));

        _logger.LogInformation("Video {VideoId} \"{Title}\" uploaded, workflow {WorkflowId} started", videoId, title, handle.Id);

        return new UploadVideoResponse
        {
            VideoId = videoId,
            Title = title,
            WorkflowId = handle.Id,
            WorkflowUrl = $"{_configuration["Temporal:UiUrl"]}/namespaces/default/workflows/{handle.Id}"
        };
    }

    public async Task<VideoStatusResponse> GetStatusAsync(string videoId)
    {
        var handle = _client.GetWorkflowHandle($"video-{videoId}");

        var description = await handle.DescribeAsync();

        if (description.Status != WorkflowExecutionStatus.Running)
        {
            var status = description.Status == WorkflowExecutionStatus.Completed ? "published" : description.Status.ToString();
            return new VideoStatusResponse { Status = status, Finished = true };
        }

        try
        {
            var status = await handle.QueryAsync<string>("Status", [], new WorkflowQueryOptions
            {
                Rpc = new RpcOptions { Timeout = TimeSpan.FromSeconds(2) }
            });

            return new VideoStatusResponse { Status = status, WaitingForApproval = status == "waiting for approval" };
        }
        catch (Exception)
        {
            return new VideoStatusResponse { Status = "no worker to answer the query" };
        }
    }

    public async Task ApproveAsync(string videoId)
    {
        await _client.GetWorkflowHandle($"video-{videoId}").SignalAsync("approve", []);
        _logger.LogInformation("Video {VideoId} approved", videoId);
    }

    public async Task RejectAsync(string videoId, string reason)
    {
        await _client.GetWorkflowHandle($"video-{videoId}").SignalAsync("reject", [reason]);
        _logger.LogInformation("Video {VideoId} rejected: {Reason}", videoId, reason);
    }
}
