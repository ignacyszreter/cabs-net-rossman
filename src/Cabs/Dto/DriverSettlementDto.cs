namespace LegacyFighter.Cabs.Dto;

public record DriverSettlementDto(
  long DriverId,
  int Year,
  Dictionary<int, int> Payments,
  int Total,
  decimal EurRate,
  decimal TotalInEur,
  DateOnly PayoutDate);
