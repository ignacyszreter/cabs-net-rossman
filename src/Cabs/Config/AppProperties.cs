namespace LegacyFighter.Cabs.Config;

public interface IAppProperties
{
  int MinNoOfCarsForEcoClass { get; set; }
  int MilesExpirationInDays { get; set; }
  int DefaultMilesBonus { get; set; }
}

public class AppProperties : IAppProperties
{
  public int MinNoOfCarsForEcoClass { get; set; }
  public int MilesExpirationInDays { get; set; } = 365;
  public int DefaultMilesBonus { get; set; } = 10;
}