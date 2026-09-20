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

  public async ValueTask Handle(ClaimRegistered notification, CancellationToken cancellationToken)
  {
    if (await _claims.Claims.FindAsync([notification.ClaimId], cancellationToken) != null)
    {
      return;
    }

    _claims.Claims.Add(new ClaimRecord
    {
      ClaimId = notification.ClaimId,
      ClaimNo = notification.ClaimNo,
      ClaimantId = notification.ClaimantId,
      TransitId = notification.TransitId,
      Status = notification.IsDraft ? ClaimStatus.Draft : ClaimStatus.New,
      CreatedAt = notification.CreatedAt,
      Reason = notification.Reason,
      IncidentDescription = notification.IncidentDescription
    });
    await _claims.SaveChangesAsync(cancellationToken);
  }
}
