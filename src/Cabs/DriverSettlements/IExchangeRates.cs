namespace LegacyFighter.Cabs.DriverSettlements;

public interface IExchangeRates
{
  Task<decimal> EurRateAtEndOf(int year);
}
