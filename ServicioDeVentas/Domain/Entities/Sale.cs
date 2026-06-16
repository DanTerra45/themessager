using Domain.Entities.Enums;

namespace Domain.Entities{
  public record Sale(
     int Id,
     int CustomerId,
     int OperatorId,
     decimal TotalPrice,
     DateTime CreatedAt,
     SaleState State
      ){
  }
}
