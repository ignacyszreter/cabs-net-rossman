using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Dto;

public class ClientDto
{
  public ClientDto()
  {

  }

  public ClientDto(Client client)
  {
    Id = client.Id;
    Name = client.Name;
    LastName = client.LastName;
  }

  public string Name { get; set; }
  public string LastName { get; set; }
  public long? Id { get; set; }
}