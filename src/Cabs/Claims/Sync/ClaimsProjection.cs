using LegacyFighter.Cabs.Claims.Storage;
using Mediator;

namespace LegacyFighter.Cabs.Claims.Sync;

public class ClaimsProjection : INotificationHandler<ClaimRegistered>
{
  private readonly ClaimsDbContext _claims;

  public ClaimsProjection(ClaimsDbContext claims)
  {
    _claims = claims;
  }

  public ValueTask Handle(ClaimRegistered notification, CancellationToken cancellationToken)
  {
    return ValueTask.CompletedTask;
  }
}
