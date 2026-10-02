using Lecture09.Background.Airflow.UploadService.Api.Videos.Contract;
using Lecture09.Background.Airflow.UploadService.Api.Videos.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lecture09.Background.Airflow.UploadService.Api.Videos.Controllers;

[ApiController]
[Route("videos")]
public class VideosController : ControllerBase
{
    private readonly IVideosService _videosService;

    public VideosController(IVideosService videosService)
    {
        _videosService = videosService;
    }

    [HttpPost]
    public async Task<ActionResult<UploadVideoResponse>> Upload(UploadVideoRequest request)
    {
        return Accepted(await _videosService.UploadAsync(request.Title));
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<UploadVideoResponse>>> UploadMany(UploadVideosRequest request)
    {
        var responses = new List<UploadVideoResponse>();

        for (var i = 0; i < request.Count; i++)
        {
            responses.Add(await _videosService.UploadAsync(null));
        }

        return Accepted(responses);
    }
}
