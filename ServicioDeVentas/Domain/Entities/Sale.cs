namespace Domain.Entities{
  public record Sale(
     int Id,
     int CustomerId,
     int OperatorId,
     Decimal TotalPrice,
     Datetime? CreatedAt,
     SaleState State
      ){
      public Datetime CreatedAt => Datetime.now();
  }
}
