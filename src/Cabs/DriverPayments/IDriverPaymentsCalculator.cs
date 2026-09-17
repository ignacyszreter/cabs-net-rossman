using LegacyFighter.Cabs.Service;
using LegacyFighter.Cabs.MoneyValue;

namespace LegacyFighter.Cabs.DriverPayments;

public interface IDriverPaymentsCalculator
{
  Task<Dictionary<Month, Money>> YearlyPayments(long? driverId, int year);
}
