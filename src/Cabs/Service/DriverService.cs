using System.Data;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.MoneyValue;
using LegacyFighter.Cabs.Repository;
using Microsoft.EntityFrameworkCore;

namespace LegacyFighter.Cabs.Service;

public class DriverService : IDriverService
{
  public const string DriverLicenseRegex = "^[A-Z9]{5}\\d{6}[A-Z9]{2}\\d[A-Z]{2}$";

  private readonly IDriverRepository _driverRepository;
  private readonly IDriverAttributeRepository _driverAttributeRepository;
  private readonly SqLiteDbContext _dbContext;

  public DriverService(
    IDriverRepository driverRepository,
    IDriverAttributeRepository driverAttributeRepository,
    SqLiteDbContext dbContext)
  {
    _driverRepository = driverRepository;
    _driverAttributeRepository = driverAttributeRepository;
    _dbContext = dbContext;
  }

  public async Task<Driver> CreateDriver(string license, string lastName, string firstName, Driver.Types type,
    Driver.Statuses status, string photo)
  {
    var driver = new Driver();
    if (status == Driver.Statuses.Active)
    {
      driver.DriverLicense = DriverLicense.WithLicense(license);
    }
    else
    {
      driver.DriverLicense = DriverLicense.WithoutValidation(license);
    }

    driver.LastName = lastName;
    driver.FirstName = firstName;
    driver.Status = status;
    driver.Type = type;
    if (photo != null && photo.Any())
    {
      if (photo.IsBase64())
      {
        driver.Photo = photo;
      }
      else
      {
        throw new ArgumentException("Illegal photo in base64");
      }
    }

    return await _driverRepository.Save(driver);
  }

  public async Task ChangeLicenseNumber(string newLicense, long? driverId)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    driver.DriverLicense = DriverLicense.WithLicense(newLicense);

    if (driver.Status != Driver.Statuses.Active)
    {
      throw new InvalidOperationException("Driver is not active, cannot change license");
    }
  }


  public async Task ChangeDriverStatus(long? driverId, Driver.Statuses status)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    if (status == Driver.Statuses.Active)
    {
      try
      {
        driver.DriverLicense = DriverLicense.WithLicense(driver.DriverLicense.ValueAsString);
      }
      catch (ArgumentException e)
      {
        throw new InvalidOperationException(e.Message, e);
      }
    }


    driver.Status = status;
  }

  public async Task ChangePhoto(long driverId, string photo)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    if (photo != null && photo.Any())
    {
      if (photo.IsBase64())
      {
        driver.Photo = photo;
      }
      else
      {
        throw new ArgumentException("Illegal photo in base64");
      }
    }

    driver.Photo = photo;
    await _driverRepository.Save(driver);
  }

  public async Task<Money> CalculateDriverMonthlyPayment(long? driverId, int year, int month) 
  {
    var payments = await CalculateDriverYearlyPayment(driverId, year);
    return payments[new Month(month)];
  }

  public async Task<Dictionary<Month, Money>> CalculateDriverYearlyPayment(long? driverId, int year)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
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

  public async Task<DriverDto> LoadDriver(long? driverId)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    return new DriverDto(driver);
  }

  public async Task AddAttribute(long driverId, DriverAttribute.DriverAttributeNames attr, string value)
  {
    var driver = await _driverRepository.Find(driverId);
    if (driver == null)
    {
      throw new ArgumentException("Driver does not exists, id = " + driverId);
    }

    await _driverAttributeRepository.Save(new DriverAttribute(driver, attr, value));

  }
}