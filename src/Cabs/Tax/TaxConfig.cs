using NodaTime;

namespace LegacyFighter.Cabs.Tax;

public class TaxConfig
{
  private readonly List<TaxRule> _taxRules = new();

  protected TaxConfig()
  {

  }

  public TaxConfig(string country, int maxRulesCount, TaxRule taxRule, Instant when)
  {
    Country = Country.Of(country);
    MaxRulesCount = maxRulesCount;
    Add(taxRule, when);
  }

  public long? Id { get; }
  public string Description { get; private set; }
  public string CountryReason { get; private set; }
  public Country Country { get; private set; }
  public Instant? LastModifiedDate { get; private set; }
  public string ModifiedBy { get; private set; }
  public int MaxRulesCount { get; private set; }
  public virtual IReadOnlyCollection<TaxRule> TaxRules => _taxRules.AsReadOnly();
  public int CurrentRulesCount => _taxRules.Count;

  public void Add(TaxRule taxRule, Instant when)
  {
    if (MaxRulesCount <= CurrentRulesCount)
    {
      throw new InvalidOperationException("Too many rules");
    }

    _taxRules.Add(taxRule);
    LastModifiedDate = when;
  }

  public void Remove(TaxRule taxRule, Instant when)
  {
    if (_taxRules.Contains(taxRule))
    {
      if (CurrentRulesCount == 1)
      {
        throw new InvalidOperationException("Last rule in country config");
      }

      _taxRules.Remove(taxRule);
      LastModifiedDate = when;
    }
  }

  public override int GetHashCode()
  {
    return GetType().GetHashCode();
  }

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(this, obj)) return true;
    return obj != null && Id != null && Id == (obj as TaxConfig)?.Id;
  }

  public static bool operator ==(TaxConfig left, TaxConfig right)
  {
    return Equals(left, right);
  }

  public static bool operator !=(TaxConfig left, TaxConfig right)
  {
    return !Equals(left, right);
  }
}
