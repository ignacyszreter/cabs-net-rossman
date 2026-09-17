CREATE OR ALTER PROCEDURE dbo.CalculateDriverMonthlyPayments
  @DriverId BIGINT,
  @Year INT
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @FeeType INT, @Amount INT, @Min INT;

  SELECT @FeeType = FeeType, @Amount = Amount, @Min = [Min]
  FROM dbo.DriverFees
  WHERE Id = @DriverId;

  IF @@ROWCOUNT = 0
  BEGIN
    RAISERROR('driver Fees not defined for driver, driver id = %I64d', 16, 1, @DriverId);
    RETURN;
  END;

  DECLARE @YearStart BIGINT = CAST(DATEDIFF(SECOND, '19700101', DATEFROMPARTS(@Year, 1, 1)) AS BIGINT) * 10000000;
  DECLARE @YearEnd BIGINT = CAST(DATEDIFF(SECOND, '19700101', DATEFROMPARTS(@Year + 1, 1, 1)) AS BIGINT) * 10000000;

  WITH Fees AS (
    SELECT
      MONTH(DATEADD(SECOND, td.[DateTime] / 10000000, '19700101')) AS [Month],
      CASE @FeeType
        WHEN 0 THEN td.Price - @Amount
        ELSE CAST(ROUND(td.Price * @Amount / 100.0, 0) AS INT)
      END AS Fee
    FROM dbo.Transits td
    WHERE td.DriverId = @DriverId
      AND td.[DateTime] >= @YearStart
      AND td.[DateTime] < @YearEnd
      AND td.Price IS NOT NULL
  )
  SELECT
    [Month],
    SUM(CASE WHEN Fee < @Min THEN @Min ELSE Fee END) AS Payment
  FROM Fees
  GROUP BY [Month]
  ORDER BY [Month];
END
