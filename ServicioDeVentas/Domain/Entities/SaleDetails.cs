namespace Domain.Entities{
    public record SaleDetails(
      int DetailId,
      int SaleId,
      int ProductId,
      int Quantity,
      decimal UnitPrice,
      decimal? SubTotal
    ){
      public int Id => DetailId;
      public decimal CalculateSubTotal {get; init;} = SubTotal ?? (Quantity * UnitPrice);
    }
}
