namespace Domain.Dto.Register{
  public record RegisterSaleDetail(
      int SaleId,
      int ProductId,
      int Quantity,
      decimal UnitPrice,
      decimal? SubTotal
      ){
    public decimal CalculateSubTotal {get;init;} = SubTotal ?? (Quantity * UnitPrice);
  }
}
