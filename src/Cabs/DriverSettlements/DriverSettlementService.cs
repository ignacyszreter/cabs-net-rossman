using System.Data;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.MoneyValue;
using LegacyFighter.Cabs.Repository;
using LegacyFighter.Cabs.Service;
using Microsoft.EntityFrameworkCore;

namespace LegacyFighter.Cabs.DriverSettlements;

public class DriverSettlementService : IDriverSettlement
{
  private readonly IDriverRepository _driverRepository;
  private readonly SqLiteDbContext _dbContext;
  private readonly IExchangeRates _exchangeRates;
  private readonly IPublicHolidays _publicHolidays;

  public DriverSettlementService(
    IDriverRepository driverRepository,
    SqLiteDbContext dbContext,
    IExchangeRates exchangeRates,
    IPublicHolidays publicHolidays)
  {
    _driverRepository = driverRepository;
    _dbContext = dbContext;
    _exchangeRates = exchangeRates;
    _publicHolidays = publicHolidays;
  }

  public async Task<DriverSettlementDto> Settle(long driverId, int year)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException($"Driver does not exists, id = {driverId}");
    }

    var payments = Month.Values().ToDictionary(m => m, _ => Money.Zero);
    var connection = _dbContext.Database.GetDbConnection();
    var wasClosed = connection.State == ConnectionState.Closed;
    if (wasClosed)
    {
      await connection.OpenAsync();
    }

    try
    {
      await using var command = _dbContext.Database.CreateCommand();
      command.CommandText = "dbo.CalculateDriverMonthlyPayments";
      command.CommandType = CommandType.StoredProcedure;
      command.AddParameter("@DriverId", driverId);
      command.AddParameter("@Year", year);
      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        payments[new Month(reader.GetInt32(0))] = new Money(reader.GetInt32(1));
      }
    }
    finally
    {
      if (wasClosed)
      {
        await connection.CloseAsync();
      }
    }

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
