using LegacyFighter.Cabs.DriverSettlements;
using LegacyFighter.Cabs.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LegacyFighter.Cabs.Controllers;

[ApiController]
[Route("[controller]")]
public class DriverSettlementController
{
  private readonly IDriverSettlement _driverSettlement;

  public DriverSettlementController(IDriverSettlement driverSettlement)
  {
    _driverSettlement = driverSettlement;
  }

  [HttpGet("/drivers/{driverId}/settlements/{year}")]
  public async Task<DriverSettlementDto> Settle(long driverId, int year)
  {
    return await _driverSettlement.Settle(driverId, year);
  }
}
