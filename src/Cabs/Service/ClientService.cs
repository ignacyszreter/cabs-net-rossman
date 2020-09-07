using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;

namespace LegacyFighter.Cabs.Service;

public class ClientService : IClientService
{
  private readonly IClientRepository _clientRepository;

  public ClientService(IClientRepository clientRepository)
  {
    _clientRepository = clientRepository;
  }

  public async Task<Client> RegisterClient(string name, string lastName)
  {
    var client = new Client();
    client.Name = name;
    client.LastName = lastName;
    return await _clientRepository.Save(client);
  }

  public async Task<ClientDto> Load(long? id)
  {
    return new ClientDto(await _clientRepository.Find(id));
  }
}