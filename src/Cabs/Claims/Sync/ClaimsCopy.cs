using LegacyFighter.Cabs.Claims.Storage;
using Mediator;

namespace LegacyFighter.Cabs.Claims.Sync;

public class ClaimsCopy : INotificationHandler<ClientTypeChanged>
{
  private readonly ClaimsDbContext _claims;

  public ClaimsCopy(ClaimsDbContext claims)
  {
    _claims = claims;
  }

  public ValueTask Handle(ClientTypeChanged notification, CancellationToken cancellationToken)
  {
    return ValueTask.CompletedTask;
  }
}
