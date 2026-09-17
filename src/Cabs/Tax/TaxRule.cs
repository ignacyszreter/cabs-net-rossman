namespace LegacyFighter.Cabs.Tax;

public class TaxRule
{
  protected TaxRule()
  {

  }

  private TaxRule(string taxCode, bool isLinear, int aFactor, int bFactor, bool isSquare, int aSquareFactor,
    int bSquareFactor, int cSquareFactor)
  {
    TaxCode = taxCode;
    IsLinear = isLinear;
    AFactor = aFactor;
    BFactor = bFactor;
    IsSquare = isSquare;
    ASquareFactor = aSquareFactor;
    BSquareFactor = bSquareFactor;
    CSquareFactor = cSquareFactor;
  }

  public long? Id { get; }
  public string TaxCode { get; private set; }
  public bool IsLinear { get; private set; }
  public int AFactor { get; private set; }
  public int BFactor { get; private set; }
  public bool IsSquare { get; private set; }
  public int ASquareFactor { get; private set; }
  public int BSquareFactor { get; private set; }
  public int CSquareFactor { get; private set; }
  public virtual TaxConfig TaxConfig { get; private set; }

  public static TaxRule LinearRule(int a, int b, string taxCode)
  {
    if (a == 0)
    {
      throw new InvalidOperationException("Invalid aFactor");
    }

    return new TaxRule(taxCode, true, a, b, false, 0, 0, 0);
  }

  public static TaxRule SquareRule(int a, int b, int c, string taxCode)
  {
    if (a == 0)
    {
      throw new InvalidOperationException("Invalid aFactor");
    }

    return new TaxRule(taxCode, false, 0, 0, true, a, b, c);
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
