namespace Domain.Entities{
  public record Customer(
      int Id,
      int Ci,
      string? Complement,
      string RazonSocial,
      int CreatedBy,
      DateTime? CreatedAt
      ){
    public DateTime? CreatedAt => CreatedAt ?? DateTime.Now;
  }
}
