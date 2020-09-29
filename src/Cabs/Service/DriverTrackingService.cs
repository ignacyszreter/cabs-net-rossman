using System.Linq;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;
using NodaTime;

namespace LegacyFighter.Cabs.Service;

public class DriverTrackingService : IDriverTrackingService
{
  private readonly IDriverPositionRepository _positionRepository;
  private readonly IDriverRepository _driverRepository;
  private IClock _clock;

  public DriverTrackingService(IDriverPositionRepository positionRepository, IDriverRepository driverRepository, IClock clock)
  {
    _positionRepository = positionRepository;
    _driverRepository = driverRepository;
    _clock = clock;
  }

  public async Task<DriverPosition> RegisterPosition(long? driverId, double latitude, double longitude)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    if (driver.Status != Driver.Statuses.Active)
    {
      throw new InvalidOperationException("Driver is not active, cannot register position, id = " + driverId);
    }

    var position = new DriverPosition
    {
      Driver = driver,
      SeenAt = SystemClock.Instance.GetCurrentInstant(),
      Latitude = latitude,
      Longitude = longitude
    };
    return await _positionRepository.Save(position);
  }
}