using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;
using NodaTime;

namespace LegacyFighter.Cabs.Service;

public class DriverSessionService : IDriverSessionService
{
  private readonly IDriverRepository _driverRepository;
  private readonly IDriverSessionRepository _driverSessionRepository;
  private readonly ICarTypeService _carTypeService;

  public DriverSessionService(IDriverRepository driverRepository, IDriverSessionRepository driverSessionRepository, ICarTypeService carTypeService)
  {
    _driverRepository = driverRepository;
    _driverSessionRepository = driverSessionRepository;
    _carTypeService = carTypeService;
  }

  public async Task<DriverSession> LogIn(long? driverId, string plateNumber, CarType.CarClasses? carClass, string carBrand)
  {
    var session = new DriverSession
    {
      Driver = await _driverRepository.Find(driverId),
      LoggedAt = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime()),
      CarClass = carClass,
      PlatesNumber = plateNumber,
      CarBrand = carBrand
    };
    await _carTypeService.RegisterActiveCar(session.CarClass);
    return await _driverSessionRepository.Save(session);
  }

  public async Task LogOut(long sessionId)
  {
    var session = await _driverSessionRepository.Find(sessionId);
    if (session == null)
    {
      throw new ArgumentException("Session does not exist");
    }

    await _carTypeService.UnregisterCar(session.CarClass);
    session.LoggedOutAt = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime());
  }

  public async Task LogOutCurrentSession(long? driverId)
  {
    var session =
      await _driverSessionRepository.FindTopByDriverAndLoggedOutAtIsNullOrderByLoggedAtDesc(
        await _driverRepository.Find(driverId));
    if (session != null)
    {
      session.LoggedOutAt = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime());
      await _carTypeService.UnregisterCar(session.CarClass);
    }
  }

  public async Task<List<DriverSession>> FindByDriver(long? driverId)
  {
    return await _driverSessionRepository.FindByDriver(await _driverRepository.Find(driverId));
  }
}