using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Service;
using Microsoft.AspNetCore.Mvc;

namespace LegacyFighter.Cabs.Controllers;

[ApiController]
[Route("[controller]")]
public class AwardsAccountController
{
  private readonly IAwardsService _awardsService;

  public AwardsAccountController(IAwardsService awardsService)
  {
    _awardsService = awardsService;
  }

  [HttpPost("/clients/{clientId}/awards")]
  public async Task<IActionResult> Register(long? clientId)
  {
    await _awardsService.RegisterToProgram(clientId);
    return new OkObjectResult(await _awardsService.FindBy(clientId));
  }

  [HttpGet("/clients/{clientId}/awards/balance")]
  public async Task<int> CalculateBalance(long? clientId)
  {
    return await _awardsService.CalculateBalance(clientId);
  }

  [HttpGet("/clients/{clientId}/awards/")]
  public async Task<AwardsAccountDto> FindBy(long? clientId)
  {
    return await _awardsService.FindBy(clientId);
  }
}