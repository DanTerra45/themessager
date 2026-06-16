namespace Domain.Dto.Register{
  public record RegisterSaleRequestDto(
      int CustomerId,
      IEnumerable<RegisterSaleDetailDto> Details
      ){
    public new decimal TotalPrice => Details.Sum(detail => detail.CalculateSubTotal);
  }
}
