namespace LegacyFighter.Cabs.Tax;

public class TaxRule
{
  public TaxRule()
  {

  }

  public long? Id { get; }
  public string TaxCode { get; set; }
  public bool IsLinear { get; set; }
  public int AFactor { get; set; }
  public int BFactor { get; set; }
  public bool IsSquare { get; set; }
  public int ASquareFactor { get; set; }
  public int BSquareFactor { get; set; }
  public int CSquareFactor { get; set; }
  public virtual TaxConfig TaxConfig { get; set; }

  public static TaxRule LinearRule(int a, int b, string taxCode)
  {
    var taxRule = new TaxRule
    {
      IsLinear = true,
      TaxCode = taxCode,
      AFactor = a,
      BFactor = b
    };

    return taxRule;
  }

  public override int GetHashCode()
  {
    return GetType().GetHashCode();
  }

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(this, obj)) return true;
    return obj != null && Id != null && Id == (obj as TaxRule)?.Id;
  }

  public static bool operator ==(TaxRule left, TaxRule right)
  {
    return Equals(left, right);
  }

  public static bool operator !=(TaxRule left, TaxRule right)
  {
    return !Equals(left, right);
  }
}
