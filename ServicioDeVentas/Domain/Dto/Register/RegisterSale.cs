using Domain.Entities.Enums;

namespace Domain.Dto.Register{
  public record RegisterSale(
      int CustomerId,
      int OperatorId,
      decimal TotalPrice
      ) {
      public new DateTime CreatedAt => DateTime.Now;
      public new SaleState State => SaleState.Pending;
  }
}
