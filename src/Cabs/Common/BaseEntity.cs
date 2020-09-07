 namespace LegacyFighter.Cabs.Common;

public class BaseEntity
{
  public override int GetHashCode()
  {
    return GetType().GetHashCode();
  }

  public long? Id { get; }
}