using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Service;
using Microsoft.AspNetCore.Mvc;

namespace LegacyFighter.Cabs.Controllers;

[ApiController]
[Route("[controller]")]
public class TransitController
{
  private readonly ITransitService _transitService;

  public TransitController(ITransitService transitService)
  {
    _transitService = transitService;
  }

  [HttpGet("/transits/{id}")]
  public async Task<TransitDto> GetTransit(long? id)
  {
    return await _transitService.LoadTransit(id);
  }

  [HttpPost("/transits/")]
  public async Task<TransitDto> CreateTransit([FromBody] TransitDto transitDto)
  {
    var transit = await _transitService.CreateTransit(transitDto);
    return await _transitService.LoadTransit(transit.Id);
  }
}