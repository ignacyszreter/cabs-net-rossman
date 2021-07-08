using System.Linq;
using System.Text.RegularExpressions;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;

namespace LegacyFighter.Cabs.Service;

public class DriverService : IDriverService
{
  public const string DriverLicenseRegex = "^[A-Z9]{5}\\d{6}[A-Z9]{2}\\d[A-Z]{2}$";

  private readonly IDriverRepository _driverRepository;

  public DriverService(IDriverRepository driverRepository)
  {
    _driverRepository = driverRepository;
  }

  public async Task<Driver> CreateDriver(string license, string lastName, string firstName, Driver.Types type,
    Driver.Statuses status)
  {
    var driver = new Driver();
    if (status == Driver.Statuses.Active)
    {
      if (license == null || !license.Any() || !Regex.IsMatch(license, DriverLicenseRegex))
      {
        throw new ArgumentException("Illegal license no = " + license);
      }
    }

    driver.DriverLicense = license;
    driver.LastName = lastName;
    driver.FirstName = firstName;
    driver.Status = status;
    driver.Type = type;
    return await _driverRepository.Save(driver);
  }

  public async Task ChangeLicenseNumber(string newLicense, long? driverId)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    if (newLicense == null || !newLicense.Any() || !Regex.IsMatch(newLicense, DriverLicenseRegex))
    {
      throw new ArgumentException("Illegal new license no = " + newLicense);
    }

    driver.DriverLicense = newLicense;


  }


  public async Task ChangeDriverStatus(long? driverId, Driver.Statuses status)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    if (status == Driver.Statuses.Active)
    {
      var license = driver.DriverLicense;
      if (license == null || !license.Any() || !Regex.IsMatch(license, DriverLicenseRegex))
      {
        throw new InvalidOperationException("Status cannot be ACTIVE. Illegal license no = " + license);
      }
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