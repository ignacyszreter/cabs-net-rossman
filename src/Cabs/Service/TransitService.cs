using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;
using NodaTime;

namespace LegacyFighter.Cabs.Service;

public class TransitService : ITransitService
{
  private readonly IDriverRepository _driverRepository;
  private readonly ITransitRepository _transitRepository;
  private readonly IClientRepository _clientRepository;
  private readonly DistanceCalculator _distanceCalculator;
  private readonly IDriverPositionRepository _driverPositionRepository;
  private readonly IDriverSessionRepository _driverSessionRepository;
  private readonly IGeocodingService _geocodingService;
  private readonly AddressRepository _addressRepository;
  private readonly IClock _clock;

  public TransitService(
    IDriverRepository driverRepository,
    ITransitRepository transitRepository,
    IClientRepository clientRepository,
    DistanceCalculator distanceCalculator,
    IDriverPositionRepository driverPositionRepository,
    IDriverSessionRepository driverSessionRepository,
    IGeocodingService geocodingService,
    AddressRepository addressRepository,
    IClock clock)
  {
    _driverRepository = driverRepository;
    _transitRepository = transitRepository;
    _clientRepository = clientRepository;
    _distanceCalculator = distanceCalculator;
    _driverPositionRepository = driverPositionRepository;
    _driverSessionRepository = driverSessionRepository;
    _geocodingService = geocodingService;
    _addressRepository = addressRepository;
    _clock = clock;
  }

  public async Task<Transit> CreateTransit(TransitDto transitDto)
  {
    var from = await AddressFromDto(transitDto.From);
    var to = await AddressFromDto(transitDto.To);
    return await CreateTransit(transitDto.ClientDto.Id, from, to);
  }

  private async Task<Address> AddressFromDto(AddressDto addressDto)
  {
    var address = addressDto.ToAddressEntity();
    return await _addressRepository.Save(address);
  }

  public async Task<Transit> CreateTransit(long? clientId, Address from, Address to)
  {
    var client = await _clientRepository.Find(clientId);

    if (client == null)
    {
      throw new ArgumentException("Client does not exist, id = " + clientId);
    }

    var transit = new Transit();

    // TODO FIXME later: add some exceptions handling
    var geoFrom = _geocodingService.GeocodeAddress(from);
    var geoTo = _geocodingService.GeocodeAddress(to);

    transit.Client = client;
    transit.From = @from;
    transit.To = to;
    transit.Status = Transit.Statuses.Draft;
    transit.DateTime = _clock.GetCurrentInstant();
    transit.Km = (float)_distanceCalculator.CalculateByMap(geoFrom[0], geoFrom[1], geoTo[0], geoTo[1]);

    return await _transitRepository.Save(transit);
  }

  public async Task CancelTransit(long? transitId)
  {
    var transit = await _transitRepository.Find(transitId);

    if (transit == null)
    {
      throw new ArgumentException("Transit does not exist, id = " + transitId);
    }

    transit.Status = Transit.Statuses.Cancelled;
    transit.Driver = null;
    transit.Km = 0;
    transit.AwaitingDriversResponses = 0;
    await _transitRepository.Save(transit);
  }

  public async Task<Transit> PublishTransit(long? transitId)
  {
    var transit = await _transitRepository.Find(transitId);

    if (transit == null)
    {
      throw new ArgumentException("Transit does not exist, id = " + transitId);
    }

    transit.Status = Transit.Statuses.WaitingForDriverAssignment;
    transit.Published = _clock.GetCurrentInstant();
    await _transitRepository.Save(transit);

    return await FindDriversForTransit(transitId);
  }

  // Abandon hope all ye who enter here...
  public async Task<Transit> FindDriversForTransit(long? transitId)
  {
    var transit = await _transitRepository.Find(transitId);

    if (transit != null)
    {
      if (transit.Status == Transit.Statuses.WaitingForDriverAssignment)
      {



        var distanceToCheck = 0;

        while (true)
        {
          if (transit.AwaitingDriversResponses
              > 4)
          {
            return transit;
          }

          distanceToCheck++;

          // TODO FIXME: to refactor when the final business logic will be determined
          if (transit.Published.Value.Plus(Duration.FromSeconds(120)) < _clock.GetCurrentInstant()
              ||
              (distanceToCheck >= 10)
              ||
              // Should it be here? How is it even possible due to previous status check above loop?
              (transit.Status == Transit.Statuses.Cancelled)
          )
          {
            transit.Status = Transit.Statuses.DriverAssignmentFailed;
            transit.Driver = null;
            transit.Km = 0;
            transit.AwaitingDriversResponses = 0;
            await _transitRepository.Save(transit);
            return transit;
          }

          var geocoded = _geocodingService.GeocodeAddress(transit.From);

          var longitude = geocoded[1];
          var latitude = geocoded[0];

          //https://gis.stackexchange.com/questions/2951/algorithm-for-offsetting-a-latitude-longitude-by-some-amount-of-meters
          //Earth’s radius, sphere
          //double R = 6378;
          double r = 6371; // Changed to 6371 due to Copy&Paste pattern from different source

          //offsets in meters
          double dn = distanceToCheck;
          double de = distanceToCheck;

          //Coordinate offsets in radians
          var dLat = dn / r;
          var dLon = de / (r * Math.Cos(Math.PI * latitude / 180));

          //Offset positions, decimal degrees
          var latitudeMin = latitude - dLat * 180 / Math.PI;
          var latitudeMax = latitude + dLat *
            180 / Math.PI;
          var longitudeMin = longitude - dLon *
            180 / Math.PI;
          var longitudeMax = longitude + dLon * 180 / Math.PI;

          var driversAvgPositions = await _driverPositionRepository
            .FindAverageDriverPositionSince(latitudeMin, latitudeMax, longitudeMin, longitudeMax,
              _clock.GetCurrentInstant().Minus(Duration.FromMinutes(5)));

          if (driversAvgPositions.Any())
          {
            driversAvgPositions.Sort((d1, d2) => 
                Math.Sqrt(Math.Pow(latitude - d1.Latitude, 2) + Math.Pow(longitude - d1.Longitude, 2)).CompareTo(
              Math.Sqrt(Math.Pow(latitude - d2.Latitude, 2) + Math.Pow(longitude - d2.Longitude, 2))
              ));
            driversAvgPositions = driversAvgPositions.Take(20).ToList();

            var drivers = driversAvgPositions.Select(p => p.Driver).ToList();

            var activeDriverIdsInSpecificCar = (await _driverSessionRepository
              .FindAllByLoggedOutAtNullAndDriverIn(drivers))

              .Select(ds => ds.Driver.Id).ToList();

            driversAvgPositions = driversAvgPositions
              .Where(dp=>activeDriverIdsInSpecificCar.Contains(dp.Driver.Id)).ToList();

            // Iterate across average driver positions
            foreach (var driverAvgPosition in driversAvgPositions) 
            {
              var driver = driverAvgPosition.Driver;
              if (driver.Status == Driver.Statuses.Active)
              {
                if (!transit.DriversRejections.Contains(driver))
                {
                  transit.ProposedDrivers.Add(driver);
                  transit.AwaitingDriversResponses = transit.AwaitingDriversResponses + 1;
                }
              }
              else
              {
                // Not implemented yet!
              }
            }

            await _transitRepository.Save(transit);

          }
          else
          {
            // Next iteration, no drivers at specified area
            continue;
          }
        }
      }
      else
      {
        throw new InvalidOperationException("..., id = " + transitId);
      }
    }
    else
    {
      throw new ArgumentException("Transit does not exist, id = " + transitId);
    }

  }

