namespace Domain.Dto.Response{
  public record SaleDetailResponseDto(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal
      ){
  }
}
