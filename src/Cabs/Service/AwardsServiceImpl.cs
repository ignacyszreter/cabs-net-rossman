using System.Linq;
using LegacyFighter.Cabs.Config;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;
using NodaTime;

namespace LegacyFighter.Cabs.Service;

public class AwardsServiceImpl : IAwardsService
{
  private readonly IAwardsAccountRepository _accountRepository;
  private readonly IAwardedMilesRepository _milesRepository;
  private readonly IClientRepository _clientRepository;
  private readonly ITransitRepository _transitRepository;
  private readonly IClock _clock;
  private readonly IAppProperties _appProperties;

  public AwardsServiceImpl(
    IAwardsAccountRepository accountRepository,
    IAwardedMilesRepository milesRepository,
    IClientRepository clientRepository,
    ITransitRepository transitRepository,
    IClock clock,
    IAppProperties appProperties)
  {
    _accountRepository = accountRepository;
    _milesRepository = milesRepository;
    _clientRepository = clientRepository;
    _transitRepository = transitRepository;
    _clock = clock;
    _appProperties = appProperties;
  }

  public async Task<AwardsAccountDto> FindBy(long? clientId)
  {
    return new AwardsAccountDto(await _accountRepository.FindByClient(await _clientRepository.Find(clientId)));
  }

  public async Task RegisterToProgram(long? clientId)
  {
    var client = await _clientRepository.Find(clientId);

    if (client == null)
    {
      throw new ArgumentException("Client does not exists, id = " + clientId);
    }

    var account = new AwardsAccount
    {
      Client = client,
      Active = false,
      Date = _clock.GetCurrentInstant()
    };

    await _accountRepository.Save(account);
  }

  public async Task ActivateAccount(long? clientId)
  {
    var account = await _accountRepository.FindByClient(await _clientRepository.Find(clientId));

    if (account == null)
    {
      throw new ArgumentException("Account does not exists, id = " + clientId);
    }

    account.Active = true;

    await _accountRepository.Save(account);
  }

  public async Task DeactivateAccount(long? clientId)
  {
    var account = await _accountRepository.FindByClient(await _clientRepository.Find(clientId));

    if (account == null)
    {
      throw new ArgumentException("Account does not exists, id = " + clientId);
    }

    account.Active = false;

    await _accountRepository.Save(account);
  }

  public async Task<AwardedMiles> RegisterMiles(long? clientId, long? transitId)
  {
    var account = await _accountRepository.FindByClient(await _clientRepository.Find(clientId));
    var transit = await _transitRepository.Find(transitId);
    if (transit == null)
    {
      throw new ArgumentException("transit does not exists, id = " + transitId);
    }

    var now = _clock.GetCurrentInstant();
    if (account == null || !account.Active)
    {
      return null;
    }
    else
    {
      var miles = new AwardedMiles
      {
        Transit = transit,
        Date = _clock.GetCurrentInstant(),
        Client = account.Client,
        Miles = _appProperties.DefaultMilesBonus,
        ExpirationDate = now.Plus(Duration.FromDays(_appProperties.MilesExpirationInDays))
      };
      account.IncreaseTransactions();

      await _milesRepository.Save(miles);
      await _accountRepository.Save(account);
      return miles;
    }
  }

  public async Task<int> CalculateBalance(long? clientId)
  {
    var client = await _clientRepository.Find(clientId);
    var milesList = await _milesRepository.FindAllByClient(client);

    var sum = milesList.Where(t => t.ExpirationDate != null && t.ExpirationDate > _clock.GetCurrentInstant())
      .Select(t => t.Miles).Sum();

    return sum;
  }
}