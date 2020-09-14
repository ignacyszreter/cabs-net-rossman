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
  private readonly IGeocodingService _geocodingService;
  private readonly AddressRepository _addressRepository;
  private readonly IClock _clock;

  public TransitService(
    IDriverRepository driverRepository,
    ITransitRepository transitRepository,
    IClientRepository clientRepository,
    DistanceCalculator distanceCalculator,
    IGeocodingService geocodingService,
    AddressRepository addressRepository,
    IClock clock)
  {
    _driverRepository = driverRepository;
    _transitRepository = transitRepository;
    _clientRepository = clientRepository;
    _distanceCalculator = distanceCalculator;
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
    transit.DateTime = SystemClock.Instance.GetCurrentInstant();
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
    await _transitRepository.Save(transit);
  }

  public async Task<TransitDto> LoadTransit(long? id)
  {
    return new TransitDto(await _transitRepository.Find(id));
  }
}