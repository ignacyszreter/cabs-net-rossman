using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Service;
using Microsoft.AspNetCore.Mvc;

namespace LegacyFighter.Cabs.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientController
{
  internal IClientService ClientService;

  public ClientController(IClientService clientService)
  {
    ClientService = clientService;
  }

  [HttpPost("/clients")]
  public async Task<ClientDto> Register([FromBody] ClientDto dto)
  {
    var c = await ClientService.RegisterClient(dto.Name, dto.LastName);
    return await ClientService.Load(c.Id);
  }

  [HttpGet("/clients/{clientId}")]
  public async Task<ClientDto> Find(long? clientId)
  {
    return await ClientService.Load(clientId);
  }
}