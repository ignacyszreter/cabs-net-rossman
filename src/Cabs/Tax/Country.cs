namespace LegacyFighter.Cabs.Tax;

public class Country : IEquatable<Country>
{
  private readonly string _name;

  public Country(string name)
  {
    _name = name;
  }

  public static Country Of(string name)
  {
    if (string.IsNullOrWhiteSpace(name) || name.Length == 1)
    {
      throw new InvalidOperationException("Invalid country");
    }

    return new Country(name);
  }

  public string AsString()
  {
    return _name;
  }

  public bool Equals(Country other)
  {
    if (ReferenceEquals(null, other)) return false;
    if (ReferenceEquals(this, other)) return true;
    return _name == other._name;
  }

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(null, obj)) return false;
    if (ReferenceEquals(this, obj)) return true;
    if (obj.GetType() != GetType()) return false;
    return Equals((Country)obj);
  }

  public override int GetHashCode()
  {
    return _name.GetHashCode();
  }
}
