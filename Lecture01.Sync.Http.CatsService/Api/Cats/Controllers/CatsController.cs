using AutoMapper;
using Lecture01.Sync.Http.CatsService.Api.Cats.Contract;
using Lecture01.Sync.Http.CatsService.Api.Cats.Services;
using Lecture01.Sync.Http.CatsService.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lecture01.Sync.Http.CatsService.Api.Cats.Controllers;

[ApiController]
[Route("cats")]
public class CatsController : ControllerBase
{
    private readonly ICatsService _catsService;
    private readonly IMapper _mapper;

    public CatsController(ICatsService catsService, IMapper mapper)
    {
        _catsService = catsService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<ActionResult<CatResponse>> Create(CatRequest request)
    {
        var created = await _catsService.CreateAsync(_mapper.Map<Cat>(request));
        var response = _mapper.Map<CatResponse>(created);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<List<CatResponse>>> GetAll()
    {
        var cats = await _catsService.GetAllAsync();
        return Ok(_mapper.Map<List<CatResponse>>(cats));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CatResponse>> GetById(Guid id)
    {
        var cat = await _catsService.GetByIdAsync(id);
        return cat is null ? NotFound() : Ok(_mapper.Map<CatResponse>(cat));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CatResponse>> Update(Guid id, CatRequest request)
    {
        var updated = await _catsService.UpdateAsync(id, _mapper.Map<Cat>(request));
        return updated is null ? NotFound() : Ok(_mapper.Map<CatResponse>(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _catsService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
