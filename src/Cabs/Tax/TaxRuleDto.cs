namespace LegacyFighter.Cabs.Tax;

public class TaxRuleDto
{
  public TaxRuleDto(TaxRule taxRule)
  {
    Id = taxRule.Id;
    TaxCode = taxRule.TaxCode;
    IsLinear = taxRule.IsLinear;
    AFactor = taxRule.AFactor;
    BFactor = taxRule.BFactor;
    IsSquare = taxRule.IsSquare;
    ASquareFactor = taxRule.ASquareFactor;
    BSquareFactor = taxRule.BSquareFactor;
    CSquareFactor = taxRule.CSquareFactor;
  }

  public TaxRuleDto()
  {

  }

  public long? Id { get; set; }
  public string TaxCode { get; set; }
  public bool IsLinear { get; set; }
  public int AFactor { get; set; }
  public int BFactor { get; set; }
  public bool IsSquare { get; set; }
  public int ASquareFactor { get; set; }
  public int BSquareFactor { get; set; }
  public int CSquareFactor { get; set; }
}
