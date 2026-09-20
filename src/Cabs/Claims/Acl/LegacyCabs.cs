using LegacyFighter.Cabs.Claims.Storage;
using LegacyFighter.Cabs.Claims.Sync;
using LegacyFighter.Cabs.Config;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;
using LegacyFighter.Cabs.Service;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LegacyFighter.Cabs.Claims.Acl;

public class LegacyCabs
{
  private readonly IClaimRepository _claims;
  private readonly IClientRepository _clients;
  private readonly ITransitRepository _transits;
  private readonly ClaimNumberGenerator _claimNumbers;
  private readonly IAppProperties _properties;
  private readonly IAwardsService _awards;
  private readonly IClientNotificationService _clientNotifications;
  private readonly IDriverNotificationService _driverNotifications;
  private readonly IClock _clock;
  private readonly IClaimsEvents _events;
  private readonly SqLiteDbContext _context;

  public LegacyCabs(
    IClaimRepository claims,
    IClientRepository clients,
    ITransitRepository transits,
    ClaimNumberGenerator claimNumbers,
    IAppProperties properties,
    IAwardsService awards,
    IClientNotificationService clientNotifications,
    IDriverNotificationService driverNotifications,
    IClock clock,
    IClaimsEvents events,
    SqLiteDbContext context)
  {
    _claims = claims;
    _clients = clients;
    _transits = transits;
    _claimNumbers = claimNumbers;
    _properties = properties;
    _awards = awards;
    _clientNotifications = clientNotifications;
    _driverNotifications = driverNotifications;
    _clock = clock;
    _events = events;
    _context = context;
  }

  public ClaimPolicy Policy()
  {
    return new ClaimPolicy(
      _properties.AutomaticRefundForVipThreshold,
      _properties.NoOfTransitsForClaimAutomaticRefund);
  }

  public async Task<ClaimToResolve> ClaimToResolve(long claimId)
  {
    var claim = await Find(claimId);
    var owner = claim.Owner;
    return new ClaimToResolve(
      claim.ClaimNo,
      new Claimant(owner.Id!.Value, owner.Type == Client.Types.Vip, (await _transits.FindByClient(owner)).Count),
      new ClaimedTransit(claim.Transit.Id!.Value, claim.Transit.Price?.IntValue, claim.Transit.Driver?.Id),
      (await _claims.FindByOwner(owner)).Count,
      (await _claims.FindByOwnerAndTransit(owner, claim.Transit)).Count);
  }

  public async Task<ClaimDto> Apply(long claimId, Resolution resolution)
  {
    var claim = await Find(claimId);
    var now = _clock.GetCurrentInstant();
    var refunded = resolution.Decision == Decision.Refunded;
    claim.Status = refunded ? Claim.Statuses.Refunded : Claim.Statuses.Escalated;
    claim.CompletionMode = refunded ? Claim.CompletionModes.Automatic : Claim.CompletionModes.Manual;
    claim.CompletionDate = now;
    claim.ChangeDate = now;
    Send(claim.ClaimNo, resolution);
    if (resolution.SpecialMiles > 0)
    {
      await _awards.RegisterSpecialMiles(claim.Owner.Id, resolution.SpecialMiles);
    }

    return new ClaimDto(claim);
  }

  public async Task<ClaimDto> Register(NewClaim newClaim)
  {
    var owner = await _clients.Find(newClaim.ClientId)
                ?? throw new InvalidOperationException("Client does not exists");
    var transit = await _transits.Find(newClaim.TransitId)
                  ?? throw new InvalidOperationException("Transit does not exists");
    var claim = new Claim { CreationDate = _clock.GetCurrentInstant() };
    claim.ClaimNo = await _claimNumbers.Generate(claim);
    claim.Owner = owner;
    claim.Transit = transit;
    claim.Reason = newClaim.Reason;
    claim.IncidentDescription = newClaim.IncidentDescription;
    claim.Status = newClaim.IsDraft ? Claim.Statuses.Draft : Claim.Statuses.New;
    var registered = await _claims.Save(claim);
    await _events.Publish(new ClaimRegistered(
      registered.Id!.Value, registered.ClaimNo, owner.Id!.Value, transit.Id!.Value,
      newClaim.IsDraft, registered.CreationDate, registered.Reason, registered.IncidentDescription));
    return new ClaimDto(registered);
  }