  public async Task AcceptTransit(long? driverId, long? transitId)
  {
    var driver = await _driverRepository.Find(driverId);

    if (driver == null)
    {
      throw new ArgumentException("Driver does not exist, id = " + driverId);
    }
    else
    {
      var transit = await _transitRepository.Find(transitId);

      if (transit == null)
      {
        throw new ArgumentException("Transit does not exist, id = " + transitId);
      }
      else
      {
        if (transit.Driver != null)
        {
          throw new InvalidOperationException("Transit already accepted, id = " + transitId);
        }
        else
        {
          if (!transit.ProposedDrivers.Contains(driver))
          {
            throw new InvalidOperationException("Driver out of possible drivers, id = " + transitId);
          }
          else
          {
            transit.Driver = driver;
            transit.AwaitingDriversResponses = 0;
            transit.AcceptedAt = _clock.GetCurrentInstant();
            transit.Status = Transit.Statuses.TransitToPassenger;
            await _transitRepository.Save(transit);
          }
        }
      }
    }
  }

  public async Task StartTransit(long? driverId, long? transitId)
  {
    var driver = _driverRepository.Find(driverId);

    if (driver == null)
    {
      throw new ArgumentException("Driver does not exist, id = " + driverId);
    }

    var transit = await _transitRepository.Find(transitId);

    if (transit == null)
    {
      throw new ArgumentException("Transit does not exist, id = " + transitId);
    }

    if (transit.Status != Transit.Statuses.TransitToPassenger)
    {
      throw new InvalidOperationException("Transit cannot be started, id = " + transitId);
    }

    transit.Status = Transit.Statuses.InTransit;
    transit.Started = _clock.GetCurrentInstant();
    await _transitRepository.Save(transit);
  }

  public async Task RejectTransit(long? driverId, long? transitId)
  {
    var driver = await _driverRepository.Find(driverId);

    if (driver == null)
    {
      throw new ArgumentException("Driver does not exist, id = " + driverId);
    }

    var transit = await _transitRepository.Find(transitId);

    if (transit == null)
    {
      throw new ArgumentException("Transit does not exist, id = " + transitId);
    }

    transit.DriversRejections.Add(driver);
    transit.AwaitingDriversResponses = transit.AwaitingDriversResponses - 1;
    await _transitRepository.Save(transit);
  }

  public async Task CompleteTransit(long? driverId, long? transitId, AddressDto destinationAddress)
  {
    await CompleteTransit(driverId, transitId, destinationAddress.ToAddressEntity());
  }

  public async Task CompleteTransit(long? driverId, long? transitId, Address destinationAddress)
  {
    destinationAddress = await _addressRepository.Save(destinationAddress);
    var driver = await _driverRepository.Find(driverId);

    if (driver == null)
    {
      throw new ArgumentException("Driver does not exist, id = " + driverId);
    }

    var transit = await _transitRepository.Find(transitId);

    if (transit == null)
    {
      throw new ArgumentException("Transit does not exist, id = " + transitId);
    }

    if (transit.Status == Transit.Statuses.InTransit)
    {
      // TODO FIXME later: add some exceptions handling
      var geoFrom = _geocodingService.GeocodeAddress(transit.From);
      var geoTo = _geocodingService.GeocodeAddress(transit.To);

      transit.To = destinationAddress;
      transit.Km = (float)_distanceCalculator.CalculateByMap(geoFrom[0], geoFrom[1], geoTo[0], geoTo[1]);
      transit.Status = Transit.Statuses.Completed;
      transit.CalculateFinalCosts();
      transit.CompleteTransitAt(_clock.GetCurrentInstant());
      await _transitRepository.Save(transit);
    }
    else
    {
      throw new ArgumentException("Cannot complete Transit, id = " + transitId);
    }
  }

  public async Task<TransitDto> LoadTransit(long? id)
  {
    return new TransitDto(await _transitRepository.Find(id));
  }
}