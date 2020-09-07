using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;

namespace LegacyFighter.Cabs.Service;

public class DriverService : IDriverService
{
  private readonly IDriverRepository _driverRepository;

  public DriverService(IDriverRepository driverRepository)
  {
    _driverRepository = driverRepository;
  }

  public async Task<Driver> CreateDriver(string license, string lastName, string firstName, Driver.Types type,
    Driver.Statuses status)
  {
    var driver = new Driver();
    driver.DriverLicense = license;
    driver.LastName = lastName;
    driver.FirstName = firstName;
    driver.Status = status;
    driver.Type = type;
    return await _driverRepository.Save(driver);
  }


  public async Task ChangeDriverStatus(long? driverId, Driver.Statuses status)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    driver.Status = status;
  }

  public async Task<DriverDto> LoadDriver(long? driverId)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    return new DriverDto(driver);
  }
}