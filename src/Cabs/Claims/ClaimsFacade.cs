using LegacyFighter.Cabs.Claims.Acl;
using LegacyFighter.Cabs.Claims.Storage;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LegacyFighter.Cabs.Claims;

public class ClaimsFacade
{
  private readonly LegacyCabs _legacy;
  private readonly ClaimsDbContext _claims;
  private readonly IClock _clock;

  public ClaimsFacade(LegacyCabs legacy, ClaimsDbContext claims, IClock clock)
  {
    _legacy = legacy;
    _claims = claims;
    _clock = clock;
  }

  public async Task<ClaimView> Register(NewClaim newClaim)
  {
    var registered = await _legacy.Register(newClaim);
    return ClaimView.Of(await Claim(registered.ClaimId!.Value));
  }

  public async Task<ClaimView> Resolve(long claimId)
  {
    var claim = await Claim(claimId);
    var resolution = ClaimResolver.Resolve(await ToResolve(claim), _legacy.Policy());
    var refunded = resolution.Decision == Decision.Refunded;
    claim.Status = refunded ? ClaimStatus.Refunded : ClaimStatus.Escalated;
    claim.CompletionMode = refunded ? ClaimCompletionMode.Automatic : ClaimCompletionMode.Manual;
    claim.CompletedAt = _clock.GetCurrentInstant();
    await _claims.SaveChangesAsync();

    // Synchronous: resolving fails when the legacy model is down.
    // A ClaimResolved event would cut this last dependency.
    await _legacy.Apply(claimId, resolution);
    return ClaimView.Of(claim);
  }

  public async Task<ClaimView> MarkInProcess(long claimId)
  {
    var claim = await Claim(claimId);
    claim.Status = ClaimStatus.InProcess;
    await _claims.SaveChangesAsync();
    await _legacy.MarkInProcess(claimId);
    return ClaimView.Of(claim);
  }

  public async Task<ClaimView> View(long claimId)
  {
    return ClaimView.Of(await Claim(claimId));
  }

  private async Task<ClaimToResolve> ToResolve(ClaimRecord claim)
  {
    var claimant = await _claims.Claimants.FindAsync(claim.ClaimantId)
                   ?? new ClaimantRecord { ClientId = claim.ClaimantId };
    var transit = await _claims.ClaimedTransits.FindAsync(claim.TransitId)
                  ?? new ClaimedTransitRecord { TransitId = claim.TransitId };
    return new ClaimToResolve(
      claim.ClaimNo,
      new Claimant(claimant.ClientId, claimant.IsVip, claimant.OrderedTransits),
      new ClaimedTransit(transit.TransitId, transit.Fare, transit.DriverId),
      await _claims.Claims.CountAsync(other => other.ClaimantId == claim.ClaimantId),
      await _claims.Claims.CountAsync(other =>
        other.ClaimantId == claim.ClaimantId && other.TransitId == claim.TransitId));
  }

  private async Task<ClaimRecord> Claim(long claimId)
  {
    return await _claims.Claims.FindAsync(claimId)
           ?? throw new InvalidOperationException("Claim does not exists");
  }
}
