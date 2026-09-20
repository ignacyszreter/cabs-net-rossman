using LegacyFighter.Cabs.Claims.Storage;
using Mediator;

namespace LegacyFighter.Cabs.Claims.Sync;

public class ClaimsCopy :
  INotificationHandler<ClientTypeChanged>,
  INotificationHandler<TransitOrdered>,
  INotificationHandler<TransitCompleted>
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

  public ValueTask Handle(TransitOrdered notification, CancellationToken cancellationToken)
  {
    return ValueTask.CompletedTask;
  }

  public ValueTask Handle(TransitCompleted notification, CancellationToken cancellationToken)
  {
    return ValueTask.CompletedTask;
  }
}
