namespace Domain.Dto.Register{
  public sealed record RegisterSaleDetailDto(
      int ProductId,
      int Quantity,
      decimal UnitPrice,
      decimal? SubTotal
      ){
    public decimal CalculateSubTotal {get;init;} = SubTotal ?? (Quantity * UnitPrice);
  }
}
