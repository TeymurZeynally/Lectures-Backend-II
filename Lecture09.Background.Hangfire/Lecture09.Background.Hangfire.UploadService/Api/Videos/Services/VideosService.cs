using Hangfire;
using Lecture09.Background.Hangfire.Contracts;
using Lecture09.Background.Hangfire.UploadService.Api.Videos.Contract;

namespace Lecture09.Background.Hangfire.UploadService.Api.Videos.Services;

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

    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<VideosService> _logger;

    public VideosService(IBackgroundJobClient jobs, ILogger<VideosService> logger)
    {
        _jobs = jobs;
        _logger = logger;
    }

    public UploadVideoResponse Upload(string? title)
    {
        var videoId = Guid.NewGuid().ToString()[..8];
        title = string.IsNullOrWhiteSpace(title) ? Titles[Random.Shared.Next(Titles.Length)] : title;

        var validate = _jobs.Enqueue<IVideoJobs>(x => x.ValidateAsync(videoId, title, CancellationToken.None));

        var transcode1080 = _jobs.ContinueJobWith<IVideoJobs>(validate, x => x.TranscodeAsync(videoId, "1080p", CancellationToken.None));
        var transcode720 = _jobs.ContinueJobWith<IVideoJobs>(transcode1080, x => x.TranscodeAsync(videoId, "720p", CancellationToken.None));
        var transcode480 = _jobs.ContinueJobWith<IVideoJobs>(transcode720, x => x.TranscodeAsync(videoId, "480p", CancellationToken.None));
        var publish = _jobs.ContinueJobWith<IVideoJobs>(transcode480, x => x.PublishAsync(videoId, CancellationToken.None));


        var deleteOriginal = _jobs.Schedule<IVideoJobs>(
            x => x.DeleteOriginalAsync(videoId, CancellationToken.None),
            TimeSpan.FromSeconds(60));

        _logger.LogInformation(
            "Video {VideoId} \"{Title}\" uploaded: validate #{Validate} -> 1080p #{Transcode1080} -> 720p #{Transcode720} -> 480p #{Transcode480} -> publish #{Publish}, delete original #{DeleteOriginal} in 60 s",
            videoId, title, validate, transcode1080, transcode720, transcode480, publish, deleteOriginal);

        return new UploadVideoResponse
        {
            VideoId = videoId,
            Title = title,
            Jobs =
            [
                new VideoJobResponse { Name = "проверка", JobId = validate },
                new VideoJobResponse { Name = "1080p", JobId = transcode1080 },
                new VideoJobResponse { Name = "720p", JobId = transcode720 },
                new VideoJobResponse { Name = "480p", JobId = transcode480 },
                new VideoJobResponse { Name = "публикация", JobId = publish },
                new VideoJobResponse { Name = "удаление исходника", JobId = deleteOriginal }
            ]
        };
    }
}
