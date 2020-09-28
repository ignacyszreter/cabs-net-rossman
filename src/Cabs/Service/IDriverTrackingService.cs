using System.Linq;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public interface IDriverTrackingService
{
  Task<DriverPosition> RegisterPosition(long? driverId, double latitude, double longitude);
}