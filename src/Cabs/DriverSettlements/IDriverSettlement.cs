using LegacyFighter.Cabs.Dto;

namespace LegacyFighter.Cabs.DriverSettlements;

public interface IDriverSettlement
{
  Task<DriverSettlementDto> Settle(long driverId, int year);
}
