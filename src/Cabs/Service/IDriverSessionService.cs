using System.Linq;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public interface IDriverSessionService
{
  Task<DriverSession> LogIn(long? driverId, string plateNumber);
  Task LogOut(long sessionId);
  Task<List<DriverSession>> FindByDriver(long? driverId);
}