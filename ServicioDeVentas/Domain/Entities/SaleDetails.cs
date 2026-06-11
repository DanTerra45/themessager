namespace Domain.Entities{
    public record SaleDetails(
      int Id,
      int SaleId,
      int ProductId,
      int Quantity,
      decimal UnitPrice,
      decimal? SubTotal
    ){
      public decimal CalculateSubTotal {get; init;} = SubTotal ?? (Quantity * UnitPrice);
    }
}
