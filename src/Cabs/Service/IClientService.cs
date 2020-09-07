using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public interface IClientService
{
  Task<Client> RegisterClient(string name, string lastName);
  Task<ClientDto> Load(long? id);
}