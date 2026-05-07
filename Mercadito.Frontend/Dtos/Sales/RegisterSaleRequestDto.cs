namespace Mercadito.Frontend.Dtos.Sales;

public sealed record RegisterSaleRequestDto(
    long? CustomerId,
    CreateSaleCustomerRequestDto? NewCustomer,
    string Channel,
    string PaymentMethod,
    IReadOnlyList<RegisterSaleLineRequestDto> Lines);

public sealed record CreateSaleCustomerRequestDto(
    string CiNit,
    string BusinessName,
    string? Phone,
    string? Email,
    string? Address);

public sealed record RegisterSaleLineRequestDto(
    long ProductId,
    string LotCode,
    int Quantity);
