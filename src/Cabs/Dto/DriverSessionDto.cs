using LegacyFighter.Cabs.Entity;
using NodaTime;

namespace LegacyFighter.Cabs.Dto;

public class DriverSessionDto
{
  public DriverSessionDto()
  {

  }

  public DriverSessionDto(DriverSession session)
  {
    PlatesNumber = session.PlatesNumber;
    LoggedAt = session.LoggedAt;
    LoggedOutAt = session.LoggedOutAt;
  }

  public Instant LoggedAt { get; set; }
  public Instant? LoggedOutAt { get; set; }
  public string PlatesNumber { get; set; }
}