namespace LegacyFighter.Cabs.DriverSettlements;

public interface IPublicHolidays
{
  Task<IReadOnlySet<DateOnly>> In(int year);
}
