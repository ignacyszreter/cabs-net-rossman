using LegacyFighter.Cabs.Entity;
using NodaTime;

namespace LegacyFighter.Cabs.Dto;

public class TransitDto
{
  public DriverDto Driver;

  private decimal _baseFee;

  public TransitDto()
  {

  }

  public TransitDto(Transit transit)
  {
    Id = transit.Id;
    if (transit.Price != null)
    {
      Price = new decimal(transit.Price.Value);
    }

    Status = transit.Status;
    foreach (var d in transit.ProposedDrivers) 
    {
      ProposedDrivers.Add(new DriverDto(d));
    }
    To = new AddressDto(transit.To);
    From = new AddressDto(transit.From);
    ClientDto = new ClientDto(transit.Client);
    DateTime = transit.DateTime;
    Published = transit.Published;
    CompleteAt = transit.CompleteAt;

  }

  public List<DriverDto> ProposedDrivers { get; set; } = new();
  public AddressDto To { get; set; }
  public AddressDto From { get; set; }
  public ClientDto ClientDto { get; set; }
  public long? Id { get; }
  public Transit.Statuses? Status { get; set; }
  public decimal? Price { get; }
  public Instant? DateTime { get; set; }
  public Instant? Published { get; set; }
  public Instant? CompleteAt { get; set; }
}