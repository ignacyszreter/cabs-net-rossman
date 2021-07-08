using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public interface IDriverService
{
  Task<Driver> CreateDriver(string license, string lastName, string firstName, Driver.Types type,
    Driver.Statuses status);

  Task ChangeLicenseNumber(string newLicense, long? driverId);
  Task ChangeDriverStatus(long? driverId, Driver.Statuses status);
  Task<DriverDto> LoadDriver(long? driverId);
}