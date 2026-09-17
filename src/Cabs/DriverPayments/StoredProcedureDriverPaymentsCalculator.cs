using System.Data;
using LegacyFighter.Cabs.MoneyValue;
using LegacyFighter.Cabs.Repository;
using LegacyFighter.Cabs.Service;
using Microsoft.EntityFrameworkCore;

namespace LegacyFighter.Cabs.DriverPayments;

public class StoredProcedureDriverPaymentsCalculator : IDriverPaymentsCalculator
{
  private readonly IDriverRepository _driverRepository;
  private readonly SqLiteDbContext _dbContext;

  public StoredProcedureDriverPaymentsCalculator(
    IDriverRepository driverRepository,
    SqLiteDbContext dbContext)
  {
    _driverRepository = driverRepository;
    _dbContext = dbContext;
  }

  public async Task<Dictionary<Month, Money>> YearlyPayments(long? driverId, int year)
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

    return payments;
  }
}
