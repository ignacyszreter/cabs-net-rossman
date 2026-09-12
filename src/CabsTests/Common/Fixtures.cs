using System;
using System.Threading.Tasks;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;

namespace LegacyFighter.CabsTests.Common;

internal class Fixtures
{
  private readonly CabsApi _api;
  private readonly FakeClock _clock;
  private readonly IServiceProvider _services;

  public Fixtures(CabsApi api, FakeClock clock, IServiceProvider services)
  {
    _api = api;
    _clock = clock;
    _services = services;
  }

  public AddressDto AnAddress(string street, int buildingNumber)
  {
    return new AddressDto("Polska", "Warszawa", street, buildingNumber);
  }

  public (AddressDto From, AddressDto To) AddressesOf42KmDistance()
  {
    return (AnAddress("Młynarska", 20), AnAddress("Żytnia", 25));
  }

  public Task<long> AClient()
  {
    return _api.RegisterClient("Janusz", "Kowalski");
  }

  public async Task<long> ADriverNearby(string plateNumber)
  {
    var driver = await _api.RegisterDriver("FARME100165AB5EW", "Janusz", "Kowalski");
    await _api.ActivateDriver(driver);
    await _api.LogInDriver(driver, plateNumber, CarType.CarClasses.Van, "BRAND");
    await _api.RegisterDriverPosition(driver, 1, 1);
    return driver;
  }

  public async Task DriverHasFlatFee(long driverId, int amount)
  {
    using var scope = _services.CreateScope();
    var driver = await scope.ServiceProvider.GetRequiredService<IDriverRepository>().Find(driverId);
    await scope.ServiceProvider.GetRequiredService<IDriverFeeRepository>()
      .Save(new DriverFee(DriverFee.FeeTypes.Flat, driver, amount, 0));
  }

  public async Task AnActiveCarCategory(CarType.CarClasses carClass)
  {
    var (carType, minNoOfCars) = await _api.RegisterCarType(carClass, "opis");
    for (var car = 0; car < minNoOfCars; car++)
    {
      await _api.RegisterCar(carClass);
    }

    await _api.ActivateCarType(carType);
  }

  public async Task<long> ADraftTransitNow(AddressDto from, AddressDto to)
  {
    var transit = await _api.OrderTransit(await AClient(), from, to);
    return transit.Id!.Value;
  }

  public async Task<long> ADraftTransitAt(Instant when, AddressDto from, AddressDto to)
  {
    _clock.Reset(when);
    return await ADraftTransitNow(from, to);
  }
}
