namespace Domain.Dto.Response{
  public record CustomerResponseDto(
      int Id,
      int Ci,
      string? Complement,
      string RazonSocial
      ){

  }
}
