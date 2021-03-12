using System.Linq;
using LegacyFighter.Cabs.Entity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LegacyFighter.Cabs.Repository;

public interface IDriverSessionRepository
{

  Task<List<DriverSession>> FindAllByLoggedOutAtNullAndDriverInAndCarClassIn(ICollection<Driver> drivers,
    List<CarType.CarClasses?> carClasses);

  Task<List<DriverSession>> FindByDriver(Driver driver);
  Task<DriverSession> Save(DriverSession session);
  Task<DriverSession> Find(long sessionId);
}

internal class EfCoreDriverSessionRepository : IDriverSessionRepository
{
  private readonly SqLiteDbContext _context;

  public EfCoreDriverSessionRepository(SqLiteDbContext context)
  {
    _context = context;
  }

  public async Task<List<DriverSession>> FindAllByLoggedOutAtNullAndDriverInAndCarClassIn(ICollection<Driver> drivers,
    List<CarType.CarClasses?> carClasses)
  {
    var driverSessions = await _context.DriverSessions.Where(d =>
      d.LoggedOutAt == null && drivers.Contains(d.Driver) && carClasses.Contains(d.CarClass))
      .ToListAsync();
    return driverSessions;
  }

  public async Task<List<DriverSession>> FindByDriver(Driver driver)
  {
    return await _context.DriverSessions.Where(session => session.Driver == driver).ToListAsync();
  }

  public async Task<DriverSession> Save(DriverSession session)
  {
    _context.DriverSessions.Update(session);
    await _context.SaveChangesAsync();
    return session;
  }

  public async Task<DriverSession> Find(long sessionId)
  {
    return await _context.DriverSessions.FindAsync(sessionId);
  }
}