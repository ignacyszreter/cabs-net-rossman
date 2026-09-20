using LegacyFighter.Cabs.Claims.Acl;

namespace LegacyFighter.Cabs.Claims.Storage;

public class ClaimsMigration
{
  private readonly LegacyCabs _legacy;
  private readonly ClaimsDbContext _claims;

  public ClaimsMigration(LegacyCabs legacy, ClaimsDbContext claims)
  {
    _legacy = legacy;
    _claims = claims;
  }

  public async Task<int> Run()
  {
    var moved = await MoveClaimants();
    moved += await MoveClaimedTransits();
    moved += await MoveClaims();
    return moved;
  }

  private async Task<int> MoveClaimants()
  {
    var moved = 0;
    foreach (var claimant in await _legacy.AllClaimants())
    {
      if (await _claims.Claimants.FindAsync(claimant.ClientId) != null)
      {
        continue;
      }

      _claims.Claimants.Add(claimant);
      await _claims.SaveChangesAsync();
      moved++;
    }

    return moved;
  }

  private async Task<int> MoveClaimedTransits()
  {
    var moved = 0;
    foreach (var transit in await _legacy.AllClaimedTransits())
    {
      if (await _claims.ClaimedTransits.FindAsync(transit.TransitId) != null)
      {
        continue;
      }

      _claims.ClaimedTransits.Add(transit);
      await _claims.SaveChangesAsync();
      moved++;
    }

    return moved;
  }

  private async Task<int> MoveClaims()
  {
    var moved = 0;
    foreach (var claimId in await _legacy.AllClaimIds())
    {
      if (await _claims.Claims.FindAsync(claimId) != null)
      {
        continue;
      }

      _claims.Claims.Add(await _legacy.ReadClaim(claimId));
      await _claims.SaveChangesAsync();
      moved++;
    }

    return moved;
  }
}
