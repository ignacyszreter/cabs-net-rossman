using LegacyFighter.Cabs.DriverPayments;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.MoneyValue;

namespace LegacyFighter.Cabs.DriverSettlements;

public class DriverSettlementService : IDriverSettlement
{
  private readonly IDriverPaymentsCalculator _paymentsCalculator;
  private readonly IExchangeRates _exchangeRates;
  private readonly IPublicHolidays _publicHolidays;

  public DriverSettlementService(
    IDriverPaymentsCalculator paymentsCalculator,
    IExchangeRates exchangeRates,
    IPublicHolidays publicHolidays)
  {
    _paymentsCalculator = paymentsCalculator;
    _exchangeRates = exchangeRates;
    _publicHolidays = publicHolidays;
  }

  public async Task<DriverSettlementDto> Settle(long driverId, int year)
  {
    var payments = await _paymentsCalculator.YearlyPayments(driverId, year);
    var total = payments.Values.Aggregate(Money.Zero, (sum, payment) => sum + payment);

    var eurRate = await _exchangeRates.EurRateAtEndOf(year);
    var totalInEur = Math.Round(total.IntValue / eurRate, 2);

    var holidayDates = await _publicHolidays.In(year + 1);
    var payoutDate = new DateOnly(year + 1, 1, 10);
    while (payoutDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidayDates.Contains(payoutDate))
    {
      payoutDate = payoutDate.AddDays(1);
    }

    return new DriverSettlementDto(
      driverId,
      year,
      payments.ToDictionary(p => p.Key.Value, p => p.Value.IntValue),
      total.IntValue,
      eurRate,
      totalInEur,
      payoutDate);
  }
}
