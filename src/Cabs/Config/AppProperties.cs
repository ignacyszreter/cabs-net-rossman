namespace LegacyFighter.Cabs.Config;

public interface IAppProperties
{
  int MinNoOfCarsForEcoClass { get; set; }
}

public class AppProperties : IAppProperties
{
  public int MinNoOfCarsForEcoClass { get; set; }
}