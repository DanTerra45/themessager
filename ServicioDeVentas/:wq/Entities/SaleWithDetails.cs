using Domain.Entities.Enums;


namespace Domain.Entities
{
    public record SaleWithDetails(
     int Id,
     int CustomerId,
     int OperatorId,
     decimal TotalPrice,
     DateTime CreatedAt,
     SaleState State,
     IEnumerable<SaleDetails> Details
    ): Sale(Id,CustomerId,OperatorId,TotalPrice,CreatedAt,State){
      public new decimal TotalPrice => Details?.Sum(detail => detail.CalculateSubTotal) ?? base.TotalPrice;
    }
}
