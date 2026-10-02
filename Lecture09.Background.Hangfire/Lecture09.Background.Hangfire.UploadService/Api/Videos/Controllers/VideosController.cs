using Lecture09.Background.Hangfire.UploadService.Api.Videos.Contract;
using Lecture09.Background.Hangfire.UploadService.Api.Videos.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lecture09.Background.Hangfire.UploadService.Api.Videos.Controllers;

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
    public ActionResult<UploadVideoResponse> Upload(UploadVideoRequest request)
    {
        return Accepted(_videosService.Upload(request.Title));
    }

    [HttpPost("batch")]
    public ActionResult<List<UploadVideoResponse>> UploadMany(UploadVideosRequest request)
    {
        return Accepted(Enumerable.Range(0, request.Count).Select(_ => _videosService.Upload(null)).ToList());
    }
}
