using NodaTime;

namespace LegacyFighter.Cabs.Tax;

public class TaxConfig
{
  public TaxConfig()
  {

  }

  public long? Id { get; }
  public string Description { get; set; }
  public string CountryReason { get; set; }
  public Country Country { get; set; }
  public Instant? LastModifiedDate { get; set; }
  public string ModifiedBy { get; set; }
  public int CurrentRulesCount { get; set; }
  public int MaxRulesCount { get; set; }
  public virtual List<TaxRule> TaxRules { get; set; }

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
