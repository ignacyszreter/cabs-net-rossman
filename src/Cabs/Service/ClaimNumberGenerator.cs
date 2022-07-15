using System.Linq;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;

namespace LegacyFighter.Cabs.Service;

public class ClaimNumberGenerator
{

  private readonly IClaimRepository _claimRepository;

  public ClaimNumberGenerator(IClaimRepository claimRepository)
  {
    _claimRepository = claimRepository;
  }

  public async Task<string> Generate(Claim claim)
  {
    var count = await _claimRepository.Count();
    var prefix = count;
    if (count == 0)
    {
      prefix = 1L;
    }

    return prefix.ToString();
  }
}