  public async Task<ClaimDto> MarkInProcess(long claimId)
  {
    var claim = await Find(claimId);
    claim.Status = Claim.Statuses.InProcess;
    return new ClaimDto(claim);
  }

  public async Task<ClaimDto> View(long claimId)
  {
    return new ClaimDto(await Find(claimId));
  }

  public async Task<List<ClaimantRecord>> AllClaimants()
  {
    return await _context.Clients
      .Select(client => new ClaimantRecord
      {
        ClientId = client.Id!.Value,
        IsVip = client.Type == Client.Types.Vip,
        OrderedTransits = _context.Transits.Count(transit => transit.Client == client)
      })
      .ToListAsync();
  }

  public async Task<List<ClaimedTransitRecord>> AllClaimedTransits()
  {
    return await _context.Transits
      .Select(transit => new ClaimedTransitRecord
      {
        TransitId = transit.Id!.Value,
        ClaimantId = transit.Client.Id!.Value,
        DriverId = transit.Driver == null ? null : transit.Driver.Id,
        Fare = transit.Price == null ? null : transit.Price.IntValue
      })
      .ToListAsync();
  }

  public async Task<List<long>> AllClaimIds()
  {
    return await _context.Claims.Select(claim => claim.Id!.Value).ToListAsync();
  }

  public async Task<ClaimRecord> ReadClaim(long claimId)
  {
    return RecordOf(await Find(claimId));
  }

  private static ClaimRecord RecordOf(Claim claim)
  {
    return new ClaimRecord
    {
      ClaimId = claim.Id!.Value,
      ClaimNo = claim.ClaimNo,
      ClaimantId = claim.Owner.Id!.Value,
      TransitId = claim.Transit.Id!.Value,
      Status = StatusOf(claim.Status!.Value),
      CompletionMode = claim.CompletionMode == null ? null : ModeOf(claim.CompletionMode.Value),
      CreatedAt = claim.CreationDate,
      CompletedAt = claim.CompletionDate,
      Reason = claim.Reason,
      IncidentDescription = claim.IncidentDescription
    };
  }

  private static ClaimStatus StatusOf(Claim.Statuses status)
  {
    return status switch
    {
      Claim.Statuses.Draft => ClaimStatus.Draft,
      Claim.Statuses.New => ClaimStatus.New,
      Claim.Statuses.InProcess => ClaimStatus.InProcess,
      Claim.Statuses.Refunded => ClaimStatus.Refunded,
      _ => ClaimStatus.Escalated
    };
  }

  private static ClaimCompletionMode ModeOf(Claim.CompletionModes mode)
  {
    return mode == Claim.CompletionModes.Automatic ? ClaimCompletionMode.Automatic : ClaimCompletionMode.Manual;
  }

  private void Send(string claimNo, Resolution resolution)
  {
    switch (resolution.Ask)
    {
      case Ask.ClientAboutRefund:
        _clientNotifications.NotifyClientAboutRefund(claimNo, resolution.Recipient);
        break;
      case Ask.ClientForMoreInformation:
        _clientNotifications.AskForMoreInformation(claimNo, resolution.Recipient);
        break;
      case Ask.DriverForDetails:
        _driverNotifications.AskDriverForDetailsAboutClaim(claimNo, resolution.Recipient);
        break;
    }
  }

  private async Task<Claim> Find(long claimId)
  {
    return await _claims.Find(claimId) ?? throw new InvalidOperationException("Claim does not exists");
  }
}
