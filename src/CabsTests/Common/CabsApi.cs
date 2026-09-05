using System;
using System.Threading.Tasks;
using LegacyFighter.Cabs.CarFleet;
using LegacyFighter.Cabs.Crm;
using LegacyFighter.Cabs.DriverFleet;
using LegacyFighter.Cabs.Geolocation.Address;
using LegacyFighter.Cabs.Ride;

namespace LegacyFighter.CabsTests.Common;

internal class CabsApi
{
  private readonly CabsApp _app;

  public CabsApi(CabsApp app)
  {
    _app = app;
  }

  public async Task<long> RegisterClient(string name, string lastName)
  {
    var client = await _app.ClientService.RegisterClient(
      name, lastName, Client.Types.Normal, Client.PaymentTypes.PostPaid);
    return client.Id!.Value;
  }

  public async Task<long> RegisterDriver(string license, string firstName, string lastName)
  {
    var driver = await _app.DriverService.CreateDriver(
      license, lastName, firstName, Driver.Types.Candidate, Driver.Statuses.Inactive, null);
    return driver.Id!.Value;
  }

  public async Task ActivateDriver(long driverId)
  {
    await _app.DriverService.ChangeDriverStatus(driverId, Driver.Statuses.Active);
  }

  public async Task LogInDriver(long driverId, string plateNumber, CarClasses carClass, string carBrand)
  {
    await _app.DriverSessionService.LogIn(driverId, plateNumber, carClass, carBrand);
  }

  public async Task RegisterDriverPosition(long driverId, double latitude, double longitude)
  {
    await _app.DriverTrackingService.RegisterPosition(driverId, latitude, longitude, _app.Clock.GetCurrentInstant());
  }

  public async Task<(long Id, int MinNoOfCars)> RegisterCarType(CarClasses carClass, string description)
  {
    var carType = await _app.CarTypeService.Create(
      new CarTypeDto { CarClass = carClass, Description = description });
    return (carType.Id!.Value, carType.MinNoOfCarsToActivateClass);
  }

  public async Task RegisterCar(CarClasses carClass)
  {
    await _app.CarTypeService.RegisterCar(carClass);
  }

  public async Task ActivateCarType(long carTypeId)
  {
    await _app.CarTypeService.Activate(carTypeId);
  }

  public async Task<TransitDto> OrderTransit(long clientId, AddressDto from, AddressDto to)
  {
    var transit = await _app.RideService.CreateTransit(new TransitDto
    {
      ClientDto = new ClientDto { Id = clientId },
      From = from,
      To = to
    });
    return await FindTransit(transit.RequestId);
  }

  public async Task<TransitDto> PublishTransit(Guid requestId)
  {
    await _app.RideService.PublishTransit(requestId);
    return await FindTransit(requestId);
  }

  public async Task<TransitDto> AcceptTransit(Guid requestId, long driverId)
  {
    await _app.RideService.AcceptTransit(driverId, requestId);
    return await FindTransit(requestId);
  }

  public async Task<TransitDto> StartTransit(Guid requestId, long driverId)
  {
    await _app.RideService.StartTransit(driverId, requestId);
    return await FindTransit(requestId);
  }

  public async Task<TransitDto> CompleteTransit(Guid requestId, long driverId, AddressDto destination)
  {
    await _app.RideService.CompleteTransit(driverId, requestId, destination);
    return await FindTransit(requestId);
  }

  public async Task<TransitDto> FindTransit(Guid requestId)
  {
    return await _app.RideService.LoadTransit(requestId);
  }
}
