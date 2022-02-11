using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public interface IAwardsService
{
  Task<AwardsAccountDto> FindBy(long? clientId);
  Task RegisterToProgram(long? clientId);
  Task ActivateAccount(long? clientId);
  Task DeactivateAccount(long? clientId);
  Task<AwardedMiles> RegisterMiles(long? clientId, long? transitId);
  Task<int> CalculateBalance(long? clientId);
}