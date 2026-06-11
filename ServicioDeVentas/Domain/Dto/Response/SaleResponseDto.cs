using Domain.Entities.Enums;

namespace Domain.Dto.Response{
  public record SaleResponseDto(
    int SaleId,
    CustomerResponseDto Customer,
    DateTime CreatedAt,
    SaleState State,
    IEnumerable<SaleDetailResponseDto> Details
      ){
        public decimal TotalPrice{get; init;} = Details?.Sum(detail => detail.SubTotal) ?? 0;
  }
}
