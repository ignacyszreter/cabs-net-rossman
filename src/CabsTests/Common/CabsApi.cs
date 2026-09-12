using System.Threading.Tasks;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;

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

  public async Task LogInDriver(long driverId, string plateNumber, CarType.CarClasses carClass, string carBrand)
  {
    await _app.DriverSessionService.LogIn(driverId, plateNumber, carClass, carBrand);
  }

  public async Task RegisterDriverPosition(long driverId, double latitude, double longitude)
  {
    await _app.DriverTrackingService.RegisterPosition(driverId, latitude, longitude);
  }

  public async Task<(long Id, int MinNoOfCars)> RegisterCarType(CarType.CarClasses carClass, string description)
  {
    var carType = await _app.CarTypeService.Create(
      new CarTypeDto { CarClass = carClass, Description = description });
    return (carType.Id!.Value, carType.MinNoOfCarsToActivateClass);
  }

  public async Task RegisterCar(CarType.CarClasses carClass)
  {
    await _app.CarTypeService.RegisterCar(carClass);
  }

  public async Task ActivateCarType(long carTypeId)
  {
    await _app.CarTypeService.Activate(carTypeId);
  }

  public async Task<TransitDto> OrderTransit(long clientId, AddressDto from, AddressDto to)
  {
    var transit = await _app.TransitService.CreateTransit(new TransitDto
    {
      ClientDto = new ClientDto { Id = clientId },
      From = from,
      To = to
    });
    return await FindTransit(transit.Id);
  }

  public async Task<TransitDto> PublishTransit(long? transitId)
  {
    await _app.TransitService.PublishTransit(transitId);
    return await FindTransit(transitId);
  }

  public async Task<TransitDto> AcceptTransit(long? transitId, long driverId)
  {
    await _app.TransitService.AcceptTransit(driverId, transitId);
    return await FindTransit(transitId);
  }

  public async Task<TransitDto> StartTransit(long? transitId, long driverId)
  {
    await _app.TransitService.StartTransit(driverId, transitId);
    return await FindTransit(transitId);
  }

  public async Task<TransitDto> CompleteTransit(long? transitId, long driverId, AddressDto destination)
  {
    await _app.TransitService.CompleteTransit(driverId, transitId, destination);
    return await FindTransit(transitId);
  }

  public async Task<TransitDto> FindTransit(long? transitId)
  {
    return await _app.TransitService.LoadTransit(transitId);
  }
}
